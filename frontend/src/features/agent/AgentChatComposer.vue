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
			<div v-else-if="pendingAttachment" class="chat-selection-badge">
				<span :title="pendingAttachment.label">Вложение: {{ pendingAttachment.label }}</span>
				<button
					type="button"
					@click="emit('clearAttachment')"
					class="chat-selection-remove"
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
import { ref } from 'vue';
import Icon from '@/components/Icon.vue';

const userMessage = defineModel<string>('userMessage', { required: true });

defineProps<{
	activeChatId: string | null;
	isProcessing: boolean;
	showAttachmentUi: boolean;
	isUploadingAttachment: boolean;
	pendingAttachment: { label: string } | null;
	hasSelection: boolean;
	startLine?: number;
	endLine?: number;
	inputPlaceholder: string;
}>();

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
</style>
