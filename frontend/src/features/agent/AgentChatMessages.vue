<template>
	<div class="chat-messages-wrapper">
		<div class="chat-messages" ref="scrollContainerRef">
			<AgentChatMessageRow
				v-for="message in historyMessages"
				:key="message.id"
				:message="message"
				:hovered-message-id="hoveredMessageId"
				:render-markdown="renderMarkdown"
				@hover="(id) => (hoveredMessageId = id)"
				@copy="emit('copyMessage', $event)"
			/>

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
						<AgentChatAttachmentStrip
							v-if="pendingAttachments.length"
							:attachments="pendingAttachments"
						/>
						<div class="chat-message-content" v-html="renderMarkdown(pendingUserMessage)"></div>
					</div>
				</div>
			</div>

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
						<div v-if="!currentResponseFinalMessage" class="dots" aria-hidden="true">
							<span></span>
							<span></span>
							<span></span>
						</div>
					</div>
				</div>
			</div>

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
						<div class="chat-message-content" v-html="renderMarkdown(currentResponseFinalMessage)"></div>
					</div>
				</div>
			</div>

			<button
				v-if="showScrollDownButton"
				class="agent-chat__scroll-down-btn"
				type="button"
				@click="emit('scrollToBottom')"
				title="Прокрутить вниз"
			>
				<Icon name="arrow_downward" size="18" ariaLabel="Вниз" />
			</button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { inject } from 'vue';
import Icon from '@/components/Icon.vue';
import type { ChatMessage, ChatMessageAttachment } from '@/shared/api/ChatAPI';
import { agentChatScrollContainerKey } from './agentChatKeys';
import { getToolLabel, formatToolResult } from './agentChatUtils';
import AgentChatMessageRow from './AgentChatMessageRow.vue';
import AgentChatAttachmentStrip from './AgentChatAttachmentStrip.vue';

defineProps<{
	historyMessages: ChatMessage[];
	pendingUserMessage: string | null;
	pendingAttachments: ChatMessageAttachment[];
	renderMarkdown: (text: string) => string;
	isProcessing: boolean;
	liveToolCalls: Array<{ toolName: string; result?: string }>;
	showFinalResponseBlock: boolean;
	currentResponseFinalMessage: string;
	showScrollDownButton: boolean;
}>();

const hoveredMessageId = defineModel<string | null>('hoveredMessageId', { required: true });

const emit = defineEmits<{
	copyMessage: [content: string];
	scrollToBottom: [];
}>();

const scrollContainerRef = inject(agentChatScrollContainerKey)!;
</script>

<style scoped>
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
</style>
