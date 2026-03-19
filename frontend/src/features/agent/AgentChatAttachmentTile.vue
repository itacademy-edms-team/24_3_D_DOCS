<template>
	<div class="agent-chat-attachment-tile">
		<div class="agent-chat-attachment-tile__thumb">
			<img
				v-if="imageSrc"
				:src="imageSrc"
				:alt="fileName"
				class="agent-chat-attachment-tile__img"
			/>
			<div v-else class="agent-chat-attachment-tile__placeholder">
				<Icon name="description" size="28" ariaLabel="" decorative />
				<span class="agent-chat-attachment-tile__kind">{{ kindLabel }}</span>
			</div>
		</div>
		<div class="agent-chat-attachment-tile__body">
			<span class="agent-chat-attachment-tile__name" :title="fileName">{{ fileName }}</span>
			<button
				type="button"
				class="agent-chat-attachment-tile__download"
				:disabled="busy"
				:title="busy ? 'Загрузка…' : 'Скачать'"
				@click="download"
			>
				{{ busy ? '…' : 'Скачать' }}
			</button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import Icon from '@/components/Icon.vue';
import AIAPI from '@/shared/api/AIAPI';
import {
	isImageAttachment,
	attachmentKindLabel,
} from './agentAttachmentKind';

const props = defineProps<{
	sourceSessionId: string;
	fileName: string;
	contentType: string;
	/** Если задан (ожидающее сообщение), не качаем файл и не отзываем URL */
	previewObjectUrl?: string | null;
}>();

const busy = ref(false);
const imageSrc = ref<string | null>(null);
let objectUrl: string | null = null;

const kindLabel = computed(() =>
	attachmentKindLabel(props.fileName, props.contentType)
);

const shouldLoadImage = computed(() =>
	isImageAttachment(props.fileName, props.contentType)
);

function revokePreview() {
	if (objectUrl) {
		URL.revokeObjectURL(objectUrl);
		objectUrl = null;
	}
	imageSrc.value = null;
}

onMounted(async () => {
	if (!shouldLoadImage.value) return;
	if (props.previewObjectUrl) {
		imageSrc.value = props.previewObjectUrl;
		return;
	}
	try {
		const blob = await AIAPI.fetchAgentSourceOriginalBlob(props.sourceSessionId);
		objectUrl = URL.createObjectURL(blob);
		imageSrc.value = objectUrl;
	} catch {
		imageSrc.value = null;
	}
});

onUnmounted(() => {
	if (props.previewObjectUrl) {
		imageSrc.value = null;
		return;
	}
	revokePreview();
});

async function download() {
	busy.value = true;
	try {
		await AIAPI.downloadAgentSourceOriginal(props.sourceSessionId, props.fileName);
	} catch (e) {
		console.error('download failed', e);
	} finally {
		busy.value = false;
	}
}
</script>

<style scoped>
.agent-chat-attachment-tile {
	display: flex;
	align-items: stretch;
	gap: 10px;
	min-width: 0;
	max-width: 240px;
	padding: 6px 8px;
	border-radius: var(--chat-radius-sm, 10px);
	border: 1px solid color-mix(in srgb, var(--chat-border) 45%, transparent);
	background: color-mix(in srgb, var(--chat-foreground) 3.5%, transparent);
}

.agent-chat-attachment-tile__thumb {
	flex-shrink: 0;
	width: 52px;
	height: 52px;
	border-radius: 8px;
	overflow: hidden;
	background: color-mix(in srgb, var(--chat-foreground) 5%, transparent);
}

.agent-chat-attachment-tile__img {
	width: 100%;
	height: 100%;
	object-fit: cover;
	display: block;
}

.agent-chat-attachment-tile__placeholder {
	width: 100%;
	height: 100%;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 2px;
	color: var(--chat-muted, var(--chat-foreground));
	opacity: 0.72;
}

.agent-chat-attachment-tile__kind {
	font-size: 9px;
	font-weight: 600;
	letter-spacing: 0.02em;
	line-height: 1;
	opacity: 0.9;
}

.agent-chat-attachment-tile__body {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	justify-content: center;
	gap: 6px;
}

.agent-chat-attachment-tile__name {
	font-size: 12px;
	line-height: 1.3;
	color: color-mix(in srgb, var(--chat-foreground) 88%, transparent);
	overflow: hidden;
	text-overflow: ellipsis;
	display: -webkit-box;
	-webkit-line-clamp: 2;
	-webkit-box-orient: vertical;
}

.agent-chat-attachment-tile__download {
	align-self: flex-start;
	display: inline-flex;
	align-items: center;
	padding: 3px 6px;
	font-size: 11px;
	font-weight: 500;
	border: none;
	border-radius: 6px;
	background: transparent;
	color: var(--chat-muted, var(--chat-foreground));
	opacity: 0.9;
	cursor: pointer;
	text-decoration: underline;
	text-underline-offset: 2px;
	text-decoration-color: color-mix(in srgb, currentColor 35%, transparent);
}

.agent-chat-attachment-tile__download:hover:not(:disabled) {
	opacity: 1;
	background: color-mix(in srgb, var(--chat-foreground) 6%, transparent);
	text-decoration-color: color-mix(in srgb, currentColor 55%, transparent);
}

.agent-chat-attachment-tile__download:disabled {
	opacity: 0.5;
	cursor: default;
}
</style>
