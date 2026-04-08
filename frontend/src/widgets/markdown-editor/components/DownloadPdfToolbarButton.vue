<template>
	<NormalToolbar :title="title" :onClick="handleClick" :disabled="disabled || !documentId">
		<svg
			xmlns="http://www.w3.org/2000/svg"
			width="24"
			height="24"
			viewBox="0 0 24 24"
			fill="none"
			stroke="currentColor"
			stroke-width="2"
			stroke-linecap="round"
			stroke-linejoin="round"
			class="download-icon"
		>
			<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
			<polyline points="7 10 12 15 17 10" />
			<line x1="12" y1="15" x2="12" y2="3" />
		</svg>
		<span v-if="showToolbarName" class="toolbar-item-name">{{ title }}</span>
	</NormalToolbar>
</template>

<script setup lang="ts">
import { inject } from 'vue';
import { NormalToolbar } from 'md-editor-v3';

interface Props {
	title?: string;
	disabled?: boolean;
	showToolbarName?: boolean;
	documentId?: string;
}

const props = withDefaults(defineProps<Props>(), {
	title: 'Скачать PDF',
	disabled: false,
	showToolbarName: false,
});

const openExportPdfModal = inject<(() => void) | undefined>('openExportPdfModal', undefined);

const handleClick = () => {
	if (!props.documentId || props.disabled) return;
	openExportPdfModal?.();
};
</script>

<style scoped>
.download-icon {
	width: 24px;
	height: 24px;
	padding: 2px;
	color: currentColor;
	transition: transform 0.2s;
}

.toolbar-item-name {
	font-size: 12px;
	margin-top: 2px;
}
</style>
