import type { ChatMessage, ChatMessageAttachment } from '@/shared/api/ChatAPI';

export function parseMessageAttachments(msg: ChatMessage): ChatMessageAttachment[] {
	if (!msg.attachmentsJson?.trim()) return [];
	try {
		const parsed = JSON.parse(msg.attachmentsJson) as unknown;
		if (!Array.isArray(parsed)) return [];
		return parsed
			.filter(
				(x): x is ChatMessageAttachment =>
					typeof x === 'object' &&
					x !== null &&
					typeof (x as ChatMessageAttachment).sourceSessionId === 'string' &&
					typeof (x as ChatMessageAttachment).fileName === 'string'
			)
			.map((x) => ({
				sourceSessionId: x.sourceSessionId,
				fileName: x.fileName,
				contentType: typeof x.contentType === 'string' ? x.contentType : 'application/octet-stream',
			}));
	} catch {
		return [];
	}
}

export function parseToolCalls(
	toolCallsJson: string | undefined
): Array<{ toolName: string; result?: string }> {
	if (!toolCallsJson) return [];
	try {
		const arr = JSON.parse(toolCallsJson);
		return Array.isArray(arr)
			? arr.map((tc: Record<string, unknown>) => ({
					toolName: String(tc.toolName ?? tc.tool_name ?? tc.ToolName ?? ''),
					result: (tc.result ?? tc.Result) as string | undefined,
			  }))
			: [];
	} catch {
		return [];
	}
}

export function getToolLabel(toolName: string): string {
	const labels: Record<string, string> = {
		list_documents: 'Получение списка документов',
		create_document: 'Создание документа',
		delete_document: 'Удаление документа',
		rename_document: 'Переименование документа',
		delegate_to_document_agent: 'Передача задачи агенту документа',
		read_document: 'Чтение документа',
		propose_insert: 'Предложение вставки',
		propose_delete: 'Предложение удаления',
		propose_replace: 'Предложение замены',
		propose_document_changes: 'Предложение правок по сущностям',
		query_attachment_text: 'Вопрос по тексту вложения',
		query_attachment_image: 'Вопрос по изображению вложения',
	};
	return labels[toolName] ?? `Вызов ${toolName}`;
}

function num(v: unknown): number | undefined {
	return typeof v === 'number' && !Number.isNaN(v) ? v : undefined;
}

function lineRangePhrase(start: number, end: number): string {
	return start === end ? `строка ${start}` : `строки ${start}–${end}`;
}

function truncateText(s: string, max: number): string {
	const t = s.trim();
	return t.length <= max ? t : t.slice(0, max).trimEnd() + '…';
}

/** Форматирует сырой result инструмента для отображения в чате (без сырого JSON). */
export function formatToolResult(result: string | undefined, _toolName?: string): string {
	if (!result?.trim()) return '—';

	let parsed: unknown;
	try {
		parsed = JSON.parse(result);
	} catch {
		return result.length > 200 ? result.slice(0, 200) + '…' : result;
	}

	if (Array.isArray(parsed)) {
		return `Найдено документов: ${parsed.length}`;
	}

	if (typeof parsed !== 'object' || parsed === null) {
		const s = String(parsed);
		return s.length > 200 ? s.slice(0, 200) + '…' : s;
	}

	const o = parsed as Record<string, unknown>;

	if (typeof o.message === 'string' && o.message.trim()) {
		return truncateText(o.message, 400);
	}

	if (o.replaced === true) {
		const start = num(o.startLine);
		const end = num(o.endLine) ?? start;
		if (start !== undefined && end !== undefined) {
			return `Замена ${lineRangePhrase(start, end)} добавлена в предложения`;
		}
		return 'Замена добавлена в предложения';
	}

	if (o.inserted === true) {
		const line = num(o.startLine);
		if (line !== undefined) {
			return `Вставка после строки ${line} добавлена в предложения`;
		}
		return 'Вставка добавлена в предложения';
	}

	if (o.renamed === true && typeof o.name === 'string' && o.name.trim()) {
		return `Документ переименован: «${truncateText(o.name, 80)}»`;
	}

	if (o.created === true) {
		const name = typeof o.name === 'string' ? o.name.trim() : '';
		return name ? `Документ создан: «${truncateText(name, 80)}»` : 'Документ создан';
	}

	if (o.delegated === true) {
		return 'Задача передана агенту документа';
	}

	if (o.deleted === true) {
		const start = num(o.startLine);
		const end = num(o.endLine) ?? start;
		if (start !== undefined && end !== undefined) {
			return `Удаление ${lineRangePhrase(start, end)} добавлено в предложения`;
		}
		return 'Документ удалён';
	}

	// Неизвестный объект — не показываем JSON целиком
	return 'Готово';
}
