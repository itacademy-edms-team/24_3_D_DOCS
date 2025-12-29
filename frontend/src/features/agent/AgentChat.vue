<template>
	<div class="agent-chat chat-container">
		<!-- Header -->
		<div class="chat-header">
			<h3 class="chat-title">AI Агент</h3>
			<div style="display: flex; align-items: center; gap: 8px;">
				<div v-if="isProcessing" style="font-size: 13px; color: var(--chat-accent); font-weight: 500;">
					Обработка... (шагов: {{ steps.length }})
				</div>
				<button
					class="button-ghost"
					@click="compactMode = !compactMode"
					:title="compactMode ? 'Компактный вид: включен' : 'Компактный вид: выключен'"
					style="font-size: 13px; padding: 6px 10px;"
				>
					{{ compactMode ? 'Компакт' : 'Развернутый' }}
				</button>
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
				@contextmenu.prevent="handleContextMenu($event, chat.id)"
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
		<div class="chat-messages" v-if="activeChat">
			<!-- History messages -->
			<div
				v-for="message in activeChat.messages"
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
							<span v-if="message.stepNumber" style="font-size: 12px; background: var(--chat-assistant-bubble-solid); padding: 2px 8px; border-radius: 12px; margin-left: 8px;">
								Шаг {{ message.stepNumber }}
							</span>
							<div
								v-if="hoveredMessageId === message.id"
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
						<div class="chat-message-content" v-html="renderMarkdown(message.content)"></div>
					</div>
				</div>
			</div>

			<!-- Typing indicator -->
			<div v-if="isProcessing && !steps.length" class="chat-message chat-message--assistant">
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
						<div class="dots" aria-hidden="true">
							<span></span>
							<span></span>
							<span></span>
						</div>
					</div>
				</div>
			</div>

			<!-- Current processing steps (compact inline visualizers) -->
			<div class="agent-chat__steps" role="list">
				<StepVisualizer
					v-for="step in steps"
					:key="`step-${step.stepNumber}`"
					:step="step"
					:compact="compactMode"
					:current-step-number="currentStep?.stepNumber"
					:class="{ 'agent-chat__step--active': step.stepNumber === currentStep?.stepNumber }"
					@toggle="(num, expanded) => handleToggleStep(num, expanded)"
					@accept="(num) => handleAcceptStepChanges(num)"
					@reject="(num) => handleRejectStepChanges(num)"
				/>
			</div>

			<!-- Final response -->
			<div v-if="currentResponse?.isComplete && currentResponse.finalMessage" class="agent-chat__response">
				<h4>Результат:</h4>
				<div class="agent-chat__final-message">{{ currentResponse.finalMessage }}</div>
			</div>
		</div>

		<div v-else class="chat-empty">
			<p>Выберите чат или создайте новый</p>
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
					<span
						v-if="props.pendingChangesByChat?.[chat.id]"
						style="color: var(--warning, #f59e0b); font-size: 12px; margin: 0 4px;"
						title="Есть неподтверждённые изменения агента"
					>
						●
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
				<textarea
					v-model="userMessage"
					:disabled="isProcessing || !activeChatId"
					:placeholder="hasSelection ? 'Введите запрос для выделенного фрагмента...' : 'Введите запрос агенту...'"
					class="chat-textarea"
					rows="3"
					@keydown.ctrl.enter="handleSend"
					@keydown.meta.enter="handleSend"
				></textarea>
				<div class="chat-actions">
					<button
						@click="handleSend"
						:disabled="isProcessing || !userMessage.trim() || !activeChatId"
						class="chat-send-button"
						:aria-label="isProcessing ? 'Остановить' : 'Отправить сообщение'"
					>
						<Icon :name="isProcessing ? 'stop' : 'send'" size="20" :ariaLabel="isProcessing ? 'Остановить' : 'Отправить'" />
					</button>
				</div>
			</div>
		</div>
	</div>

	<!-- Delete chat with pending changes modal -->
	<Modal
		v-model="showDeleteModal"
		title="Есть неподтверждённые изменения агента"
		size="sm"
	>
		<div class="agent-chat__delete-modal-body">
			<p>
				В этом чате есть изменения агента, которые ещё не были приняты или отклонены.
			</p>
			<p>
				Вы можете:
			</p>
			<ul>
				<li>перейти к чату и вручную принять или отклонить изменения;</li>
				<li>отменить все изменения этого чата и удалить его.</li>
			</ul>
		</div>
		<template #footer>
			<button
				class="agent-chat__delete-modal-btn agent-chat__delete-modal-btn--secondary"
				type="button"
				@click="handleDeleteModalGoToChat"
			>
				Перейти к чату
			</button>
			<button
				class="agent-chat__delete-modal-btn agent-chat__delete-modal-btn--danger"
				type="button"
				@click="handleDeleteModalDiscardAndDelete"
			>
				Отменить изменения и удалить
			</button>
		</template>
	</Modal>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, nextTick } from 'vue';
import { useAgent } from '@/app/composables/useAgent';
import { useChats } from '@/app/composables/useChats';
import Modal from '@/shared/ui/Modal/Modal.vue';
import StepVisualizer from './StepVisualizer.vue';
import Icon from '@/components/Icon.vue';
import '@/styles/chat-ui.css';
import type { AgentRequestDTO } from '@/shared/api/AIAPI';

// Simple markdown renderer
function renderMarkdown(text: string): string {
	return text
		.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
		.replace(/\*(.*?)\*/g, '<em>$1</em>')
		.replace(/`(.*?)`/g, '<code>$1</code>')
		.replace(/\n/g, '<br>')
		.replace(/\n\n/g, '</p><p>')
		.replace(/^/, '<p>')
		.replace(/$/, '</p>');
}

interface Props {
	documentId: string;
	startLine?: number;
	endLine?: number;
	pendingChangesByChat?: Record<string, boolean>;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	close: [];
	clearSelection: [];
	documentUpdated: [];
	documentChanged: [change: {
		stepNumber: number;
		operation: string;
		chatId: string;
		changes: Array<{ lineNumber: number; type: 'added' | 'deleted'; text?: string }>;
	}];
	acceptChanges: [stepNumber: number];
	rejectChanges: [stepNumber: number];
	discardChatChanges: [chatId: string];
}>();

const { isProcessing, steps, currentResponse, error: agentError, currentStep, sendMessage, reset: resetAgent, parseDocumentChanges } = useAgent();
const {
	chats,
	archivedChats,
	activeChatId,
	activeChat,
	isLoading: chatsLoading,
	error: chatsError,
	loadChats,
	createChat,
	switchChat,
	updateChatTitle,
	archiveChat,
	restoreChat,
	deleteChatPermanently,
	reset: resetChats
} = useChats();

const userMessage = ref('');
const showArchive = ref(false);
const editingChatId = ref<string | null>(null);
const editingTitle = ref('');
const renameInputRef = ref<HTMLInputElement | null>(null);
const hoveredMessageId = ref<string | null>(null);

const showDeleteModal = ref(false);
const chatIdToDelete = ref<string | null>(null);
// Compact mode preference persisted in localStorage
const COMPACT_KEY = 'agent_chat_compact_mode';
const compactMode = ref<boolean>(true);

// Restore preference on mount
onMounted(() => {
	const stored = localStorage.getItem(COMPACT_KEY);
	if (stored !== null) {
		compactMode.value = stored === 'true';
	}
});

watch(compactMode, (val) => {
	localStorage.setItem(COMPACT_KEY, String(val));
});

// Loading states for step actions
const acceptingSteps = ref<Set<number>>(new Set());
const rejectingSteps = ref<Set<number>>(new Set());
const acceptedSteps = ref<Set<number>>(new Set());
const rejectedSteps = ref<Set<number>>(new Set());

const error = computed(() => agentError.value || chatsError.value);

const hasSelection = computed(() => {
	return props.startLine !== undefined && props.endLine !== undefined;
});

// Accessibility helpers for keyboard interaction - ensure StepVisualizer is keyboard reachable
onMounted(() => {
	// nothing heavy, placeholder for potential focus management
});

// Load chats on mount
onMounted(async () => {
	await loadChats(props.documentId);
});

// Watch for documentId changes
watch(() => props.documentId, async (newDocId) => {
	if (newDocId) {
		resetChats();
		resetAgent();
		await loadChats(newDocId);
	}
});

// Watch for agent completion and emit documentUpdated event
watch(
	() => currentResponse.value?.isComplete,
	(isComplete) => {
		if (isComplete) {
			emit('documentUpdated');
		}
	}
);

// Watch for steps with document changes and emit documentChanged events
watch(
	() => steps.value,
	(newSteps) => {
		// Проверяем каждый новый шаг на наличие изменений документа
		for (const step of newSteps) {
			if (!step.toolCalls || step.toolCalls.length === 0) continue;
			
			// Проверяем, есть ли инструменты для изменения документа
			const hasDocumentTools = step.toolCalls.some(
				tc => tc.toolName === 'insert' || tc.toolName === 'edit' || tc.toolName === 'delete'
			);
			
			if (hasDocumentTools) {
				// Парсим изменения из toolCalls
				const parsedChanges = parseDocumentChanges(step);
				
				if (parsedChanges.length > 0) {
					// Определяем операцию (может быть несколько разных в одном шаге)
					const operations = new Set(parsedChanges.map(c => c.operation));
					const operation = operations.size === 1 
						? Array.from(operations)[0] 
						: 'update'; // Если смешанные операции, считаем update
					
					// Эмитим событие с изменениями
					emit('documentChanged', {
						stepNumber: step.stepNumber,
						operation,
						chatId: activeChatId.value ?? '',
						changes: parsedChanges.map(c => ({
							lineNumber: c.lineNumber,
							type: c.type,
							text: c.text,
						})),
					});
				}
			}
		}
	},
	{ deep: true }
);

// Watch for chat changes and reload messages
watch(activeChatId, async (newChatId) => {
	if (newChatId && activeChat.value) {
		// Reload chat to get latest messages
		await loadChats(props.documentId);
	}
});

const clearSelection = () => {
	emit('clearSelection');
};

const handleCreateChat = async () => {
	try {
		await createChat(props.documentId);
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

const handleContextMenu = (event: MouseEvent, chatId: string) => {
	// Можно добавить контекстное меню в будущем
	// Пока используем двойной клик для переименования
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
	// Если по этому чату есть неподтверждённые изменения агента — показываем кастомное модальное окно
	if (props.pendingChangesByChat?.[chatId]) {
		chatIdToDelete.value = chatId;
		showDeleteModal.value = true;
		return;
	}

	try {
		await deleteChatPermanently(chatId);
	} catch (err) {
		console.error('Error deleting chat permanently:', err);
	}
};

const handleDeleteModalClose = () => {
	showDeleteModal.value = false;
	chatIdToDelete.value = null;
};

const handleDeleteModalGoToChat = async () => {
	if (!chatIdToDelete.value) return;

	// Вариант 1: просто закрываем модалку и даём пользователю самому перейти и принять изменения
	// Здесь логично восстановить чат (он уже в архиве) и переключиться на него
	try {
		await restoreChat(chatIdToDelete.value);
		await switchChat(chatIdToDelete.value);
		showArchive.value = false;
	} catch (err) {
		console.error('Error restoring chat from delete modal:', err);
	} finally {
		handleDeleteModalClose();
	}
};

const handleDeleteModalDiscardAndDelete = async () => {
	if (!chatIdToDelete.value) return;

	const chatId = chatIdToDelete.value;

	// Сначала просим родителя откатить изменения агента по этому чату,
	// затем удаляем сам чат
	emit('discardChatChanges', chatId);

	try {
		await deleteChatPermanently(chatId);
	} catch (err) {
		console.error('Error deleting chat permanently after discard:', err);
	} finally {
		handleDeleteModalClose();
	}
};

const handleSend = async () => {
	if (!userMessage.value.trim() || isProcessing.value || !activeChatId.value) return;

	try {
		const request: AgentRequestDTO = {
			documentId: props.documentId,
			userMessage: userMessage.value.trim(),
			startLine: props.startLine,
			endLine: props.endLine,
			chatId: activeChatId.value
		};

		await sendMessage(request);
		userMessage.value = '';
		
		// Reload chat to get updated messages
		if (activeChatId.value) {
			await loadChats(props.documentId);
		}
	} catch (err) {
		console.error('Error sending message to agent:', err);
	}
};

// Check if step has document changes
const hasDocumentChanges = (step: typeof steps.value[0]): boolean => {
	if (!step.toolCalls || step.toolCalls.length === 0) return false;
	return step.toolCalls.some(
		tc => tc.toolName === 'insert' || tc.toolName === 'edit' || tc.toolName === 'delete'
	);
};

// Handle accept changes for a step
const handleAcceptStepChanges = async (stepNumber: number) => {
	if (acceptingSteps.value.has(stepNumber)) return; // Already processing

	try {
		acceptingSteps.value.add(stepNumber);
		rejectedSteps.value.delete(stepNumber); // Remove any previous rejected state
		emit('acceptChanges', stepNumber);
		// Small delay to show loading state and indicate success
		await new Promise(resolve => setTimeout(resolve, 300));
		acceptedSteps.value.add(stepNumber);
		// Remove success indicator after 2 seconds
		setTimeout(() => {
			acceptedSteps.value.delete(stepNumber);
		}, 2000);
	} finally {
		acceptingSteps.value.delete(stepNumber);
	}
};

// Handle reject changes for a step
const handleRejectStepChanges = async (stepNumber: number) => {
	if (rejectingSteps.value.has(stepNumber)) return; // Already processing

	if (confirm('Отменить все изменения этого шага? Изменения будут откачены в документе.')) {
		try {
			rejectingSteps.value.add(stepNumber);
			acceptedSteps.value.delete(stepNumber); // Remove any previous accepted state
			emit('rejectChanges', stepNumber);
			// Small delay to show loading state and indicate success
			await new Promise(resolve => setTimeout(resolve, 300));
			rejectedSteps.value.add(stepNumber);
			// Remove success indicator after 2 seconds
			setTimeout(() => {
				rejectedSteps.value.delete(stepNumber);
			}, 2000);
		} finally {
			rejectingSteps.value.delete(stepNumber);
		}
	}
};

// Handle toggling step expanded state (can be used for analytics or scrolling)
const handleToggleStep = (stepNumber: number, expanded: boolean) => {
	// If expanded, scroll step into view
	if (expanded) {
		nextTick(() => {
			const el = document.querySelector(`.step-visualizer:nth-child(${stepNumber})`);
			el && (el as HTMLElement).scrollIntoView({ behavior: 'smooth', block: 'center' });
		});
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
</script>

<style scoped>
/* Минимальные стили только для специфичных элементов компонента */
.agent-chat {
	height: 100%;
	display: flex;
	flex-direction: column;
}

/* Стили для специфичных элементов компонента */
.agent-chat__steps {
	display: flex;
	flex-direction: column;
	gap: 12px;
	padding: 0 16px;
}

.agent-chat__step--active {
	border-color: var(--chat-accent) !important;
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
</style>
