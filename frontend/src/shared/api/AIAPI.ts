import HttpClient from './HttpClient';
import { getAccessToken } from '@/shared/auth/tokenStorage';
import { tryRefreshAccessToken } from '@/shared/auth/tryRefreshAccessToken';
import { readAgentSseStream } from './sseAgentStream';

export enum AgentMode {
	Default = 'Default',
	CaptionGeneration = 'CaptionGeneration',
	ExplainSelection = 'ExplainSelection',
	Refactor = 'Refactor',
	QuestionAnswering = 'QuestionAnswering',
	DocumentEditing = 'DocumentEditing'
}

export type AgentScope = 'global' | 'document';

export interface AgentRequestDTO {
	scope?: AgentScope;
	documentId?: string;
	userMessage: string;
	startLine?: number;
	endLine?: number;
	chatId?: string;
	mode?: AgentMode;
	/** Сессия вложения после POST /api/ai/agent-sources/ingest */
	sourceSessionId?: string;
}

export interface AgentSourcePartSummaryDTO {
	index: number;
	kind: string;
	label: string;
}

export interface AgentSourceIngestResponseDTO {
	sourceSessionId: string;
	originalFileName: string;
	parts: AgentSourcePartSummaryDTO[];
	notes?: string | null;
}

export interface AgentResponseDTO {
	finalMessage: string;
	steps: AgentStepDTO[];
	isComplete: boolean;
}

export interface AgentStepDTO {
	stepNumber: number;
	description: string;
	toolCalls?: ToolCallDTO[];
	toolResult?: string;
	documentChanges?: DocumentEntityChangeDTO[];
}

export interface ToolCallDTO {
	toolName: string;
	arguments: Record<string, any>;
	result?: string;
}

export interface DocumentEntityChangeDTO {
	changeId: string;
	changeType: 'insert' | 'delete';
	entityType: string;
	startLine: number;
	endLine?: number;
	content: string;
	groupId?: string;
	order?: number;
}

interface OllamaKeyStatusDTO {
	hasKey: boolean;
}

interface OllamaKeyDTO {
	apiKey: string;
}

export interface LlmModelOptionDTO {
	modelName: string;
	hasView: boolean;
}

export interface OllamaModelPreferencesResponseDTO {
	agentModelName: string | null;
	attachmentTextModelName: string | null;
	visionModelName: string | null;
	effectiveAgentModel: string;
	effectiveAttachmentTextModel: string;
	effectiveVisionModel: string;
}

export interface SetOllamaModelPreferencesDTO {
	agentModelName: string | null;
	attachmentTextModelName: string | null;
	visionModelName: string | null;
}

class AIAPI extends HttpClient {
	constructor() {
		super();
	}

	private getApiBaseUrl(): string {
		return this.instance.defaults.baseURL || 'http://localhost:5159';
	}

	/** fetch без axios: при 401 один раз пробуем refresh (как interceptors у HttpClient). */
	private async fetchWithAuthRetry(
		url: string,
		init: RequestInit,
		baseURL: string
	): Promise<Response> {
		const withAuth = (): Headers => {
			const h = new Headers(init.headers);
			const token = getAccessToken();
			if (token) h.set('Authorization', `Bearer ${token}`);
			else h.delete('Authorization');
			return h;
		};

		let res = await fetch(url, { ...init, headers: withAuth() });
		if (res.status === 401 && (await tryRefreshAccessToken(baseURL))) {
			res = await fetch(url, { ...init, headers: withAuth() });
		}
		return res;
	}

	async getOllamaKeyStatus(): Promise<OllamaKeyStatusDTO> {
		return this.get<OllamaKeyStatusDTO>('/api/ai/ollama-key/status');
	}

	async getOllamaKey(): Promise<OllamaKeyDTO> {
		return this.get<OllamaKeyDTO>('/api/ai/ollama-key');
	}

	async verifyOllamaKey(apiKey: string): Promise<void> {
		await this.post('/api/ai/ollama-key/verify', { apiKey });
	}

	async setOllamaKey(apiKey: string): Promise<void> {
		await this.post('/api/ai/ollama-key', { apiKey });
	}

	async deleteOllamaKey(): Promise<void> {
		await this.delete('/api/ai/ollama-key');
	}

	async getOllamaModels(): Promise<LlmModelOptionDTO[]> {
		return this.get<LlmModelOptionDTO[]>('/api/ai/ollama-models');
	}

	async getOllamaModelPreferences(): Promise<OllamaModelPreferencesResponseDTO> {
		return this.get<OllamaModelPreferencesResponseDTO>('/api/ai/ollama-model-preferences');
	}

	async putOllamaModelPreferences(
		body: SetOllamaModelPreferencesDTO,
	): Promise<void> {
		await this.put('/api/ai/ollama-model-preferences', body);
	}

	async ingestAgentSource(
		chatId: string,
		file: File,
		documentId?: string | null
	): Promise<AgentSourceIngestResponseDTO> {
		const baseURL = this.getApiBaseUrl();
		const form = new FormData();
		form.append('chatId', chatId);
		form.append('file', file);
		if (documentId) {
			form.append('documentId', documentId);
		}
		let res = await this.fetchWithAuthRetry(
			`${baseURL}/api/ai/agent-sources/ingest`,
			{ method: 'POST', body: form },
			baseURL
		);
		const text = await res.text();
		let body: unknown;
		try {
			body = text ? JSON.parse(text) : {};
		} catch {
			body = {};
		}
		if (!res.ok) {
			const msg =
				typeof body === 'object' && body !== null && 'message' in body
					? String((body as { message?: string }).message)
					: `Ошибка загрузки (${res.status})`;
			throw new Error(msg);
		}
		return body as AgentSourceIngestResponseDTO;
	}

	/**
	 * Отправить запрос агенту (потоковая передача через SSE)
	 * @param request - Запрос к агенту
	 * @param onStep - Callback для обработки каждого шага в реальном времени
	 * @param signal - AbortSignal для остановки запроса (опционально)
	 * @returns Promise с финальным ответом агента
	 */
	async agent(
		request: AgentRequestDTO,
		onStep?: (step: AgentStepDTO) => void,
		signal?: AbortSignal
	): Promise<AgentResponseDTO> {
		const baseURL = this.getApiBaseUrl();
		const url = `${baseURL}/api/ai/agent`;
		const response = await this.fetchWithAuthRetry(
			url,
			{
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify(request),
				signal,
			},
			baseURL
		);

		if (!response.ok) {
			const errorText = await response.text();
			let errorData: { message?: string; details?: string } = {};
			try {
				errorData = JSON.parse(errorText) as { message?: string; details?: string };
			} catch {
				errorData = { message: errorText };
			}
			throw new Error(
				errorData.details
					? `${errorData.message || 'Ошибка запроса'}: ${errorData.details}`
					: errorData.message || `HTTP error! status: ${response.status}`
			);
		}

		const reader = response.body?.getReader();
		if (!reader) {
			throw new Error('Response body is not readable');
		}

		return readAgentSseStream(reader, onStep);
	}

	async downloadAgentSourceOriginal(sessionId: string, suggestedFileName: string): Promise<void> {
		const baseURL = this.getApiBaseUrl();
		let res = await this.fetchWithAuthRetry(
			`${baseURL}/api/ai/agent-sources/${sessionId}/original`,
			{ method: 'GET' },
			baseURL
		);
		if (!res.ok) {
			let msg = `Ошибка скачивания (${res.status})`;
			try {
				const j = await res.json();
				if (j?.message) msg = String(j.message);
			} catch {
				/* use default */
			}
			throw new Error(msg);
		}
		const blob = await res.blob();
		const url = URL.createObjectURL(blob);
		const a = document.createElement('a');
		a.href = url;
		a.download = suggestedFileName || 'download';
		a.rel = 'noopener';
		document.body.appendChild(a);
		a.click();
		a.remove();
		URL.revokeObjectURL(url);
	}
}

export default new AIAPI();
