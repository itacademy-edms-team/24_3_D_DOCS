<template>
	<div ref="editorContainerRef" class="markdown-editor">
		<MdEditor
			ref="mdEditorRef"
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
import { ref, computed, watch, h, nextTick, onMounted } from 'vue';
import { MdEditor } from 'md-editor-v3';
import type { UploadImgEvent } from 'md-editor-v3';
import 'md-editor-v3/lib/style.css';
import { StateEffect } from '@codemirror/state';
import type { EditorView } from '@codemirror/view';
import { useTheme } from '@/app/composables/useTheme';
import { useMarkdownEditorShortcuts } from '@/app/composables/useMarkdownEditorShortcuts';
import UploadAPI from '@/shared/api/UploadAPI';
import type { DocumentEntityChangeDTO } from '@/shared/api/AIAPI';
import HighlightToolbarButton from './components/HighlightToolbarButton.vue';
import CaptionToolbarButton from './components/CaptionToolbarButton.vue';
import DownloadPdfToolbarButton from './components/DownloadPdfToolbarButton.vue';
import { createAiChangeExtensions, setAiChangesEffect } from './aiChangeDecorations';

interface Props {
	modelValue: string;
	documentId?: string;
	aiPendingChanges?: DocumentEntityChangeDTO[];
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: string];
	acceptAiChange: [changeId: string];
	undoAiChange: [changeId: string];
}>();

const { theme } = useTheme();
const editorId = 'markdown-editor';
const editorContainerRef = ref<HTMLElement>();
const mdEditorRef = ref<{ getEditorView?: () => EditorView | undefined } | null>(null);
const localContent = ref(props.modelValue);
const aiExtensionInstalled = ref(false);

const aiChangeExtensions = createAiChangeExtensions(
	(changeId) => emit('acceptAiChange', changeId),
	(changeId) => emit('undoAiChange', changeId),
);

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

const ensureAiExtensionInstalled = () => {
	const editorView = mdEditorRef.value?.getEditorView?.();
	if (!editorView || aiExtensionInstalled.value) {
		return;
	}

	editorView.dispatch({
		effects: StateEffect.appendConfig.of(aiChangeExtensions),
	});
	aiExtensionInstalled.value = true;
};

const pushPendingAiChangesToEditor = () => {
	const editorView = mdEditorRef.value?.getEditorView?.();
	if (!editorView || !aiExtensionInstalled.value) {
		return;
	}

	editorView.dispatch({
		effects: setAiChangesEffect.of(props.aiPendingChanges ?? []),
	});
};

onMounted(async () => {
	await nextTick();
	ensureAiExtensionInstalled();
	pushPendingAiChangesToEditor();
});

watch(
	() => props.aiPendingChanges,
	async () => {
		await nextTick();
		ensureAiExtensionInstalled();
		pushPendingAiChangesToEditor();
	},
	{ deep: true, immediate: true }
);

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

:deep(.cm-ai-change-delete) {
	background: rgba(239, 68, 68, 0.16);
	border-radius: 4px;
}

:deep(.cm-ai-change-widget) {
	border: 1px solid transparent;
	border-radius: 8px;
	margin: 6px 0;
	padding: 8px 10px;
	font-size: 12px;
	line-height: 1.4;
}

:deep(.cm-ai-change-widget--insert) {
	background: rgba(34, 197, 94, 0.16);
	border-color: rgba(34, 197, 94, 0.38);
}

:deep(.cm-ai-change-widget--delete) {
	background: rgba(239, 68, 68, 0.16);
	border-color: rgba(239, 68, 68, 0.38);
}

:deep(.cm-ai-change-widget__header) {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	font-weight: 600;
}

:deep(.cm-ai-change-widget__actions) {
	display: inline-flex;
	gap: 6px;
}

:deep(.cm-ai-change-widget__btn) {
	border: 1px solid transparent;
	border-radius: 6px;
	padding: 3px 8px;
	font-size: 12px;
	font-weight: 600;
	cursor: pointer;
}

:deep(.cm-ai-change-widget__btn--accept) {
	background: rgba(34, 197, 94, 0.22);
	border-color: rgba(34, 197, 94, 0.45);
	color: #166534;
}

:deep(.cm-ai-change-widget__btn--undo) {
	background: rgba(239, 68, 68, 0.2);
	border-color: rgba(239, 68, 68, 0.4);
	color: #991b1b;
}

:deep(.cm-ai-change-widget__content) {
	margin: 8px 0 0 0;
	white-space: pre-wrap;
	font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
	font-size: 12px;
}
</style>
