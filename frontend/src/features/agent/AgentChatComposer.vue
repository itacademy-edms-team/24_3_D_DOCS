<template>
	<div class="chat-input-area">
		<input
			ref="fileInputRef"
			type="file"
			class="agent-chat__file-input"
			accept=".pdf,.txt,.md,.png,.jpg,.jpeg,.webp,.gif"
			@change="emit('attachmentSelected', $event)"
		/>
		<div v-if="showAttachmentUi && (pendingAttachment || isUploadingAttachment)" style="margin-bottom: 8px;">
			<div v-if="isUploadingAttachment" class="chat-selection-badge">Загрузка файла…</div>
			<div v-else-if="pendingAttachment" class="chat-pending-attachment-chip">
				<div class="chat-pending-attachment-chip__thumb">
					<img
						v-if="showImageThumb"
						:src="pendingAttachment.previewObjectUrl!"
						:alt="pendingAttachment.label"
						class="chat-pending-attachment-chip__img"
					/>
					<div v-else class="chat-pending-attachment-chip__placeholder">
						<Icon name="description" size="22" ariaLabel="" decorative />
						<span class="chat-pending-attachment-chip__kind">{{ pendingKindLabel }}</span>
					</div>
				</div>
				<span class="chat-pending-attachment-chip__name" :title="pendingAttachment.label">
					{{ pendingAttachment.label }}
				</span>
				<button
					type="button"
					@click="emit('clearAttachment')"
					class="chat-selection-remove chat-pending-attachment-chip__remove"
					title="Убрать вложение"
				>
					<Icon name="close" size="12" ariaLabel="Убрать вложение" />
				</button>
			</div>
		</div>
		<div v-if="hasSelection" style="margin-bottom: 8px;">
			<div class="chat-selection-badge">
				<span>Строки {{ startLine }}-{{ endLine }}</span>
				<button
					type="button"
					@click="emit('clearSelection')"
					class="chat-selection-remove"
					title="Убрать выделение"
				>
					<Icon name="close" size="12" ariaLabel="Убрать выделение" />
				</button>
			</div>
		</div>
		<div class="chat-input">
			<button
				v-if="showAttachmentUi"
				type="button"
				class="chat-attach-button"
				:disabled="isProcessing || !activeChatId || isUploadingAttachment"
				title="Прикрепить файл (PDF, текст, изображение)"
				aria-label="Прикрепить файл"
				@click="fileInputRef?.click()"
			>
				<Icon name="folder_open" size="20" ariaLabel="Прикрепить файл" />
			</button>
			<input
				:value="userMessage"
				:disabled="isProcessing || !activeChatId"
				:placeholder="inputPlaceholder"
				class="chat-input-field"
				type="text"
				@input="userMessage = ($event.target as HTMLInputElement).value"
				@keydown.enter="emit('sendOrStop')"
			/>
			<div class="chat-actions">
				<button
					type="button"
					@click="emit('sendOrStop')"
					:disabled="!isProcessing && (!userMessage.trim() || !activeChatId)"
					class="chat-send-button"
					:aria-label="isProcessing ? 'Остановить' : 'Отправить сообщение'"
				>
					<Icon
						:name="isProcessing ? 'stop' : 'send'"
						size="20"
						:ariaLabel="isProcessing ? 'Остановить' : 'Отправить'"
					/>
				</button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import Icon from '@/components/Icon.vue';
import type { PendingAgentAttachment } from './useAgentChatAttachments';
import {
	isImageAttachment,
	attachmentKindLabel,
} from './agentAttachmentKind';

const userMessage = defineModel<string>('userMessage', { required: true });

const props = defineProps<{
	activeChatId: string | null;
	isProcessing: boolean;
	showAttachmentUi: boolean;
	isUploadingAttachment: boolean;
	pendingAttachment: PendingAgentAttachment | null;
	hasSelection: boolean;
	startLine?: number;
	endLine?: number;
	inputPlaceholder: string;
}>();

const showImageThumb = computed(
	() =>
		!!props.pendingAttachment?.previewObjectUrl &&
		isImageAttachment(
			props.pendingAttachment.label,
			props.pendingAttachment.contentType
		)
);

const pendingKindLabel = computed(() => {
	const p = props.pendingAttachment;
	if (!p) return '';
	return attachmentKindLabel(p.label, p.contentType);
});

const emit = defineEmits<{
	sendOrStop: [];
	attachmentSelected: [ev: Event];
	clearAttachment: [];
	clearSelection: [];
}>();

const fileInputRef = ref<HTMLInputElement | null>(null);
</script>

<style scoped>
.agent-chat__file-input {
	position: absolute;
	width: 0;
	height: 0;
	opacity: 0;
	pointer-events: none;
}

.chat-attach-button {
	flex-shrink: 0;
	align-self: center;
	display: flex;
	align-items: center;
	justify-content: center;
	width: 44px;
	height: 44px;
	padding: 0;
	border-radius: var(--chat-radius-sm, 8px);
	border: 2px solid var(--chat-border);
	background: var(--chat-bg);
	color: var(--chat-foreground);
	cursor: pointer;
	transition: all var(--chat-transition, 0.2s ease);
}

.chat-attach-button:hover:not(:disabled) {
	border-color: var(--chat-accent);
	color: var(--chat-accent);
}

.chat-attach-button:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.chat-pending-attachment-chip {
	display: flex;
	align-items: center;
	gap: 10px;
	max-width: 100%;
	padding: 6px 8px 6px 6px;
	border-radius: var(--chat-radius-sm, 10px);
	border: 1px solid var(--chat-border);
	background: var(--chat-bg, var(--bg-secondary));
}

.chat-pending-attachment-chip__thumb {
	flex-shrink: 0;
	width: 48px;
	height: 48px;
	border-radius: 8px;
	overflow: hidden;
	background: var(--chat-assistant-bubble-solid, rgba(0, 0, 0, 0.06));
}

.chat-pending-attachment-chip__img {
	width: 100%;
	height: 100%;
	object-fit: cover;
	display: block;
}

.chat-pending-attachment-chip__placeholder {
	width: 100%;
	height: 100%;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 2px;
	color: var(--chat-accent, var(--accent));
}

.chat-pending-attachment-chip__kind {
	font-size: 9px;
	font-weight: 700;
	line-height: 1;
}

.chat-pending-attachment-chip__name {
	flex: 1;
	min-width: 0;
	font-size: 13px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.chat-pending-attachment-chip__remove {
	flex-shrink: 0;
}
</style>
