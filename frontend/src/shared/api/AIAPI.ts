import HttpClient from './HttpClient';
import { getAccessToken } from '@/shared/auth/tokenStorage';

export enum AgentMode {
	Default = 'Default',
	CaptionGeneration = 'CaptionGeneration',
	ExplainSelection = 'ExplainSelection',
	Refactor = 'Refactor',
	QuestionAnswering = 'QuestionAnswering',
	DocumentEditing = 'DocumentEditing'
}

export interface AgentRequestDTO {
	documentId: string;
	userMessage: string;
	startLine?: number;
	endLine?: number;
	chatId?: string;
	mode?: AgentMode;
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
}

export interface ToolCallDTO {
	toolName: string;
	arguments: Record<string, any>;
	result?: string;
}

export interface OllamaKeyStatusDTO {
	hasKey: boolean;
}

export interface OllamaKeyDTO {
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
		// Получаем baseURL и токен авторизации
		const baseURL = this.instance.defaults.baseURL || 'http://localhost:5159';
		const accessToken = getAccessToken() || '';
		const url = `${baseURL}/api/ai/agent`;
		const headers: HeadersInit = {
			'Content-Type': 'application/json',
		};
		if (accessToken) {
			headers['Authorization'] = `Bearer ${accessToken}`;
		}

		console.log('Starting SSE request to:', url, 'with headers:', headers);
		const response = await fetch(url, {
			method: 'POST',
			headers,
			body: JSON.stringify(request),
			signal,
		});

		const responseHeaders: Record<string, string> = {};
		response.headers.forEach((value, key) => {
			responseHeaders[key] = value;
		});

		console.log('Response received:', { 
			status: response.status, 
			ok: response.ok, 
			statusText: response.statusText,
			contentType: responseHeaders['content-type'],
			headers: responseHeaders
		});

		if (!response.ok) {
			// Для SSE ошибки могут приходить в потоке, но если статус не OK, пробуем прочитать ошибку
			try {
				const errorText = await response.text();
				console.error('Response error, body:', errorText);
				const errorData = JSON.parse(errorText).catch(() => ({ message: errorText }));
				throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
			} catch (e) {
				if (e instanceof Error && e.message.includes('HTTP error')) {
					throw e;
				}
				throw new Error(`HTTP error! status: ${response.status}`);
			}
		}

		// Проверяем Content-Type для SSE
		const contentType = responseHeaders['content-type'] || '';
		if (!contentType.includes('text/event-stream')) {
			console.warn('Response Content-Type is not text/event-stream:', contentType);
		}

		// Обрабатываем SSE поток
		const reader = response.body?.getReader();
		if (!reader) {
			console.error('Response body is not readable, body:', response.body);
			throw new Error('Response body is not readable');
		}

		console.log('SSE stream reader created, starting to read...');
		const decoder = new TextDecoder();
		let buffer = '';
		let finalResponse: AgentResponseDTO | null = null;
		let chunkCount = 0;

		return new Promise<AgentResponseDTO>((resolve, reject) => {
			const processChunk = async () => {
				try {
					while (true) {
						const { done, value } = await reader.read();
						
						if (done) {
							console.log('Stream ended. Total chunks received:', chunkCount, 'Final response:', finalResponse ? 'present' : 'missing');
							if (finalResponse) {
								resolve(finalResponse);
							} else {
								reject(new Error('Stream ended without complete event'));
							}
							break;
						}

						chunkCount++;
						const chunk = decoder.decode(value, { stream: true });
						buffer += chunk;
						
						console.log(`SSE chunk #${chunkCount} received, chunk length:`, chunk.length, 'buffer length:', buffer.length);
						if (chunkCount <= 5) {
							console.log('Chunk content (first 500 chars):', chunk.substring(0, Math.min(500, chunk.length)));
							console.log('Full buffer (first 1000 chars):', buffer.substring(0, Math.min(1000, buffer.length)));
						}
						
						// Парсим события (события разделены двойным переводом строки)
						const events = buffer.split('\n\n');
						buffer = events.pop() || ''; // Сохраняем неполное событие

						console.log('Parsed events count:', events.length, 'remaining buffer length:', buffer.length);

						for (const eventBlock of events) {
							if (!eventBlock.trim()) {
								continue;
							}

							// Пропускаем комментарии SSE (начинаются с :)
							if (eventBlock.trim().startsWith(':')) {
								continue;
							}

							let eventType = '';
							let dataStr = '';

							const lines = eventBlock.split('\n');
							for (const line of lines) {
								const trimmedLine = line.trim();
								if (trimmedLine.startsWith('event: ')) {
									eventType = trimmedLine.substring(7).trim();
								} else if (trimmedLine.startsWith('data: ')) {
									// Может быть несколько строк data: - объединяем их
									const dataLine = trimmedLine.substring(6);
									if (dataStr) {
										dataStr += '\n' + dataLine;
									} else {
										dataStr = dataLine;
									}
								}
							}

							if (!dataStr) {
								continue;
							}

							try {
								const data = JSON.parse(dataStr);
								
								// Определяем тип события
								if (!eventType) {
									if (data.stepNumber !== undefined) {
										eventType = 'step';
									} else if (data.isComplete !== undefined) {
										eventType = 'complete';
									} else if (data.message) {
										eventType = 'error';
									}
								}

								console.log('SSE Event processed:', { eventType, hasStepNumber: data.stepNumber !== undefined, hasIsComplete: data.isComplete !== undefined });

								if (eventType === 'step' || data.stepNumber !== undefined) {
									const step = data as AgentStepDTO;
									console.log('STEP EVENT:', step);
									if (onStep) {
										// Вызываем callback синхронно, чтобы Vue успел обновить UI
										onStep(step);
										console.log('onStep callback called for step:', step.stepNumber);
									}
								} else if (eventType === 'complete' || data.isComplete !== undefined) {
									finalResponse = data as AgentResponseDTO;
									console.log('COMPLETE EVENT:', finalResponse);
								} else if (eventType === 'error' || data.message) {
									console.error('ERROR EVENT:', data);
									reject(new Error(data.message || 'Unknown error'));
									return;
								}
							} catch (e) {
								console.error('Error parsing SSE data:', e, 'dataStr (first 200):', dataStr.substring(0, 200));
							}
						}
					}
				} catch (error) {
					reject(error);
				}
			};

			processChunk();
		});
	}
}

export default new AIAPI();
