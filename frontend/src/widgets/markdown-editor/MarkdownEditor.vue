<template>
	<div ref="editorContainerRef" class="markdown-editor">
		<MdEditor
			:id="editorId"
			:modelValue="localContent"
			:theme="editorTheme"
			:preview="false"
			:toolbars="toolbars"
			:toolbarsExclude="excludedToolbars"
			:defToolbars="defToolbars"
			:onUploadImg="handleUploadImg"
			language="en-US"
			@onChange="handleChange"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, h } from 'vue';
import { MdEditor } from 'md-editor-v3';
import type { UploadImgEvent } from 'md-editor-v3';
import 'md-editor-v3/lib/style.css';
import { useTheme } from '@/app/composables/useTheme';
import { useMarkdownEditorShortcuts } from '@/app/composables/useMarkdownEditorShortcuts';
import UploadAPI from '@/shared/api/UploadAPI';
import HighlightToolbarButton from './components/HighlightToolbarButton.vue';
import CaptionToolbarButton from './components/CaptionToolbarButton.vue';
import DownloadPdfToolbarButton from './components/DownloadPdfToolbarButton.vue';

interface Props {
	modelValue: string;
	documentId?: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: string];
}>();

const { theme } = useTheme();
const editorId = 'markdown-editor';
const editorContainerRef = ref<HTMLElement>();
const localContent = ref(props.modelValue);

const editorTheme = computed(() => {
	return theme.value === 'dark' ? 'dark' : 'light';
});

const excludedToolbars = ['github', 'preview', 'previewOnly', 'htmlPreview'];

// Toolbars array with custom buttons
const toolbars = computed(() => [
	0, // Index for highlight button in defToolbars
	'bold',
	'underline',
	'italic',
	'strikeThrough',
	'-',
	'title',
	'sub',
	'sup',
	'quote',
	'unorderedList',
	'orderedList',
	'task',
	'-',
	'codeRow',
	'code',
	'link',
	'image',
	1, // Index for caption button in defToolbars
	'table',
	'-',
	2, // Index for download PDF button in defToolbars
	'revoke',
	'next',
]);

// Custom toolbars - md-editor-v3 expects a VNode or array of VNodes
const defToolbars = computed(() => [
	h(HighlightToolbarButton),
	h(CaptionToolbarButton),
	h(DownloadPdfToolbarButton, { documentId: props.documentId }),
]);

// Handle image upload
const handleUploadImg: UploadImgEvent = async (files, callBack) => {
	if (!props.documentId) {
		console.error('Document ID is required for image upload');
		callBack([]);
		return;
	}

	try {
		const uploadPromises = Array.from(files).map((file) =>
			UploadAPI.uploadAsset(props.documentId!, file)
		);

		const results = await Promise.all(uploadPromises);
		const urls = results.map((result) => result.url);
		callBack(urls);
	} catch (error) {
		console.error('Failed to upload images:', error);
		callBack([]);
	}
};

// Setup keyboard shortcuts with layout-independent key detection
useMarkdownEditorShortcuts({
	editorElementRef: editorContainerRef,
	onContentChange: (newContent: string) => {
		localContent.value = newContent;
		emit('update:modelValue', newContent);
	},
});

watch(
	() => props.modelValue,
	(newValue) => {
		if (localContent.value !== newValue) {
			localContent.value = newValue;
		}
	},
);

const handleChange = (value: string) => {
	localContent.value = value;
	emit('update:modelValue', value);
};

</script>

<style scoped>
.markdown-editor {
	height: 100%;
	width: 100%;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}

:deep(.md-editor) {
	height: 100%;
	width: 100%;
	display: flex;
	flex-direction: column;
}

:deep(.md-editor-content-wrapper) {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}

:deep(.md-editor-content) {
	flex: 1;
	overflow: auto;
}
</style>
