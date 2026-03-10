<template>
	<div
		class="chat-message"
		:class="`chat-message--${message.role}`"
		@mouseenter="emit('hover', message.id)"
		@mouseleave="emit('hover', null)"
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
							type="button"
							@click="emit('copy', message.content)"
							class="button-ghost"
							title="Копировать"
							style="padding: 4px;"
						>
							<Icon name="copy" size="16" ariaLabel="Копировать" />
						</button>
					</div>
				</div>
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
				<template v-else>
					<AgentChatAttachmentStrip v-if="attachmentList.length" :attachments="attachmentList" />
					<div class="chat-message-content" v-html="renderMarkdown(message.content)"></div>
				</template>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Icon from '@/components/Icon.vue';
import type { ChatMessage } from '@/shared/api/ChatAPI';
import {
	parseMessageAttachments,
	parseToolCalls,
	getToolLabel,
	formatToolResult,
} from './agentChatUtils';
import AgentChatAttachmentStrip from './AgentChatAttachmentStrip.vue';

const props = defineProps<{
	message: ChatMessage;
	hoveredMessageId: string | null;
	renderMarkdown: (text: string) => string;
}>();

const emit = defineEmits<{
	hover: [id: string | null];
	copy: [content: string];
}>();

const attachmentList = computed(() => parseMessageAttachments(props.message));
</script>

<style scoped>
.agent-chat__tool-steps {
	display: flex;
	flex-direction: column;
	gap: 12px;
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
</style>
