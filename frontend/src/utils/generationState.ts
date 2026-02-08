/**
 * Утилита для управления состоянием генерации документов
 * Сохраняет состояние в localStorage для восстановления после обновления страницы
 */

type GenerationType = 'pdf' | 'ddoc';

interface GenerationState {
	documentId: string;
	type: GenerationType;
	startTime: number;
}

const STORAGE_KEY = 'document-generation-state';

/**
 * Сохранить состояние генерации
 */
export function saveGenerationState(documentId: string, type: GenerationType): void {
	try {
		const states = getGenerationStates();
		const existingIndex = states.findIndex(
			(s) => s.documentId === documentId && s.type === type,
		);
		
		const newState: GenerationState = {
			documentId,
			type,
			startTime: Date.now(),
		};

		if (existingIndex >= 0) {
			states[existingIndex] = newState;
		} else {
			states.push(newState);
		}

		localStorage.setItem(STORAGE_KEY, JSON.stringify(states));
	} catch (error) {
		console.error('Failed to save generation state:', error);
	}
}

/**
 * Удалить состояние генерации
 */
export function removeGenerationState(documentId: string, type: GenerationType): void {
	try {
		const states = getGenerationStates();
		const filtered = states.filter(
			(s) => !(s.documentId === documentId && s.type === type),
		);
		localStorage.setItem(STORAGE_KEY, JSON.stringify(filtered));
	} catch (error) {
		console.error('Failed to remove generation state:', error);
	}
}

/**
 * Получить все состояния генерации
 */
export function getGenerationStates(): GenerationState[] {
	try {
		const stored = localStorage.getItem(STORAGE_KEY);
		if (!stored) return [];
		
		const states: GenerationState[] = JSON.parse(stored);
		// Очищаем устаревшие состояния (старше 30 минут)
		const now = Date.now();
		const validStates = states.filter((s) => now - s.startTime < 30 * 60 * 1000);
		
		if (validStates.length !== states.length) {
			localStorage.setItem(STORAGE_KEY, JSON.stringify(validStates));
		}
		
		return validStates;
	} catch (error) {
		console.error('Failed to get generation states:', error);
		return [];
	}
}

/**
 * Проверить, идет ли генерация для документа
 */
export function isGenerating(documentId: string, type: GenerationType): boolean {
	const states = getGenerationStates();
	return states.some((s) => s.documentId === documentId && s.type === type);
}
