import { createScopedChatsStore } from './createScopedChatsStore';

export function useDocumentChats() {
	const store = createScopedChatsStore('document');

	return {
		...store,
		loadChats: (documentId: string) => store.loadChats(documentId),
		createChat: (documentId: string, title?: string) => store.createChat(documentId, title),
	};
}
