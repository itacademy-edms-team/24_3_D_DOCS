<template>
	<div v-if="attachments.length > 0" class="agent-chat-attachment-strip">
		<div
			v-for="a in attachments"
			:key="a.sourceSessionId"
			class="agent-chat-attachment-card"
		>
			<span class="agent-chat-attachment-name" :title="a.fileName">{{ a.fileName }}</span>
			<button
				type="button"
				class="button-ghost agent-chat-attachment-download"
				:disabled="busyId === a.sourceSessionId"
				@click="download(a)"
			>
				{{ busyId === a.sourceSessionId ? '…' : 'Скачать' }}
			</button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { ChatMessageAttachment } from '@/shared/api/ChatAPI';
import AIAPI from '@/shared/api/AIAPI';

defineProps<{
	attachments: ChatMessageAttachment[];
}>();

const busyId = ref<string | null>(null);

async function download(a: ChatMessageAttachment) {
	busyId.value = a.sourceSessionId;
	try {
		await AIAPI.downloadAgentSourceOriginal(a.sourceSessionId, a.fileName);
	} catch (e) {
		console.error('download failed', e);
	} finally {
		busyId.value = null;
	}
}
</script>

<style scoped>
.agent-chat-attachment-strip {
	display: flex;
	flex-direction: column;
	gap: 8px;
	margin-top: 8px;
}

.agent-chat-attachment-card {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 10px;
	padding: 8px 10px;
	border-radius: var(--chat-radius-sm, 8px);
	border: 1px solid var(--chat-border);
	background: var(--chat-bg, var(--bg-secondary));
	font-size: 13px;
}

.agent-chat-attachment-name {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	flex: 1;
	min-width: 0;
}

.agent-chat-attachment-download {
	flex-shrink: 0;
	padding: 4px 10px;
	font-size: 12px;
}
</style>
