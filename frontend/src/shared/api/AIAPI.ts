import HttpClient from './HttpClient';
import { getAccessToken } from '@/shared/auth/tokenStorage';
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

class AIAPI extends HttpClient {
	constructor() {
		super();
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

	async ingestAgentSource(
		chatId: string,
		file: File,
		documentId?: string | null
	): Promise<AgentSourceIngestResponseDTO> {
		const baseURL = this.instance.defaults.baseURL || 'http://localhost:5159';
		const form = new FormData();
		form.append('chatId', chatId);
		form.append('file', file);
		if (documentId) {
			form.append('documentId', documentId);
		}
		const headers: HeadersInit = {};
		const accessToken = getAccessToken();
		if (accessToken) {
			headers['Authorization'] = `Bearer ${accessToken}`;
		}
		const res = await fetch(`${baseURL}/api/ai/agent-sources/ingest`, {
			method: 'POST',
			headers,
			body: form,
		});
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
		const baseURL = this.instance.defaults.baseURL || 'http://localhost:5159';
		const accessToken = getAccessToken() || '';
		const url = `${baseURL}/api/ai/agent`;
		const headers: HeadersInit = {
			'Content-Type': 'application/json',
		};
		if (accessToken) {
			headers['Authorization'] = `Bearer ${accessToken}`;
		}

		const response = await fetch(url, {
			method: 'POST',
			headers,
			body: JSON.stringify(request),
			signal,
		});

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
		const baseURL = this.instance.defaults.baseURL || 'http://localhost:5159';
		const accessToken = getAccessToken() || '';
		const res = await fetch(`${baseURL}/api/ai/agent-sources/${sessionId}/original`, {
			headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
		});
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
