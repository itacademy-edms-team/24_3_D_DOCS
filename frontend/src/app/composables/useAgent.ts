import { ref, computed, nextTick } from 'vue';
import AIAPI, {
	type AgentRequestDTO,
	type AgentResponseDTO,
	type AgentStepDTO,
} from '@/shared/api/AIAPI';

export function useAgent() {
	const isProcessing = ref(false);
	const steps = ref<AgentStepDTO[]>([]);
	const currentResponse = ref<AgentResponseDTO | null>(null);
	const error = ref<string | null>(null);
	const abortController = ref<AbortController | null>(null);

	const sendMessage = async (request: AgentRequestDTO) => {
		// #region agent log
		try {
			fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					sessionId: 'debug-session',
					runId: 'run1',
					hypothesisId: 'A',
					location: 'useAgent.ts:14',
					message: 'sendMessage entry',
					data: {
						documentId: request.documentId,
						userMessage: request.userMessage.substring(0, Math.min(50, request.userMessage.length)),
						mode: request.mode,
						startLine: request.startLine,
						endLine: request.endLine
					},
					timestamp: Date.now()
				})
			}).catch(() => {});
		} catch {}
		// #endregion

		// Отменяем предыдущий запрос, если он ещё идёт
		if (abortController.value) {
			abortController.value.abort();
			abortController.value = null;
		}

		abortController.value = new AbortController();
		isProcessing.value = true;
		error.value = null;
		steps.value = [];
		currentResponse.value = null;

		try {
			// #region agent log
			try {
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6', {
					method: 'POST',
					headers: { 'Content-Type': 'application/json' },
					body: JSON.stringify({
						sessionId: 'debug-session',
						runId: 'run1',
						hypothesisId: 'B',
						location: 'useAgent.ts:32',
						message: 'Before AIAPI.agent call',
						data: { request },
						timestamp: Date.now()
					})
				}).catch(() => {});
			} catch {}
			// #endregion

			// Обработка шагов в реальном времени через callback
			const response = await AIAPI.agent(request, (step) => {
				console.log('=== onStep callback called ===');
				console.log('Step received:', step);
				console.log('Current steps before update:', steps.value.length, steps.value.map(s => s.stepNumber));
				
				// Обновляем или добавляем шаг в список
				const existingIndex = steps.value.findIndex(
					(s) => s.stepNumber === step.stepNumber
				);
				
				if (existingIndex >= 0) {
					// Обновляем существующий шаг - создаем новый массив для реактивности Vue
					const newSteps = [...steps.value];
					newSteps[existingIndex] = { ...step };
					steps.value = newSteps;
					console.log('Updated existing step at index:', existingIndex);
				} else {
					// Добавляем новый шаг - создаем новый массив для реактивности Vue
					steps.value = [...steps.value, { ...step }];
					console.log('Added new step, total steps now:', steps.value.length);
				}
				
				console.log('Steps after update:', steps.value.length);
				console.log('Step numbers:', steps.value.map(s => s.stepNumber));
				console.log('Step descriptions:', steps.value.map(s => s.description));
				
				// Принудительно обновляем Vue
				nextTick(() => {
					console.log('Vue nextTick - steps should be updated in UI');
				});
			}, abortController.value.signal);

			// #region agent log
			try {
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6', {
					method: 'POST',
					headers: { 'Content-Type': 'application/json' },
					body: JSON.stringify({
						sessionId: 'debug-session',
						runId: 'run1',
						hypothesisId: 'B',
						location: 'useAgent.ts:40',
						message: 'AIAPI.agent completed successfully',
						data: {
							isComplete: response.isComplete,
							stepsCount: response.steps?.length ?? 0,
							finalMessageLength: response.finalMessage?.length ?? 0
						},
						timestamp: Date.now()
					})
				}).catch(() => {});
			} catch {}
			// #endregion

			currentResponse.value = response;
			// Дополняем шаги из финального ответа (на случай если некоторые не пришли через SSE)
			if (response.steps && response.steps.length > 0) {
				// Объединяем шаги из финального ответа с уже имеющимися
				response.steps.forEach((step) => {
					const existingIndex = steps.value.findIndex(
						(s) => s.stepNumber === step.stepNumber
					);
					if (existingIndex >= 0) {
						// Обновляем существующий шаг, если в финальном ответе больше данных
						steps.value[existingIndex] = { ...step };
					} else {
						// Добавляем новый шаг
						steps.value.push({ ...step });
					}
				});
			}

			return response;
		} catch (err: any) {
			// Если запрос был отменён пользователем — не считаем это ошибкой
			if (err?.name === 'AbortError') {
				console.warn('Agent request aborted by user');
				return;
			}
			// #region agent log
			try {
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6', {
					method: 'POST',
					headers: { 'Content-Type': 'application/json' },
					body: JSON.stringify({
						sessionId: 'debug-session',
						runId: 'run1',
						hypothesisId: 'B',
						location: 'useAgent.ts:57',
						message: 'sendMessage error',
						data: {
							errorType: err?.constructor?.name,
							errorMessage: err?.message,
							responseStatus: err?.response?.status,
							responseData: err?.response?.data
						},
						timestamp: Date.now()
					})
				}).catch(() => {});
			} catch {}
			// #endregion

			const errorMessage =
				err.response?.data?.message || err.message || 'Ошибка при обращении к агенту';
			error.value = errorMessage;
			throw err;
		} finally {
			isProcessing.value = false;
			abortController.value = null;
		}
	};

	const stop = () => {
		if (abortController.value) {
			abortController.value.abort();
			abortController.value = null;
			isProcessing.value = false;
		}
	};

	const reset = () => {
		isProcessing.value = false;
		steps.value = [];
		currentResponse.value = null;
		error.value = null;
		if (abortController.value) {
			abortController.value.abort();
			abortController.value = null;
		}
	};

	const currentStep = computed(() => {
		if (steps.value.length === 0) return null;
		return steps.value[steps.value.length - 1];
	});

	const totalSteps = computed(() => steps.value.length);

	/**
	 * Парсит изменения документа из toolCalls шага
	 * @param step - Шаг агента с toolCalls
	 * @param currentContent - Текущее содержимое документа (для получения originalText)
	 * @returns Массив изменений для визуализации
	 */
	const parseDocumentChanges = (
		step: AgentStepDTO,
		currentContent?: string
	): Array<{
		lineNumber: number;
		type: 'added' | 'deleted';
		operation: 'insert' | 'update' | 'delete';
		text?: string;
	}> => {
		const changes: Array<{
			lineNumber: number;
			type: 'added' | 'deleted';
			operation: 'insert' | 'update' | 'delete';
			text?: string;
		}> = [];

		if (!step.toolCalls || step.toolCalls.length === 0) {
			return changes;
		}

		const lines = currentContent ? currentContent.split('\n') : [];

		for (const toolCall of step.toolCalls) {
			const toolName = toolCall.toolName;
			const args = toolCall.arguments || {};

			if (toolName === 'insert') {
				// insert(id, content) - вставляет ПОСЛЕ строки id (1-based)
				// Если id=5 (1-based), то вставляется после строки с индексом 4 (0-based)
				// Значит новые строки будут на позициях 5, 6, 7, ... (0-based)
				// Формула: lineNumber = id - 1 + index + 1 = id + index (0-based), где id 1-based, index 0-based
				const id = typeof args.id === 'number' ? args.id : parseInt(args.id);
				const content = args.content || '';
				
				if (!isNaN(id) && content) {
					const contentLines = content.split('\n');
					contentLines.forEach((line, index) => {
						changes.push({
							lineNumber: id + index, // id (1-based) -> id-1 (0-based) + index + 1 = id + index (0-based)
							type: 'added',
							operation: 'insert',
							text: line,
						});
					});
				}
			} else if (toolName === 'edit') {
				// edit(id, content) - заменяет строку id и последующие (1-based)
				const id = typeof args.id === 'number' ? args.id : parseInt(args.id);
				const content = args.content || '';
				
				if (!isNaN(id) && content) {
					const contentLines = content.split('\n');
					const lineIndex = id - 1; // 1-based -> 0-based
					
					// Для update: сначала удаляем старые строки, потом добавляем новые
					// Определяем сколько строк нужно удалить (минимум 1, максимум до конца или до количества новых)
					const oldLinesCount = Math.min(
						contentLines.length,
						lines.length - lineIndex
					);
					
					// Добавляем удаленные строки
					for (let i = 0; i < oldLinesCount; i++) {
						if (lineIndex + i < lines.length) {
							changes.push({
								lineNumber: lineIndex + i,
								type: 'deleted',
								operation: 'update',
								text: lines[lineIndex + i],
							});
						}
					}
					
					// Добавляем новые строки
					contentLines.forEach((line, index) => {
						changes.push({
							lineNumber: lineIndex + index,
							type: 'added',
							operation: 'update',
							text: line,
						});
					});
				}
			} else if (toolName === 'delete') {
				// delete(id, [id_end]) - удаляет строку(и) (1-based)
				const id = typeof args.id === 'number' ? args.id : parseInt(args.id);
				const idEnd = args.id_end !== undefined 
					? (typeof args.id_end === 'number' ? args.id_end : parseInt(args.id_end))
					: id;
				
				if (!isNaN(id)) {
					const startIndex = id - 1; // 1-based -> 0-based
					const endIndex = Math.min(
						(idEnd - 1), // 1-based -> 0-based
						lines.length - 1
					);
					
					for (let i = startIndex; i <= endIndex; i++) {
						if (i >= 0 && i < lines.length) {
							changes.push({
								lineNumber: i,
								type: 'deleted',
								operation: 'delete',
								text: lines[i],
							});
						}
					}
				}
			}
		}

		return changes;
	};

	return {
		isProcessing,
		steps,
		currentResponse,
		error,
		currentStep,
		totalSteps,
		sendMessage,
		stop,
		reset,
		parseDocumentChanges,
	};
}
