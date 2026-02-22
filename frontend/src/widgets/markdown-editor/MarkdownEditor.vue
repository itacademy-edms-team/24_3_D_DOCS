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
import HighlightToolbarButton from './components/HighlightToolbarButton.vue';
import CaptionToolbarButton from './components/CaptionToolbarButton.vue';
import DownloadPdfToolbarButton from './components/DownloadPdfToolbarButton.vue';
import { createAiChangeExtensions, aiChangeStyles } from './aiChangeDecorations';

interface Props {
	modelValue: string;
	documentId?: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: string];
	acceptAiChange: [changeId: string, changeType: 'insert' | 'delete'];
	undoAiChange: [changeId: string, changeType: 'insert' | 'delete'];
}>();

const { theme } = useTheme();
const editorId = 'markdown-editor';
const editorContainerRef = ref<HTMLElement>();
const mdEditorRef = ref<{ getEditorView?: () => EditorView | undefined } | null>(null);
const localContent = ref(props.modelValue);
const aiExtensionInstalled = ref(false);

const aiChangeExtensions = [
	createAiChangeExtensions(
		(changeId, changeType) => emit('acceptAiChange', changeId, changeType),
		(changeId, changeType) => emit('undoAiChange', changeId, changeType),
	),
	aiChangeStyles,
];

const editorTheme = computed(() => {
	return theme.value === 'dark' ? 'dark' : 'light';
});

const excludedToolbars = ['github', 'preview', 'previewOnly', 'htmlPreview'];

const toolbars = computed(() => [
	0,
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
	1,
	'table',
	'-',
	2,
	'revoke',
	'next',
]);

const defToolbars = computed(() => [
	h(HighlightToolbarButton),
	h(CaptionToolbarButton),
	h(DownloadPdfToolbarButton, { documentId: props.documentId }),
]);

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

const getEditorViewFromDOM = (): EditorView | null => {
	const container = editorContainerRef.value;
	if (!container) return null;
	
	const cmElement = container.querySelector('.cm-editor');
	if (!cmElement) return null;
	
	// CodeMirror 6 stores the view on the DOM element
	const view = (cmElement as any).cmView?.view as EditorView | undefined;
	return view ?? null;
};

const ensureAiExtensionInstalled = () => {
	if (aiExtensionInstalled.value) {
		return;
	}
	
	// Try both methods to get EditorView
	let editorView = mdEditorRef.value?.getEditorView?.();
	if (!editorView) {
		editorView = getEditorViewFromDOM() ?? undefined;
	}
	
	if (!editorView) {
		console.log('[AI Extension] EditorView not available yet, retrying...');
		setTimeout(ensureAiExtensionInstalled, 200);
		return;
	}

	console.log('[AI Extension] Installing AI change decorations extension');
	editorView.dispatch({
		effects: StateEffect.appendConfig.of(aiChangeExtensions),
	});
	aiExtensionInstalled.value = true;
	console.log('[AI Extension] Extension installed successfully');
};

onMounted(async () => {
	await nextTick();
	ensureAiExtensionInstalled();
});

watch(
	() => props.modelValue,
	async () => {
		await nextTick();
		ensureAiExtensionInstalled();
	},
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

/* AI Change Marker Styles */
:deep(.cm-ai-marker) {
	font-size: 0.7em;
	color: #9ca3af;
	font-family: monospace;
	opacity: 0.6;
	background: rgba(156, 163, 175, 0.1);
	padding: 1px 3px;
	border-radius: 2px;
}

:deep(.cm-ai-marker--insert) {
	color: #16a34a;
	background: rgba(34, 197, 94, 0.1);
}

:deep(.cm-ai-marker--delete) {
	color: #dc2626;
	background: rgba(239, 68, 68, 0.1);
}

:deep(.cm-ai-content--insert) {
	background-color: rgba(34, 197, 94, 0.15);
}

:deep(.cm-ai-content--delete) {
	background-color: rgba(239, 68, 68, 0.15);
	text-decoration: line-through;
	text-decoration-color: rgba(239, 68, 68, 0.5);
}

:deep(.cm-ai-actions) {
	display: inline-flex;
	gap: 4px;
	margin-left: 8px;
	vertical-align: middle;
}

:deep(.cm-ai-btn) {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	border: 1px solid;
	border-radius: 4px;
	cursor: pointer;
	font-size: 12px;
	font-weight: bold;
	line-height: 1;
	padding: 0;
}

:deep(.cm-ai-btn--accept) {
	background-color: rgba(34, 197, 94, 0.2);
	border-color: rgba(34, 197, 94, 0.4);
	color: #16a34a;
}

:deep(.cm-ai-btn--accept:hover) {
	background-color: rgba(34, 197, 94, 0.3);
}

:deep(.cm-ai-btn--undo) {
	background-color: rgba(239, 68, 68, 0.2);
	border-color: rgba(239, 68, 68, 0.4);
	color: #dc2626;
}

:deep(.cm-ai-btn--undo:hover) {
	background-color: rgba(239, 68, 68, 0.3);
}
</style>
