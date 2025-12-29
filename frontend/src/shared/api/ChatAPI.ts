import HttpClient from './HttpClient';

export interface ChatSession {
	id: string;
	documentId: string;
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

export interface CreateChatSession {
	documentId: string;
	title?: string;
}

export interface UpdateChatSession {
	title?: string;
}

class ChatAPI extends HttpClient {
	constructor() {
		super();
	}

	/**
	 * Получить все чаты документа
	 */
	async getChatsByDocument(
		documentId: string,
		includeArchived: boolean = false
	): Promise<ChatSession[]> {
		const response = await this.instance.get<ChatSession[]>(
			`/api/chats/document/${documentId}`,
			{
				params: { includeArchived }
			}
		);
		return response.data;
	}

	/**
	 * Получить чат с сообщениями по ID
	 */
	async getChatById(chatId: string): Promise<ChatSessionWithMessages> {
		const response = await this.instance.get<ChatSessionWithMessages>(
			`/api/chats/${chatId}`
		);
		return response.data;
	}

	/**
	 * Создать новый чат
	 */
	async createChat(dto: CreateChatSession): Promise<ChatSession> {
		const response = await this.instance.post<ChatSession>('/api/chats', dto);
		return response.data;
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
}

export default new ChatAPI();
