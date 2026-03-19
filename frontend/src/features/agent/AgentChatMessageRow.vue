<template>
	<div class="chat-message" :class="`chat-message--${message.role}`">
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
				</div>
				<div v-if="message.stepNumber && message.toolCalls" class="agent-chat__tool-steps">
					<AgentToolStepCard
						v-for="(tc, idx) in parseToolCalls(message.toolCalls)"
						:key="`${message.id}-${idx}`"
						:tool-name="tc.toolName"
						:result="tc.result"
					/>
				</div>
				<template v-else>
					<div class="chat-message-content" v-html="renderMarkdown(message.content)"></div>
					<AgentChatAttachmentStrip v-if="attachmentList.length" :attachments="attachmentList" />
				</template>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Icon from '@/components/Icon.vue';
import type { ChatMessage } from '@/shared/api/ChatAPI';
import { parseMessageAttachments, parseToolCalls } from './agentChatUtils';
import AgentChatAttachmentStrip from './AgentChatAttachmentStrip.vue';
import AgentToolStepCard from './AgentToolStepCard.vue';

const props = defineProps<{
	message: ChatMessage;
	renderMarkdown: (text: string) => string;
}>();

const attachmentList = computed(() => parseMessageAttachments(props.message));
</script>

<style scoped>
.agent-chat__tool-steps {
	display: flex;
	flex-direction: column;
	gap: 12px;
}

</style>
