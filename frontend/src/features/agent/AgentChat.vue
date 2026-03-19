<template>
	<div class="agent-chat chat-container">
		<!-- Header -->
		<div class="chat-header">
			<h3 class="chat-title">DDOCS AI</h3>
			<div style="display: flex; align-items: center; gap: 8px;">
				<div v-if="isProcessing" style="font-size: 13px; color: var(--chat-accent); font-weight: 500;">
					Обработка...
				</div>
				<button
					class="button-ghost chat-close-btn"
					@click="emit('close')"
					title="Закрыть панель"
					style="padding: 6px;"
				>
					<Icon name="close" size="18" ariaLabel="Закрыть" />
				</button>
			</div>
		</div>

		<!-- Chat tabs -->
		<div class="chat-tabs">
			<button
				@click="handleCreateChat"
				class="chat-tab chat-tab--new"
				title="Создать новый чат"
			>
				<Icon name="plus" size="18" ariaLabel="Создать новый чат" />
			</button>
			<button
				v-for="chat in chats"
				:key="chat.id"
				class="chat-tab"
				:class="{ 'chat-tab--active': activeChatId === chat.id }"
				@click="handleSwitchChat(chat.id)"
				@dblclick="handleStartRename(chat.id, chat.title)"
			>
				<span
					v-if="editingChatId !== chat.id"
					class="chat-tab-title"
					:title="chat.title"
				>
					{{ chat.title }}
				</span>
				<input
					v-else
					v-model="editingTitle"
					@blur="handleFinishRename"
					@keydown.enter="handleFinishRename"
					@keydown.esc="handleCancelRename"
					class="chat-tab-input"
					:ref="(el) => bindRenameInputRef(el, chat.id)"
				/>
				<button
					@click.stop="handleArchiveChat(chat.id)"
					class="chat-tab-close"
					title="Архивировать чат"
				>
					<Icon name="close" size="14" ariaLabel="Архивировать чат" />
				</button>
			</button>
		</div>

		<AgentChatMessages
			v-if="activeChat"
			:history-messages="visibleHistoryMessages"
			:pending-user-message="pendingUserMessage"
			:pending-attachments="pendingAttachmentCards"
			:render-markdown="renderMarkdown"
			:is-processing="isProcessing"
			:live-tool-calls="liveToolCalls"
			:show-final-response-block="showFinalResponseBlock"
			:current-response-final-message="currentResponse?.finalMessage ?? ''"
			:show-scroll-down-button="showScrollDownButton"
			@scroll-to-bottom="scrollToBottom"
		/>

		<div v-else class="chat-empty">
			<p>{{ props.scope === 'global' ? 'Создайте чат для начала работы с главным агентом' : 'Выберите чат или создайте новый' }}</p>
		</div>

		<div v-if="error" class="chat-error">{{ error }}</div>

		<!-- Archive section -->
		<div class="chat-archive" v-if="archivedChats.length > 0">
			<div
				class="chat-archive-header"
				@click="showArchive = !showArchive"
			>
				<span>Архив ({{ archivedChats.length }})</span>
				<span>{{ showArchive ? '▼' : '▶' }}</span>
			</div>
			<div v-if="showArchive" class="chat-archive-list">
				<div
					v-for="chat in archivedChats"
					:key="chat.id"
					class="chat-archive-item"
				>
					<span
						@click="handleRestoreChat(chat.id)"
						class="chat-archive-title"
						:title="chat.title"
					>
						{{ chat.title }}
					</span>
					<button
						@click="handleDeletePermanently(chat.id)"
						class="button-ghost"
						title="Удалить навсегда"
						style="padding: 4px;"
					>
						<Icon name="trash" size="16" ariaLabel="Удалить навсегда" />
					</button>
				</div>
			</div>
		</div>

		<AgentChatComposer
			v-model:user-message="userMessage"
			:active-chat-id="activeChatId"
			:is-processing="isProcessing"
			:show-attachment-ui="showAttachmentUi"
			:is-uploading-attachment="isUploadingAttachment"
			:pending-attachment="pendingAttachment"
			:has-selection="hasSelection"
			:start-line="props.startLine"
			:end-line="props.endLine"
			:input-placeholder="composerPlaceholder"
			@send-or-stop="handleSendOrStop"
			@attachment-selected="onAttachmentFileSelected"
			@clear-attachment="clearPendingAttachment"
			@clear-selection="clearSelection"
		/>
	</div>

</template>

<script setup lang="ts">
import { ref, shallowRef, computed, watch, onMounted, toRef } from 'vue';
import { useAgent } from '@/app/composables/useAgent';
import { useGlobalChats } from '@/app/composables/useGlobalChats';
import { useDocumentChats } from '@/app/composables/useDocumentChats';
import Icon from '@/components/Icon.vue';
import AgentChatMessages from './AgentChatMessages.vue';
import AgentChatComposer from './AgentChatComposer.vue';
import { useAgentChatAttachments } from './useAgentChatAttachments';
import { useAgentChatScroll } from './useAgentChatScroll';
import { useAgentChatTabs } from './useAgentChatTabs';
import '@/styles/chat-ui.css';
import 'katex/dist/katex.css';
import MarkdownIt from 'markdown-it';
import markdownItMark from 'markdown-it-mark';
import markdownItSub from 'markdown-it-sub';
import markdownItSup from 'markdown-it-sup';
import { renderLatex } from '@/utils/renderers/formulaRenderer';
import type { AgentRequestDTO } from '@/shared/api/AIAPI';

const DOCUMENT_REFRESH_TOOL_NAMES = new Set([
	'create_document',
	'rename_document',
	'delete_document',
	'propose_insert',
	'propose_delete',
	'propose_replace',
	'propose_document_changes',
]);

const md = new MarkdownIt({
	html: true,
	linkify: true,
	breaks: false,
});
md.use(markdownItSup);
md.use(markdownItSub);
md.use(markdownItMark);

function renderMarkdown(text: string): string {
	const processed = renderLatex(text);
	return md.render(processed);
}

interface Props {
	scope?: 'global' | 'document';
	documentId?: string | null;
	startLine?: number;
	endLine?: number;
}

const props = withDefaults(defineProps<Props>(), {
	scope: 'document'
});

const emit = defineEmits<{
	close: [];
	clearSelection: [];
	documentContentChanged: [];
}>();

const { isProcessing, currentResponse, steps: agentSteps, events: agentEvents, error: agentError, sendMessage, stop: stopAgent, reset: resetAgent } = useAgent();
const globalChatsStore = useGlobalChats();
const documentChatsStore = useDocumentChats();
const isGlobalScope = computed(() => props.scope === 'global');

const chats = computed(() =>
	isGlobalScope.value ? globalChatsStore.chats.value : documentChatsStore.chats.value
);
const archivedChats = computed(() =>
	isGlobalScope.value ? globalChatsStore.archivedChats.value : documentChatsStore.archivedChats.value
);
const activeChatId = computed(() =>
	isGlobalScope.value ? globalChatsStore.activeChatId.value : documentChatsStore.activeChatId.value
);
const activeChat = computed(() =>
	isGlobalScope.value ? globalChatsStore.activeChat.value : documentChatsStore.activeChat.value
);
const chatsError = computed(() =>
	isGlobalScope.value ? globalChatsStore.error.value : documentChatsStore.error.value
);

const userMessage = ref('');
const pendingUserMessage = ref<string | null>(null);
const processedDocumentChangeSteps = shallowRef<Set<number>>(new Set());

const scopeComputed = computed(() => props.scope);
const documentIdComputed = computed(() => props.documentId);

const {
	pendingAttachment,
	isUploadingAttachment,
	showAttachmentUi,
	pendingAttachmentCards,
	clearPendingAttachment,
	onAttachmentFileSelected,
} = useAgentChatAttachments({
	scope: scopeComputed,
	documentId: documentIdComputed,
	activeChatId,
});

const loadChatsForCurrentScope = async () => {
	if (isGlobalScope.value) {
		await globalChatsStore.loadChats();
		return;
	}
	if (props.documentId) {
		await documentChatsStore.loadChats(props.documentId);
	}
};

const {
	showArchive,
	editingChatId,
	editingTitle,
	bindRenameInputRef,
	handleCreateChat,
	handleSwitchChat,
	handleStartRename,
	handleFinishRename,
	handleCancelRename,
	handleArchiveChat,
	handleRestoreChat,
	handleDeletePermanently,
} = useAgentChatTabs({
	isGlobalScope,
	documentId: toRef(props, 'documentId'),
	globalChatsStore,
	documentChatsStore,
	loadChatsForCurrentScope,
	resetAgent,
	clearPendingAttachment,
	clearProcessedDocumentSteps: () => {
		processedDocumentChangeSteps.value = new Set();
	},
});

const visibleHistoryMessages = computed(() => {
	return activeChat.value?.messages ?? [];
});

const { messagesContainerRef, showScrollDownButton, scrollToBottom } = useAgentChatScroll({
	agentEvents,
	visibleHistoryMessages,
	getActiveChatMessages: () => activeChat.value?.messages,
});

const error = computed(() => agentError.value || chatsError.value);

// Все tool calls из live steps (во время обработки)
const liveToolCalls = computed(() => {
	const out: Array<{ toolName: string; result?: string }> = [];
	for (const step of agentSteps.value) {
		for (const tc of step.toolCalls ?? []) {
			out.push({ toolName: tc.toolName, result: tc.result });
		}
	}
	return out;
});

// Показывать блок «финальный ответ» только если его ещё нет в истории (чтобы не дублировать после loadChatById)
const showFinalResponseBlock = computed(() => {
	if (!currentResponse.value?.finalMessage || isProcessing.value) return false;
	const history = visibleHistoryMessages.value;
	const last = history.length > 0 ? history[history.length - 1] : null;
	return !(last?.role === 'assistant' && last?.content === currentResponse.value?.finalMessage);
});

const hasSelection = computed(() => {
	return props.startLine !== undefined && props.endLine !== undefined;
});

const composerPlaceholder = computed(() => {
	if (hasSelection.value) return 'Введите запрос для выделенного фрагмента...';
	return props.scope === 'global'
		? 'Управление документами: создание, удаление, список...'
		: 'Введите запрос агенту...';
});

const ensureGlobalChatExists = async () => {
	if (!isGlobalScope.value) return;
	if (chats.value.length > 0) return;
	await globalChatsStore.createChat();
};

// Load chats on mount
onMounted(async () => {
	await loadChatsForCurrentScope();
	await ensureGlobalChatExists();
});

// Watch for scope/documentId changes
watch(
	() => [props.scope, props.documentId] as const,
	async () => {
		globalChatsStore.reset();
		documentChatsStore.reset();
		resetAgent();
		clearPendingAttachment();
		await loadChatsForCurrentScope();
		await ensureGlobalChatExists();
	}
);

// Notify when agent mutates document metadata or proposes edits (streaming + late step updates).
watch(
	agentSteps,
	(steps) => {
		let changed = false;
		const next = new Set(processedDocumentChangeSteps.value);

		for (const step of steps) {
			if (next.has(step.stepNumber)) continue;

			const hasDocChanges =
				(step.documentChanges?.length ?? 0) > 0 ||
				step.toolCalls?.some(
					(tc) => DOCUMENT_REFRESH_TOOL_NAMES.has(tc.toolName) && !!tc.result
				) === true;

			if (hasDocChanges) {
				next.add(step.stepNumber);
				changed = true;
			}
		}

		if (changed) {
			processedDocumentChangeSteps.value = next;
			emit('documentContentChanged');
		}
	},
	{ deep: true }
);

const clearSelection = () => {
	emit('clearSelection');
};

const handleSend = async () => {
	if (isProcessing.value) return; // Если идет обработка, не отправляем (кнопка должна вызывать handleStop)
	if (!userMessage.value.trim() || !activeChatId.value) return;

	const text = userMessage.value.trim();
	processedDocumentChangeSteps.value = new Set();
	pendingUserMessage.value = text;
	userMessage.value = '';

	const request: AgentRequestDTO = {
		scope: props.scope,
		documentId: props.documentId ?? undefined,
		userMessage: text,
		startLine: props.startLine,
		endLine: props.endLine,
		chatId: activeChatId.value,
		...(pendingAttachment.value?.sessionId
			? { sourceSessionId: pendingAttachment.value.sessionId }
			: {}),
	};

	try {
		const payload = {
			...request,
			scope: request.scope === 'global' ? 'Global' : 'Document',
		} as unknown as AgentRequestDTO;
		await sendMessage(payload);
		clearPendingAttachment();

		// Обновить историю чата с сервера (без этого видны только последнее сообщение до перезагрузки)
		if (activeChatId.value) {
			if (isGlobalScope.value) {
				await globalChatsStore.loadChatById(activeChatId.value);
			} else {
				await documentChatsStore.loadChatById(activeChatId.value);
			}
		}
		await loadChatsForCurrentScope();
	} catch (err) {
		console.error('Error sending message to agent:', err);
		userMessage.value = text;
	} finally {
		pendingUserMessage.value = null;
	}
};

// Stop agent
const handleStop = () => {
	stopAgent();
};

function handleSendOrStop() {
	if (isProcessing.value) handleStop();
	else void handleSend();
}
</script>

<style scoped>
.agent-chat {
	height: 100%;
	display: flex;
	flex-direction: column;
}

.chat-close-btn:hover {
	color: var(--chat-accent);
	background: var(--bg-hover);
}
</style>
