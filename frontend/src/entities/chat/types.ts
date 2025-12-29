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
