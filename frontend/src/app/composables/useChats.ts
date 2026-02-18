import { ref, computed } from 'vue';
import ChatAPI, { type ChatSession, type ChatSessionWithMessages, type ChatScope } from '@/shared/api/ChatAPI';

export function useChats() {
	const chats = ref<ChatSession[]>([]);
	const archivedChats = ref<ChatSession[]>([]);
	const activeChatId = ref<string | null>(null);
	const activeChat = ref<ChatSessionWithMessages | null>(null);
	const isLoading = ref(false);
	const error = ref<string | null>(null);

	/**
	 * Загрузить чаты по scope
	 * @param scope - 'global' или 'document'
	 * @param documentId - обязателен при scope='document'
	 */
	const loadChats = async (scope: ChatScope, documentId?: string | null) => {
		isLoading.value = true;
		error.value = null;

		try {
			const allChats = await ChatAPI.getChats(scope, documentId ?? undefined, true);
			chats.value = allChats.filter(c => !c.isArchived);
			archivedChats.value = allChats.filter(c => c.isArchived);

			if (!activeChatId.value && chats.value.length > 0) {
				activeChatId.value = chats.value[0].id;
				await loadChatById(chats.value[0].id);
			}
		} catch (err: any) {
			error.value = err.message || 'Ошибка при загрузке чатов';
			console.error('Error loading chats:', err);
		} finally {
			isLoading.value = false;
		}
	};

	/**
	 * Загрузить чаты документа (обёртка для scope=document)
	 */
	const loadChatsByDocument = async (documentId: string) => {
		await loadChats('document', documentId);
	};

	/**
	 * Загрузить чат с сообщениями по ID
	 */
	const loadChatById = async (chatId: string) => {
		isLoading.value = true;
		error.value = null;
		
		try {
			const chat = await ChatAPI.getChatById(chatId);
			activeChat.value = chat;
			activeChatId.value = chatId;
			
			// Обновляем информацию о чате в списках
			const chatIndex = chats.value.findIndex(c => c.id === chatId);
			if (chatIndex >= 0) {
				chats.value[chatIndex] = {
					...chats.value[chatIndex],
					title: chat.title,
					messageCount: chat.messages.length
				};
			} else {
				const archivedIndex = archivedChats.value.findIndex(c => c.id === chatId);
				if (archivedIndex >= 0) {
					archivedChats.value[archivedIndex] = {
						...archivedChats.value[archivedIndex],
						title: chat.title,
						messageCount: chat.messages.length
					};
				}
			}
		} catch (err: any) {
			error.value = err.message || 'Ошибка при загрузке чата';
			console.error('Error loading chat:', err);
		} finally {
			isLoading.value = false;
		}
	};

	/**
	 * Создать новый чат
	 * @param scope - 'global' или 'document'
	 * @param documentId - обязателен при scope='document'
	 */
	const createChat = async (scope: ChatScope, documentId?: string | null, title?: string) => {
		isLoading.value = true;
		error.value = null;

		try {
			const newChat = await ChatAPI.createChat({ scope, documentId, title });
			chats.value.unshift(newChat);
			activeChatId.value = newChat.id;
			activeChat.value = {
				...newChat,
				messages: []
			};
			return newChat;
		} catch (err: any) {
			error.value = err.message || 'Ошибка при создании чата';
			console.error('Error creating chat:', err);
			throw err;
		} finally {
			isLoading.value = false;
		}
	};

	/**
	 * Переключиться на чат
	 */
	const switchChat = async (chatId: string) => {
		if (activeChatId.value === chatId) return;
		await loadChatById(chatId);
	};

	/**
	 * Обновить название чата
	 */
	const updateChatTitle = async (chatId: string, title: string) => {
		error.value = null;
		
		try {
			const updated = await ChatAPI.updateChat(chatId, { title });
			
			// Обновляем в списке активных чатов
			const chatIndex = chats.value.findIndex(c => c.id === chatId);
			if (chatIndex >= 0) {
				chats.value[chatIndex] = updated;
			}
			
			// Обновляем в списке архивных чатов
			const archivedIndex = archivedChats.value.findIndex(c => c.id === chatId);
			if (archivedIndex >= 0) {
				archivedChats.value[archivedIndex] = updated;
			}
			
			// Обновляем активный чат
			if (activeChat.value && activeChat.value.id === chatId) {
				activeChat.value.title = updated.title;
			}
		} catch (err: any) {
			error.value = err.message || 'Ошибка при обновлении чата';
			console.error('Error updating chat title:', err);
			throw err;
		}
	};

	/**
	 * Переместить чат в архив
	 */
	const archiveChat = async (chatId: string) => {
		error.value = null;
		
		try {
			await ChatAPI.archiveChat(chatId);
			
			// Перемещаем из активных в архивные
			const chatIndex = chats.value.findIndex(c => c.id === chatId);
			if (chatIndex >= 0) {
				const chat = chats.value[chatIndex];
				chats.value.splice(chatIndex, 1);
				archivedChats.value.unshift({ ...chat, isArchived: true });
			}
			
			// Если это был активный чат, переключаемся на другой
			if (activeChatId.value === chatId) {
				if (chats.value.length > 0) {
					await switchChat(chats.value[0].id);
				} else {
					activeChatId.value = null;
					activeChat.value = null;
				}
			}
		} catch (err: any) {
			error.value = err.message || 'Ошибка при архивировании чата';
			console.error('Error archiving chat:', err);
			throw err;
		}
	};

	/**
	 * Восстановить чат из архива
	 */
	const restoreChat = async (chatId: string) => {
		error.value = null;
		
		try {
			await ChatAPI.restoreChat(chatId);
			
			// Перемещаем из архивных в активные
			const archivedIndex = archivedChats.value.findIndex(c => c.id === chatId);
			if (archivedIndex >= 0) {
				const chat = archivedChats.value[archivedIndex];
				archivedChats.value.splice(archivedIndex, 1);
				chats.value.unshift({ ...chat, isArchived: false });
			}
		} catch (err: any) {
			error.value = err.message || 'Ошибка при восстановлении чата';
			console.error('Error restoring chat:', err);
			throw err;
		}
	};

	/**
	 * Удалить чат навсегда
	 */
	const deleteChatPermanently = async (chatId: string) => {
		error.value = null;
		
		try {
			await ChatAPI.deleteChatPermanently(chatId);
			
			// Удаляем из списка архивных
			const archivedIndex = archivedChats.value.findIndex(c => c.id === chatId);
			if (archivedIndex >= 0) {
				archivedChats.value.splice(archivedIndex, 1);
			}
			
			// Если это был активный чат, очищаем
			if (activeChatId.value === chatId) {
				activeChatId.value = null;
				activeChat.value = null;
			}
		} catch (err: any) {
			error.value = err.message || 'Ошибка при удалении чата';
			console.error('Error deleting chat permanently:', err);
			throw err;
		}
	};

	/**
	 * Сбросить состояние
	 */
	const reset = () => {
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
		loadChatsByDocument,
		loadChatById,
		createChat,
		switchChat,
		updateChatTitle,
		archiveChat,
		restoreChat,
		deleteChatPermanently,
		reset
	};
}
