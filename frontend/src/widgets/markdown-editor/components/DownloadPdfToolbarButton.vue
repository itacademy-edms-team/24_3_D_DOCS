<template>
	<NormalToolbar :title="title" :onClick="handleClick" :disabled="disabled || isDownloading || !documentId">
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
			:class="{ 'download-icon--loading': isDownloading }"
		>
			<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
			<polyline points="7 10 12 15 17 10" />
			<line x1="12" y1="15" x2="12" y2="3" />
		</svg>
		<span v-if="showToolbarName" class="toolbar-item-name">{{ title }}</span>
	</NormalToolbar>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { NormalToolbar } from 'md-editor-v3';
import DocumentAPI from '@/entities/document/api/DocumentAPI';

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

const isDownloading = ref(false);

const downloadFile = (blob: Blob, filename: string) => {
	const url = URL.createObjectURL(blob);
	const link = document.createElement('a');
	link.href = url;
	link.download = filename;
	document.body.appendChild(link);
	link.click();
	document.body.removeChild(link);
	URL.revokeObjectURL(url);
};

const handleClick = async () => {
	if (!props.documentId || props.disabled || isDownloading.value) return;

	isDownloading.value = true;

	try {
		const filename = `document-${props.documentId}.pdf`;
		
		// Always generate a new PDF to ensure it's up-to-date with the latest content
		const blob = await DocumentAPI.generatePdf(props.documentId);
		
		downloadFile(blob, filename);
	} catch (error) {
		console.error('Failed to generate PDF:', error);
		console.error('Error details:', error);
		alert('Не удалось сгенерировать PDF. Попробуйте позже.');
	} finally {
		isDownloading.value = false;
	}
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

.download-icon--loading {
	animation: spin 1s linear infinite;
}

@keyframes spin {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}

.toolbar-item-name {
	font-size: 12px;
	margin-top: 2px;
}
</style>
