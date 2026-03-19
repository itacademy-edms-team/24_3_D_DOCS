<template>
	<div v-if="attachments.length > 0" class="agent-chat-attachment-strip">
		<AgentChatAttachmentTile
			v-for="a in attachments"
			:key="a.sourceSessionId"
			:source-session-id="a.sourceSessionId"
			:file-name="a.fileName"
			:content-type="a.contentType"
			:preview-object-url="a.previewObjectUrl ?? null"
		/>
	</div>
</template>

<script setup lang="ts">
import type { ChatMessageAttachment } from '@/shared/api/ChatAPI';
import AgentChatAttachmentTile from './AgentChatAttachmentTile.vue';

defineProps<{
	attachments: ChatMessageAttachment[];
}>();
</script>

<style scoped>
.agent-chat-attachment-strip {
	display: flex;
	flex-direction: row;
	flex-wrap: wrap;
	gap: 8px;
	margin-top: 8px;
}
</style>
