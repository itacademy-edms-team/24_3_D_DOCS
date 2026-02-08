import { ref, computed } from 'vue';
import AIAPI, {
	type AgentRequestDTO,
	type AgentResponseDTO,
	type AgentStepDTO,
} from '@/shared/api/AIAPI';

type AgentEvent =
	| {
			id: string;
			type: 'tool';
			stepNumber: number;
			toolName: string;
			result?: string;
			arguments?: Record<string, any>;
	  }
	| {
			id: string;
			type: 'final';
			content: string;
	  };

export function useAgent() {
	const isProcessing = ref(false);
	const steps = ref<AgentStepDTO[]>([]);
	const currentResponse = ref<AgentResponseDTO | null>(null);
	const error = ref<string | null>(null);
	const abortController = ref<AbortController | null>(null);

	const sendMessage = async (request: AgentRequestDTO) => {

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
			// Обработка шагов в реальном времени через callback
			const response = await AIAPI.agent(request, (step) => {
				// Обновляем или добавляем шаг в список
				const existingIndex = steps.value.findIndex(
					(s) => s.stepNumber === step.stepNumber
				);
				
				if (existingIndex >= 0) {
					// Обновляем существующий шаг - создаем новый массив для реактивности Vue
					const newSteps = [...steps.value];
					newSteps[existingIndex] = { ...step };
					steps.value = newSteps;
				} else {
					// Добавляем новый шаг - создаем новый массив для реактивности Vue
					steps.value = [...steps.value, { ...step }];
				}
			}, abortController.value.signal);

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

	const events = computed<AgentEvent[]>(() => {
		const toolEvents: AgentEvent[] = [];
		for (const step of steps.value) {
			if (!step.toolCalls || step.toolCalls.length === 0) continue;
			step.toolCalls.forEach((toolCall, idx) => {
				toolEvents.push({
					id: `tool-${step.stepNumber}-${idx}-${toolCall.toolName}`,
					type: 'tool',
					stepNumber: step.stepNumber,
					toolName: toolCall.toolName,
					result: toolCall.result,
					arguments: toolCall.arguments,
				});
			});
		}

		const finalMessage = currentResponse.value?.finalMessage?.trim();
		const finalEvent = finalMessage
			? [
					{
						id: `final-${steps.value.length}-${finalMessage.length}`,
						type: 'final' as const,
						content: finalMessage,
					},
			  ]
			: [];

		return [...toolEvents, ...finalEvent];
	});

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
					contentLines.forEach((line: string, index: number) => {
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
					contentLines.forEach((line: string, index: number) => {
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
		events,
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
