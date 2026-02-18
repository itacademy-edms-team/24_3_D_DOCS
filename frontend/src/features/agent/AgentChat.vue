<template>
	<div class="agent-chat chat-container">
		<!-- Header -->
		<div class="chat-header">
			<h3 class="chat-title">AI Помощник</h3>
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
					ref="renameInputRef"
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

		<!-- Messages area -->
		<div class="chat-messages-wrapper" v-if="activeChat">
			<div class="chat-messages" ref="messagesContainerRef">
				<!-- History messages -->
				<div
					v-for="message in visibleHistoryMessages"
					:key="message.id"
					class="chat-message"
					:class="`chat-message--${message.role}`"
					@mouseenter="hoveredMessageId = message.id"
					@mouseleave="hoveredMessageId = null"
				>
					<div class="chat-message-container">
						<div class="chat-avatar" :class="`chat-avatar--${message.role}`">
							<div class="chat-avatar-icon">
								<Icon :name="message.role === 'user' ? 'user' : 'assistant'" size="20" ariaLabel="avatar" />
							</div>
						</div>
						<div class="chat-bubble" :class="message.role">
							<div class="chat-message-header">
								<span class="chat-message-role">
									{{ message.role === 'user' ? 'Вы' : 'Агент' }}
								</span>
								<div
									v-if="hoveredMessageId === message.id && !message.stepNumber"
									class="chat-message-actions"
								>
									<button
										@click="copyMessage(message.content)"
										class="button-ghost"
										title="Копировать"
										style="padding: 4px;"
									>
										<Icon name="copy" size="16" ariaLabel="Копировать" />
									</button>
								</div>
							</div>
							<!-- Step (tool call) - инкапсулированный блок -->
							<div v-if="message.stepNumber && message.toolCalls" class="agent-chat__tool-steps">
								<div
									v-for="(tc, idx) in parseToolCalls(message.toolCalls)"
									:key="`${message.id}-${idx}`"
									class="maf-tool"
								>
									<div class="maf-tool__summary">
										<span>{{ getToolLabel(tc.toolName) }}</span>
										<span class="maf-tool__status maf-tool__status--accepted">✓</span>
									</div>
									<div class="maf-tool__result">{{ formatToolResult(tc.result) }}</div>
								</div>
							</div>
							<!-- Обычное сообщение -->
							<div v-else class="chat-message-content" v-html="renderMarkdown(message.content)"></div>
						</div>
					</div>
				</div>

			<!-- Сообщение пользователя, отправленное только что (пока не подгрузили историю) -->
			<div v-if="pendingUserMessage" class="chat-message chat-message--user">
				<div class="chat-message-container">
					<div class="chat-avatar chat-avatar--user">
						<div class="chat-avatar-icon">
							<Icon name="user" size="20" ariaLabel="Вы" />
						</div>
					</div>
					<div class="chat-bubble user">
						<div class="chat-message-header">
							<span class="chat-message-role">Вы</span>
						</div>
						<div class="chat-message-content" v-html="renderMarkdown(pendingUserMessage)"></div>
					</div>
				</div>
			</div>

			<!-- Live tool steps (during processing) + typing -->
			<div v-if="isProcessing" class="chat-message chat-message--assistant">
				<div class="chat-message-container">
					<div class="chat-avatar chat-avatar--assistant">
						<div class="chat-avatar-icon">
							<Icon name="assistant" size="20" ariaLabel="Агент" />
						</div>
					</div>
					<div class="chat-bubble assistant">
						<div class="chat-message-header">
							<span class="chat-message-role">Агент</span>
						</div>
						<div v-if="liveToolCalls.length > 0" class="agent-chat__tool-steps">
							<div
								v-for="(tc, idx) in liveToolCalls"
								:key="`live-tc-${idx}`"
								class="maf-tool"
							>
								<div class="maf-tool__summary">
									<span>{{ getToolLabel(tc.toolName) }}</span>
									<span class="maf-tool__status maf-tool__status--accepted">✓</span>
								</div>
								<div class="maf-tool__result">{{ formatToolResult(tc.result) }}</div>
							</div>
						</div>
						<div v-if="!currentResponse?.finalMessage" class="dots" aria-hidden="true">
							<span></span>
							<span></span>
							<span></span>
						</div>
					</div>
				</div>
			</div>

			<!-- Final agent response (when streaming completes before chat reload) -->
			<div v-if="showFinalResponseBlock" class="chat-message chat-message--assistant">
				<div class="chat-message-container">
					<div class="chat-avatar chat-avatar--assistant">
						<div class="chat-avatar-icon">
							<Icon name="assistant" size="20" ariaLabel="Агент" />
						</div>
					</div>
					<div class="chat-bubble assistant">
						<div class="chat-message-header">
							<span class="chat-message-role">Агент</span>
						</div>
						<div class="chat-message-content" v-html="renderMarkdown(currentResponse.finalMessage)"></div>
					</div>
				</div>
			</div>
			
			<!-- Scroll to bottom button -->
			<button
				v-if="showScrollDownButton"
				class="agent-chat__scroll-down-btn"
				@click="scrollToBottom"
				title="Прокрутить вниз"
			>
				<Icon name="arrow_downward" size="18" ariaLabel="Вниз" />
			</button>
			</div>
		</div>

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

		<!-- Input area -->
		<div class="chat-input-area">
			<!-- Selection badges -->
			<div v-if="hasSelection" style="margin-bottom: 8px;">
				<div class="chat-selection-badge">
					<span>
						Строки {{ props.startLine }}-{{ props.endLine }}
					</span>
					<button
						@click="clearSelection"
						class="chat-selection-remove"
						title="Убрать выделение"
					>
						<Icon name="close" size="12" ariaLabel="Убрать выделение" />
					</button>
				</div>
			</div>
			<div class="chat-input">
				<input
					v-model="userMessage"
					:disabled="isProcessing || !activeChatId"
					:placeholder="hasSelection ? 'Введите запрос для выделенного фрагмента...' : (props.scope === 'global' ? 'Управление документами: создание, удаление, список...' : 'Введите запрос агенту...')"
					class="chat-input-field"
					type="text"
					@keydown.enter="handleSend"
				/>
				<div class="chat-actions">
					<button
						@click="isProcessing ? handleStop() : handleSend()"
						:disabled="!isProcessing && (!userMessage.trim() || !activeChatId)"
						class="chat-send-button"
						:aria-label="isProcessing ? 'Остановить' : 'Отправить сообщение'"
					>
						<Icon :name="isProcessing ? 'stop' : 'send'" size="20" :ariaLabel="isProcessing ? 'Остановить' : 'Отправить'" />
					</button>
				</div>
			</div>
		</div>
	</div>

</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, nextTick } from 'vue';
import { useAgent } from '@/app/composables/useAgent';
import { useChats } from '@/app/composables/useChats';
import Icon from '@/components/Icon.vue';
import '@/styles/chat-ui.css';
import 'katex/dist/katex.css';
import MarkdownIt from 'markdown-it';
import markdownItMark from 'markdown-it-mark';
import markdownItSub from 'markdown-it-sub';
import markdownItSup from 'markdown-it-sup';
import { renderLatex } from '@/utils/renderers/formulaRenderer';
import type { AgentRequestDTO } from '@/shared/api/AIAPI';

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
	documentUpdated: [];
}>();

const { isProcessing, currentResponse, steps: agentSteps, events: agentEvents, error: agentError, sendMessage, stop: stopAgent, reset: resetAgent } = useAgent();
const {
	chats,
	archivedChats,
	activeChatId,
	activeChat,
	error: chatsError,
	loadChats,
	loadChatsByDocument,
	loadChatById,
	createChat,
	switchChat,
	updateChatTitle,
	archiveChat,
	restoreChat,
	deleteChatPermanently,
	reset: resetChats
} = useChats();

const userMessage = ref('');
const pendingUserMessage = ref<string | null>(null);
const showArchive = ref(false);
const editingChatId = ref<string | null>(null);
const editingTitle = ref('');
const renameInputRef = ref<HTMLInputElement | null>(null);
const hoveredMessageId = ref<string | null>(null);

// Auto-scroll state
const messagesContainerRef = ref<HTMLElement | null>(null);
const autoScrollEnabled = ref(true);
const showScrollDownButton = ref(false);

// Restore preference on mount
onMounted(() => {
	// Setup scroll listener
	setupScrollListener();
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

function parseToolCalls(toolCallsJson: string | undefined): Array<{ toolName: string; result?: string }> {
	if (!toolCallsJson) return [];
	try {
		const arr = JSON.parse(toolCallsJson);
		return Array.isArray(arr) ? arr.map((tc: any) => ({ toolName: tc.toolName ?? tc.tool_name ?? '', result: tc.result })) : [];
	} catch {
		return [];
	}
}

function getToolLabel(toolName: string): string {
	const labels: Record<string, string> = {
		list_documents: 'Получение списка документов',
		create_document: 'Создание документа',
		delete_document: 'Удаление документа',
	};
	return labels[toolName] ?? `Вызов ${toolName}`;
}

function formatToolResult(result: string | undefined): string {
	if (!result) return '—';
	try {
		const parsed = JSON.parse(result);
		if (Array.isArray(parsed)) return `Найдено документов: ${parsed.length}`;
		if (typeof parsed === 'object' && parsed !== null) {
			if (parsed.message) return String(parsed.message);
			if (parsed.deleted) return 'Документ удалён';
			if (parsed.created) return String(parsed.message ?? 'Документ создан');
		}
	} catch { /* fallback */ }
	return result.length > 200 ? result.slice(0, 200) + '…' : result;
}

const visibleHistoryMessages = computed(() => {
	return activeChat.value?.messages ?? [];
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

// Accessibility helpers for keyboard interaction
onMounted(() => {
	// placeholder for potential focus management
});

// Load chats on mount
onMounted(async () => {
	if (props.scope === 'global') {
		await loadChats('global');
	} else if (props.documentId) {
		await loadChats('document', props.documentId);
	}
});

// Watch for scope/documentId changes
watch(
	() => [props.scope, props.documentId] as const,
	async ([newScope, newDocId]) => {
		resetChats();
		resetAgent();
		if (newScope === 'global') {
			await loadChats('global');
		} else if (newDocId) {
			await loadChats('document', newDocId);
		}
	}
);

// Watch for agent completion and emit documentUpdated event
watch(
	() => currentResponse.value?.isComplete,
	(isComplete) => {
		if (isComplete) {
			emit('documentUpdated');
		}
	}
);

// Watch for chat changes and reload messages
watch(activeChatId, async (newChatId) => {
	if (newChatId && activeChat.value) {
		if (props.scope === 'global') await loadChats('global');
		else if (props.documentId) await loadChats('document', props.documentId);
	}
});

const clearSelection = () => {
	emit('clearSelection');
};

const handleCreateChat = async () => {
	try {
		await createChat(props.scope, props.documentId ?? null);
	} catch (err) {
		console.error('Error creating chat:', err);
	}
};

const handleSwitchChat = async (chatId: string) => {
	await switchChat(chatId);
	resetAgent();
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
			await updateChatTitle(editingChatId.value, editingTitle.value.trim());
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
		await archiveChat(chatId);
	} catch (err) {
		console.error('Error archiving chat:', err);
	}
};

const handleRestoreChat = async (chatId: string) => {
	try {
		await restoreChat(chatId);
		await switchChat(chatId);
	} catch (err) {
		console.error('Error restoring chat:', err);
	}
};

const handleDeletePermanently = async (chatId: string) => {
	try {
		await deleteChatPermanently(chatId);
	} catch (err) {
		console.error('Error deleting chat permanently:', err);
	}
};

const handleSend = async () => {
	if (isProcessing.value) return; // Если идет обработка, не отправляем (кнопка должна вызывать handleStop)
	if (!userMessage.value.trim() || !activeChatId.value) return;

	const text = userMessage.value.trim();
	pendingUserMessage.value = text;
	userMessage.value = '';

	const request: AgentRequestDTO = {
		scope: props.scope,
		documentId: props.documentId ?? undefined,
		userMessage: text,
		startLine: props.startLine,
		endLine: props.endLine,
		chatId: activeChatId.value,
	};
	// Backend expects scope as number: 0=global, 1=document
	const payload = { ...request } as Record<string, unknown>;
	if (payload.scope) {
		payload.scope = payload.scope === 'global' ? 0 : 1;
	}

	try {
		await sendMessage(payload as AgentRequestDTO);

		// Обновить историю чата с сервера (без этого видны только последнее сообщение до перезагрузки)
		if (activeChatId.value) {
			await loadChatById(activeChatId.value);
		}
		if (props.scope === 'global') {
			await loadChats('global');
		} else if (props.documentId) {
			await loadChats('document', props.documentId);
		}
	} catch (err) {
		console.error('Error sending message to agent:', err);
		userMessage.value = text;
	} finally {
		pendingUserMessage.value = null;
	}
};

// Copy message to clipboard
const copyMessage = async (content: string) => {
	try {
		await navigator.clipboard.writeText(content);
		// Could add a toast notification here
	} catch (err) {
		console.error('Failed to copy message:', err);
		// Fallback for older browsers
		const textArea = document.createElement('textarea');
		textArea.value = content;
		document.body.appendChild(textArea);
		textArea.select();
		document.execCommand('copy');
		document.body.removeChild(textArea);
	}
};

// Stop agent
const handleStop = () => {
	stopAgent();
};

// Auto-scroll functions
const setupScrollListener = () => {
	nextTick(() => {
		const container = messagesContainerRef.value;
		if (!container) return;
		
		container.addEventListener('scroll', () => {
			checkScrollPosition();
		});
	});
};

const checkScrollPosition = () => {
	const container = messagesContainerRef.value;
	if (!container) return;
	
	const { scrollTop, scrollHeight, clientHeight } = container;
	const isNearBottom = scrollHeight - scrollTop - clientHeight < 100; // 100px threshold
	
	if (isNearBottom) {
		autoScrollEnabled.value = true;
		showScrollDownButton.value = false;
	} else {
		autoScrollEnabled.value = false;
		showScrollDownButton.value = true;
	}
};

const scrollToBottom = () => {
	const container = messagesContainerRef.value;
	if (!container) return;
	
	container.scrollTo({
		top: container.scrollHeight,
		behavior: 'smooth'
	});
	
	// Enable auto-scroll after smooth scroll completes
	setTimeout(() => {
		autoScrollEnabled.value = true;
		showScrollDownButton.value = false;
	}, 500);
};

// Auto-scroll when new events or messages arrive
watch([agentEvents, visibleHistoryMessages], () => {
	if (autoScrollEnabled.value) {
		nextTick(() => {
			const container = messagesContainerRef.value;
			if (container) {
				container.scrollTop = container.scrollHeight;
			}
		});
	}
}, { deep: true });

// Auto-scroll when new messages are loaded
watch(() => activeChat.value?.messages, () => {
	if (autoScrollEnabled.value) {
		nextTick(() => {
			const container = messagesContainerRef.value;
			if (container) {
				container.scrollTop = container.scrollHeight;
			}
		});
	}
}, { deep: true });
</script>

<style scoped>
/* Минимальные стили только для специфичных элементов компонента */
.agent-chat {
	height: 100%;
	display: flex;
	flex-direction: column;
}

/* Tool steps (инкапсулированные вызовы инструментов) */
.agent-chat__tool-steps {
	display: flex;
	flex-direction: column;
	gap: 12px;
}

/* MAF events */
.agent-chat__maf-events {
	display: flex;
	flex-direction: column;
	gap: 12px;
	padding: 8px 16px 16px;
}

.agent-chat__maf-event {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.maf-tool {
	border: 1px solid var(--chat-border);
	border-radius: 12px;
	background: var(--chat-assistant-bubble-solid);
	padding: 10px 12px;
}

.maf-tool__summary {
	cursor: pointer;
	font-weight: 600;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
}

.maf-tool__result {
	margin-top: 8px;
	color: var(--chat-foreground);
}

.maf-tool__actions {
	display: flex;
	gap: 8px;
	margin-top: 10px;
}

.maf-tool__btn {
	padding: 6px 12px;
	border-radius: 8px;
	border: 1px solid var(--chat-border);
	cursor: pointer;
	font-size: 13px;
	font-weight: 600;
	transition: all var(--chat-transition);
}

.maf-tool__btn--accept {
	background: rgba(34, 197, 94, 0.12);
	color: rgb(34, 197, 94);
}

.maf-tool__btn--reject {
	background: rgba(239, 68, 68, 0.12);
	color: rgb(239, 68, 68);
}

.maf-tool__status {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	border-radius: 50%;
	font-size: 12px;
	font-weight: 700;
}

.maf-tool__status--accepted {
	background: rgba(34, 197, 94, 0.2);
	color: rgb(34, 197, 94);
}

.maf-tool__status--rejected {
	background: rgba(239, 68, 68, 0.2);
	color: rgb(239, 68, 68);
}

.agent-chat__delete-modal-body {
	display: flex;
	flex-direction: column;
	gap: 12px;
	font-size: 15px;
	color: var(--chat-foreground);
}

.agent-chat__delete-modal-body ul {
	padding-left: 20px;
	margin: 8px 0 0 0;
}

.agent-chat__delete-modal-btn {
	padding: 10px 20px;
	border-radius: 10px;
	border: 1px solid transparent;
	cursor: pointer;
	font-size: 14px;
	font-weight: 500;
	transition: all var(--chat-transition);
}

.agent-chat__delete-modal-btn--secondary {
	background: var(--chat-assistant-bubble-solid);
	color: var(--chat-foreground);
	border-color: var(--chat-border);
}

.agent-chat__delete-modal-btn--secondary:hover {
	background: var(--chat-border);
}

.agent-chat__delete-modal-btn--danger {
	background: var(--danger, #ef4444);
	color: white;
}

.agent-chat__delete-modal-btn--danger:hover {
	opacity: 0.9;
}
.chat-close-btn:hover {
	color: var(--chat-accent);
	background: var(--bg-hover);
}

.agent-chat__scroll-down-btn {
	position: absolute;
	bottom: 20px;
	right: 20px;
	width: 40px;
	height: 40px;
	border-radius: 50%;
	background: var(--chat-accent);
	color: white;
	border: none;
	cursor: pointer;
	display: flex;
	align-items: center;
	justify-content: center;
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
	transition: all var(--chat-transition);
	z-index: 100;
	animation: fadeIn 0.2s ease-out;
}

@keyframes fadeIn {
	from {
		opacity: 0;
		transform: translateY(10px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

.agent-chat__scroll-down-btn:hover {
	transform: scale(1.1);
	box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
}

.agent-chat__scroll-down-btn:active {
	transform: scale(0.95);
}

.chat-messages-wrapper {
	flex: 1;
	position: relative;
	overflow: hidden;
	display: flex;
	flex-direction: column;
}

.chat-messages {
	flex: 1;
	overflow-y: auto;
	padding: 20px 16px;
	display: flex;
	flex-direction: column;
	gap: 20px;
	scroll-behavior: smooth;
	position: relative;
}
</style>
