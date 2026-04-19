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
import { ref, computed, watch, h, nextTick, onMounted, onUnmounted } from 'vue';
import { MdEditor } from 'md-editor-v3';
import type { UploadImgEvent } from 'md-editor-v3';
import 'md-editor-v3/lib/style.css';
import { StateEffect } from '@codemirror/state';
import type { EditorView } from '@codemirror/view';
import { useTheme } from '@/app/composables/useTheme';
import { useMarkdownEditorShortcuts } from '@/app/composables/useMarkdownEditorShortcuts';
import UploadAPI from '@/shared/api/UploadAPI';
import type { DocumentAiChange } from '@/entities/document/types';
import HighlightToolbarButton from './components/HighlightToolbarButton.vue';
import CaptionToolbarButton from './components/CaptionToolbarButton.vue';
import DownloadPdfToolbarButton from './components/DownloadPdfToolbarButton.vue';
import {
	createAiChangeExtensions,
	aiChangeStyles,
	refreshAiChangeDecorationsEffect,
} from './aiChangeDecorations';
import { imagePreviewExtension } from './imagePreviewDecorations';

interface Props {
	modelValue: string;
	documentId?: string;
	aiChanges?: DocumentAiChange[];
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
const currentAiChanges = ref<DocumentAiChange[]>(props.aiChanges ?? []);

const aiChangeExtensions = [
	createAiChangeExtensions(
		() => currentAiChanges.value,
		(changeId) => emit('acceptAiChange', changeId),
		(changeId) => emit('undoAiChange', changeId),
	),
	aiChangeStyles,
];

const codeMirrorExtraExtensions = [...aiChangeExtensions, ...imagePreviewExtension];

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
	async (newValue) => {
		if (localContent.value !== newValue) {
			localContent.value = newValue;
		}
		await nextTick();
		ensureAiExtensionInstalled();
	},
);

watch(
	() => props.aiChanges,
	async (newValue) => {
		currentAiChanges.value = newValue ?? [];
		await nextTick();
		ensureAiExtensionInstalled();
		const editorView = mdEditorRef.value?.getEditorView?.() ?? getEditorViewFromDOM() ?? undefined;
		editorView?.dispatch({
			effects: refreshAiChangeDecorationsEffect.of(null),
		});
	},
	{ deep: true }
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
	
	const view = (cmElement as any).cmView?.view as EditorView | undefined;
	return view ?? null;
};

const MAX_INSTALL_RETRIES = 15;
let installRetryCount = 0;
let installRetryTimer: ReturnType<typeof setTimeout> | null = null;

const ensureAiExtensionInstalled = () => {
	if (aiExtensionInstalled.value) return;
	
	let editorView = mdEditorRef.value?.getEditorView?.();
	if (!editorView) {
		editorView = getEditorViewFromDOM() ?? undefined;
	}
	
	if (!editorView) {
		if (installRetryCount < MAX_INSTALL_RETRIES) {
			installRetryCount++;
			installRetryTimer = setTimeout(ensureAiExtensionInstalled, 200);
		}
		return;
	}

	editorView.dispatch({
		effects: StateEffect.appendConfig.of(codeMirrorExtraExtensions),
	});
	aiExtensionInstalled.value = true;
	installRetryCount = 0;
};

onMounted(async () => {
	await nextTick();
	ensureAiExtensionInstalled();
});

onUnmounted(() => {
	if (installRetryTimer !== null) {
		clearTimeout(installRetryTimer);
		installRetryTimer = null;
	}
});

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
