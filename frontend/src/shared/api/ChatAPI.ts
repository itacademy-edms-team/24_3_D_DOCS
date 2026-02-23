import HttpClient from './HttpClient';

export type ChatScope = 'global' | 'document';

interface ChatSessionRaw {
	id: string;
	scope: number | string;
	documentId?: string | null;
	title: string;
	isArchived: boolean;
	createdAt: string;
	updatedAt: string;
	messageCount: number;
}

function normalizeChatSession(r: ChatSessionRaw): ChatSession {
	const scope: ChatScope = r.scope === 0 || r.scope === 'Global' || r.scope === 'global' ? 'global' : 'document';
	return {
		...r,
		scope,
		documentId: r.documentId ?? null
	};
}

export interface ChatSession {
	id: string;
	scope: ChatScope;
	documentId: string | null;
	title: string;
	isArchived: boolean;
	createdAt: string;
	updatedAt: string;
	messageCount: number;
}

export interface ChatMessage {
	id: string;
	role: string;
	content: string;
	stepNumber?: number;
	toolCalls?: string;
	createdAt: string;
}

export interface ChatSessionWithMessages extends ChatSession {
	messages: ChatMessage[];
}

export interface ChatContextFile {
	id: string;
	chatSessionId: string;
	fileName: string;
	contentType: string;
	createdAt: string;
}

export interface CreateChatSession {
	scope: ChatScope;
	documentId?: string | null;
	title?: string;
}

interface UpdateChatSession {
	title?: string;
}

class ChatAPI extends HttpClient {
	constructor() {
		super();
	}

	/**
	 * Получить чаты по scope
	 */
	async getChats(
		scope: ChatScope,
		documentId?: string | null,
		includeArchived: boolean = false
	): Promise<ChatSession[]> {
		const params: Record<string, string | boolean> = {
			scope,
			includeArchived
		};
		if (scope === 'document' && documentId) {
			params.documentId = documentId;
		}
		const response = await this.instance.get<ChatSessionRaw[]>('/api/chats', { params });
		return response.data.map(normalizeChatSession);
	}

	/**
	 * Получить все чаты документа (обратная совместимость)
	 */
	async getChatsByDocument(
		documentId: string,
		includeArchived: boolean = false
	): Promise<ChatSession[]> {
		return this.getChats('document', documentId, includeArchived);
	}

	/**
	 * Получить чат с сообщениями по ID
	 */
	async getChatById(chatId: string): Promise<ChatSessionWithMessages> {
		const response = await this.instance.get<ChatSessionRaw & { messages: ChatMessage[] }>(
			`/api/chats/${chatId}`
		);
		const raw = response.data;
		return {
			...normalizeChatSession(raw),
			messages: raw.messages
		};
	}

	/**
	 * Создать новый чат
	 */
	async createChat(dto: CreateChatSession): Promise<ChatSession> {
		const body = {
			scope: dto.scope === 'global' ? 'Global' : 'Document',
			documentId: dto.documentId ?? null,
			title: dto.title
		};
		const response = await this.instance.post<ChatSessionRaw>('/api/chats', body);
		return normalizeChatSession(response.data);
	}

	/**
	 * Обновить чат (переименование)
	 */
	async updateChat(chatId: string, dto: UpdateChatSession): Promise<ChatSession> {
		const response = await this.instance.put<ChatSession>(
			`/api/chats/${chatId}`,
			dto
		);
		return response.data;
	}

	/**
	 * Переместить чат в архив
	 */
	async archiveChat(chatId: string): Promise<void> {
		await this.instance.delete(`/api/chats/${chatId}`);
	}

	/**
	 * Восстановить чат из архива
	 */
	async restoreChat(chatId: string): Promise<void> {
		await this.instance.post(`/api/chats/${chatId}/restore`);
	}

	/**
	 * Удалить чат навсегда
	 */
	async deleteChatPermanently(chatId: string): Promise<void> {
		await this.instance.delete(`/api/chats/${chatId}/permanent`);
	}

	/**
	 * Список контекстных файлов чата
	 */
	async getContextFiles(chatId: string): Promise<ChatContextFile[]> {
		const response = await this.instance.get<ChatContextFile[]>(
			`/api/chats/${chatId}/context-files`
		);
		return response.data;
	}

	/**
	 * Загрузить контекстный файл
	 */
	async uploadContextFile(chatId: string, file: File): Promise<ChatContextFile> {
		const formData = new FormData();
		formData.append('file', file);
		const response = await this.instance.post<ChatContextFile>(
			`/api/chats/${chatId}/context-files`,
			formData,
			{ headers: { 'Content-Type': 'multipart/form-data' } }
		);
		return response.data;
	}

	/**
	 * Удалить контекстный файл
	 */
	async deleteContextFile(chatId: string, fileId: string): Promise<void> {
		await this.instance.delete(`/api/chats/${chatId}/context-files/${fileId}`);
	}
}

export default new ChatAPI();
