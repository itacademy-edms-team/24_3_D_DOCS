import { ref } from 'vue';
import ChatAPI, { type ChatSession, type ChatSessionWithMessages } from '@/shared/api/ChatAPI';

export function useGlobalChats() {
	const chats = ref<ChatSession[]>([]);
	const archivedChats = ref<ChatSession[]>([]);
	const activeChatId = ref<string | null>(null);
	const activeChat = ref<ChatSessionWithMessages | null>(null);
	const isLoading = ref(false);
	const error = ref<string | null>(null);
	let loadChatsRequestId = 0;
	let loadChatRequestId = 0;

	const loadChats = async () => {
		const requestId = ++loadChatsRequestId;
		isLoading.value = true;
		error.value = null;

		try {
			const allChats = await ChatAPI.getChats('global', undefined, true);
			if (requestId !== loadChatsRequestId) return;

			chats.value = allChats.filter((c) => !c.isArchived);
			archivedChats.value = allChats.filter((c) => c.isArchived);

			const hasCurrentActive =
				!!activeChatId.value && chats.value.some((c) => c.id === activeChatId.value);
			if (!hasCurrentActive) {
				activeChatId.value = chats.value.length > 0 ? chats.value[0].id : null;
				activeChat.value = null;
			}

			if (activeChatId.value) {
				await loadChatById(activeChatId.value, { silent: true });
			}
		} catch (err: any) {
			if (requestId !== loadChatsRequestId) return;
			error.value = err.message || 'Ошибка при загрузке чатов';
		} finally {
			if (requestId === loadChatsRequestId) {
				isLoading.value = false;
			}
		}
	};

	const loadChatById = async (chatId: string, options?: { silent?: boolean }) => {
		const requestId = ++loadChatRequestId;
		if (!options?.silent) isLoading.value = true;
		error.value = null;

		try {
			const chat = await ChatAPI.getChatById(chatId);
			if (requestId !== loadChatRequestId) return;

			if (chat.scope !== 'global') {
				return;
			}

			activeChat.value = chat;
			activeChatId.value = chatId;

			const chatIndex = chats.value.findIndex((c) => c.id === chatId);
			if (chatIndex >= 0) {
				chats.value[chatIndex] = {
					...chats.value[chatIndex],
					title: chat.title,
					messageCount: chat.messages.length,
				};
			}
		} catch (err: any) {
			if (requestId !== loadChatRequestId) return;
			error.value = err.message || 'Ошибка при загрузке чата';
		} finally {
			if (!options?.silent && requestId === loadChatRequestId) {
				isLoading.value = false;
			}
		}
	};

	const createChat = async (title?: string) => {
		isLoading.value = true;
		error.value = null;
		try {
			const newChat = await ChatAPI.createChat({ scope: 'global', documentId: null, title });
			loadChatsRequestId++;
			chats.value = [newChat, ...chats.value.filter((c) => c.id !== newChat.id)];
			activeChatId.value = newChat.id;
			activeChat.value = { ...newChat, messages: [] };
			await loadChatById(newChat.id, { silent: true });
			return newChat;
		} catch (err: any) {
			error.value = err.message || 'Ошибка при создании чата';
			throw err;
		} finally {
			isLoading.value = false;
		}
	};

	const switchChat = async (chatId: string) => {
		if (activeChatId.value === chatId) return;
		await loadChatById(chatId);
	};

	const updateChatTitle = async (chatId: string, title: string) => {
		error.value = null;
		const updated = await ChatAPI.updateChat(chatId, { title });

		const chatIndex = chats.value.findIndex((c) => c.id === chatId);
		if (chatIndex >= 0) chats.value[chatIndex] = updated;
		const archivedIndex = archivedChats.value.findIndex((c) => c.id === chatId);
		if (archivedIndex >= 0) archivedChats.value[archivedIndex] = updated;
		if (activeChat.value && activeChat.value.id === chatId) activeChat.value.title = updated.title;
	};

	const archiveChat = async (chatId: string) => {
		await ChatAPI.archiveChat(chatId);
		const chatIndex = chats.value.findIndex((c) => c.id === chatId);
		if (chatIndex >= 0) {
			const chat = chats.value[chatIndex];
			chats.value.splice(chatIndex, 1);
			archivedChats.value.unshift({ ...chat, isArchived: true });
		}

		if (activeChatId.value === chatId) {
			if (chats.value.length > 0) await switchChat(chats.value[0].id);
			else {
				activeChatId.value = null;
				activeChat.value = null;
			}
		}
	};

	const restoreChat = async (chatId: string) => {
		await ChatAPI.restoreChat(chatId);
		const archivedIndex = archivedChats.value.findIndex((c) => c.id === chatId);
		if (archivedIndex >= 0) {
			const chat = archivedChats.value[archivedIndex];
			archivedChats.value.splice(archivedIndex, 1);
			chats.value.unshift({ ...chat, isArchived: false });
		}
	};

	const deleteChatPermanently = async (chatId: string) => {
		await ChatAPI.deleteChatPermanently(chatId);
		const archivedIndex = archivedChats.value.findIndex((c) => c.id === chatId);
		if (archivedIndex >= 0) archivedChats.value.splice(archivedIndex, 1);
		if (activeChatId.value === chatId) {
			activeChatId.value = null;
			activeChat.value = null;
		}
	};

	const reset = () => {
		loadChatsRequestId++;
		loadChatRequestId++;
		chats.value = [];
		archivedChats.value = [];
		activeChatId.value = null;
		activeChat.value = null;
		error.value = null;
	};

	return {
		chats,
		archivedChats,
		activeChatId,
		activeChat,
		isLoading,
		error,
		loadChats,
		loadChatById,
		createChat,
		switchChat,
		updateChatTitle,
		archiveChat,
		restoreChat,
		deleteChatPermanently,
		reset,
	};
}
