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

export function formatToolResult(result: string | undefined): string {
	if (!result) return '—';
	try {
		const parsed = JSON.parse(result);
		if (Array.isArray(parsed)) return `Найдено документов: ${parsed.length}`;
		if (typeof parsed === 'object' && parsed !== null) {
			const o = parsed as Record<string, unknown>;
			if (o.message) return String(o.message);
			if (o.deleted) return 'Документ удалён';
			if (o.created) return String(o.message ?? 'Документ создан');
		}
	} catch {
		/* fallback */
	}
	return result.length > 200 ? result.slice(0, 200) + '…' : result;
}
