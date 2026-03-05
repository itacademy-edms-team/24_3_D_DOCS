import { createScopedChatsStore } from './createScopedChatsStore';

export function useGlobalChats() {
	const store = createScopedChatsStore('global');

	return {
		...store,
		loadChats: () => store.loadChats(),
		createChat: (title?: string) => store.createChat(null, title),
	};
}
