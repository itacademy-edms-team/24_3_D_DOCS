import { ref, nextTick, type Ref, type ComputedRef, type ComponentPublicInstance } from 'vue';
import { useGlobalChats } from '@/app/composables/useGlobalChats';
import { useDocumentChats } from '@/app/composables/useDocumentChats';

type GlobalChatsStore = ReturnType<typeof useGlobalChats>;
type DocumentChatsStore = ReturnType<typeof useDocumentChats>;

export function useAgentChatTabs(opts: {
	isGlobalScope: ComputedRef<boolean>;
	documentId: Ref<string | null | undefined>;
	globalChatsStore: GlobalChatsStore;
	documentChatsStore: DocumentChatsStore;
	loadChatsForCurrentScope: () => Promise<void>;
	resetAgent: () => void;
	clearPendingAttachment: () => void;
	clearProcessedDocumentSteps: () => void;
}) {
	const showArchive = ref(false);
	const editingChatId = ref<string | null>(null);
	const editingTitle = ref('');
	const renameInputRef = ref<HTMLInputElement | null>(null);

	function bindRenameInputRef(
		el: Element | ComponentPublicInstance | null,
		chatId: string
	) {
		if (el instanceof HTMLInputElement) {
			if (editingChatId.value === chatId) renameInputRef.value = el;
		} else {
			renameInputRef.value = null;
		}
	}

	const handleCreateChat = async () => {
		try {
			if (opts.isGlobalScope.value) {
				await opts.globalChatsStore.createChat();
			} else if (opts.documentId.value) {
				await opts.documentChatsStore.createChat(opts.documentId.value);
			}
			await opts.loadChatsForCurrentScope();
		} catch (err) {
			console.error('Error creating chat:', err);
		}
	};

	const handleSwitchChat = async (chatId: string) => {
		if (opts.isGlobalScope.value) {
			await opts.globalChatsStore.switchChat(chatId);
		} else {
			await opts.documentChatsStore.switchChat(chatId);
		}
		opts.resetAgent();
		opts.clearPendingAttachment();
		opts.clearProcessedDocumentSteps();
	};

	const handleStartRename = (chatId: string, currentTitle: string) => {
		editingChatId.value = chatId;
		editingTitle.value = currentTitle;
		nextTick(() => {
			renameInputRef.value?.focus();
			renameInputRef.value?.select();
		});
	};

	const handleFinishRename = async () => {
		if (editingChatId.value && editingTitle.value.trim()) {
			try {
				if (opts.isGlobalScope.value) {
					await opts.globalChatsStore.updateChatTitle(
						editingChatId.value,
						editingTitle.value.trim()
					);
				} else {
					await opts.documentChatsStore.updateChatTitle(
						editingChatId.value,
						editingTitle.value.trim()
					);
				}
			} catch (err) {
				console.error('Error renaming chat:', err);
			}
		}
		editingChatId.value = null;
		editingTitle.value = '';
	};

	const handleCancelRename = () => {
		editingChatId.value = null;
		editingTitle.value = '';
	};

	const handleArchiveChat = async (chatId: string) => {
		try {
			if (opts.isGlobalScope.value) {
				await opts.globalChatsStore.archiveChat(chatId);
			} else {
				await opts.documentChatsStore.archiveChat(chatId);
			}
		} catch (err) {
			console.error('Error archiving chat:', err);
		}
	};

	const handleRestoreChat = async (chatId: string) => {
		try {
			if (opts.isGlobalScope.value) {
				await opts.globalChatsStore.restoreChat(chatId);
				await opts.globalChatsStore.switchChat(chatId);
			} else {
				await opts.documentChatsStore.restoreChat(chatId);
				await opts.documentChatsStore.switchChat(chatId);
			}
		} catch (err) {
			console.error('Error restoring chat:', err);
		}
	};

	const handleDeletePermanently = async (chatId: string) => {
		try {
			if (opts.isGlobalScope.value) {
				await opts.globalChatsStore.deleteChatPermanently(chatId);
			} else {
				await opts.documentChatsStore.deleteChatPermanently(chatId);
			}
		} catch (err) {
			console.error('Error deleting chat permanently:', err);
		}
	};

	return {
		showArchive,
		editingChatId,
		editingTitle,
		renameInputRef,
		bindRenameInputRef,
		handleCreateChat,
		handleSwitchChat,
		handleStartRename,
		handleFinishRename,
		handleCancelRename,
		handleArchiveChat,
		handleRestoreChat,
		handleDeletePermanently,
	};
}
