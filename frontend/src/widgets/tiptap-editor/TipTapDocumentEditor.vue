<template>
	<div class="tiptap-document-editor" :class="{ 'tiptap-document-editor--dark': isDark }">
		<EditorContent v-if="editor" :editor="editor" class="tiptap-document-editor__surface" />
	</div>
</template>

<script setup lang="ts">
import { ref, watch, onBeforeUnmount } from 'vue';
import { useEditor, EditorContent } from '@tiptap/vue-3';
import { useDebounceFn } from '@vueuse/core';
import StarterKit from '@tiptap/starter-kit';
import Placeholder from '@tiptap/extension-placeholder';
import Link from '@tiptap/extension-link';
import TaskList from '@tiptap/extension-task-list';
import TaskItem from '@tiptap/extension-task-item';
import Highlight from '@tiptap/extension-highlight';
import Underline from '@tiptap/extension-underline';
import { Table } from '@tiptap/extension-table';
import { TableRow } from '@tiptap/extension-table-row';
import { TableCell } from '@tiptap/extension-table-cell';
import { TableHeader } from '@tiptap/extension-table-header';
import Gapcursor from '@tiptap/extension-gapcursor';
import Image from '@tiptap/extension-image';
import 'katex/dist/katex.min.css';
import { useTheme } from '@/app/composables/useTheme';
import {
	markdownToTipTapHtml,
	tipTapHtmlToMarkdown,
	normalizeMarkdownForCompare,
} from './markdownBridge';
import { InlineMathTipTap, BlockMathTipTap } from './mathTipTap';
import { DocumentCaptionTipTap } from './documentCaptionTipTap';

/** В буфере только текст — без HTML-разметки, чтобы в content не попадали «чужие» теги. */
function clipboardHtmlToEditorHtml(html: string): string {
	if (typeof document === 'undefined') {
		return '<p></p>';
	}
	const wrap = document.createElement('div');
	wrap.innerHTML = html;
	const plain = (wrap.innerText ?? wrap.textContent ?? '').replace(/\u00a0/g, ' ');
	if (!plain) {
		return '<p></p>';
	}
	const escaped = plain
		.replace(/&/g, '&amp;')
		.replace(/</g, '&lt;')
		.replace(/>/g, '&gt;');
	return escaped
		.split(/\n/)
		.map((line) => `<p>${line}</p>`)
		.join('');
}

const props = defineProps<{
	modelValue: string;
	documentId?: string;
}>();

const emit = defineEmits<{
	'update:modelValue': [string];
}>();

const { isDark } = useTheme();

let isApplyingExternalUpdate = false;
/** Пока false — игнорируем onUpdate (исключаем ложный emit после setContent). */
const editorReady = ref(false);

const emitDebounced = useDebounceFn((md: string) => {
	emit('update:modelValue', md);
}, 400);

function emitImmediate(md: string) {
	emitDebounced.cancel();
	emit('update:modelValue', md);
}

function emitIfChanged(html: string) {
	const md = tipTapHtmlToMarkdown(html);
	if (normalizeMarkdownForCompare(md, props.modelValue)) {
		return;
	}
	emitDebounced(md);
}

const editor = useEditor({
	extensions: [
		StarterKit.configure({
			heading: { levels: [1, 2, 3, 4, 5, 6] },
		}),
		DocumentCaptionTipTap,
		Placeholder.configure({
			placeholder: 'Начните ввод…',
		}),
		Underline,
		Highlight.configure({ multicolor: true }),
		Link.configure({
			openOnClick: false,
			autolink: true,
			HTMLAttributes: {
				class: 'tiptap-document-editor__link',
			},
		}),
		TaskList,
		TaskItem.configure({ nested: true }),
		Table.configure({
			resizable: true,
			HTMLAttributes: { class: 'tiptap-editor-table' },
		}),
		TableRow,
		TableHeader,
		TableCell,
		Gapcursor,
		Image.configure({
			inline: true,
			allowBase64: true,
			HTMLAttributes: { class: 'tiptap-document-editor__img' },
		}),
		InlineMathTipTap,
		BlockMathTipTap,
	],
	content: markdownToTipTapHtml(props.modelValue),
	immediatelyRender: false,
	editorProps: {
		attributes: {
			class: 'tiptap-document-editor__prose',
		},
		transformPastedHTML(html) {
			return clipboardHtmlToEditorHtml(html);
		},
	},
	onCreate: () => {
		queueMicrotask(() => {
			editorReady.value = true;
		});
	},
	onUpdate: ({ editor: ed }) => {
		if (isApplyingExternalUpdate || !editorReady.value) {
			return;
		}
		emitIfChanged(ed.getHTML());
	},
});

watch(
	() => props.modelValue,
	(md) => {
		const ed = editor.value;
		if (!ed) {
			return;
		}
		const current = tipTapHtmlToMarkdown(ed.getHTML());
		if (normalizeMarkdownForCompare(current, md)) {
			return;
		}
		isApplyingExternalUpdate = true;
		ed.commands.setContent(markdownToTipTapHtml(md), { emitUpdate: false });
		queueMicrotask(() => {
			isApplyingExternalUpdate = false;
		});
	},
);

onBeforeUnmount(() => {
	const ed = editor.value;
	if (ed) {
		const next = tipTapHtmlToMarkdown(ed.getHTML());
		if (!normalizeMarkdownForCompare(next, props.modelValue)) {
			emitImmediate(next);
		}
	}
});
</script>

<style scoped>
.tiptap-document-editor {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-height: 0;
	min-width: 0;
}

.tiptap-document-editor__surface {
	flex: 1;
	min-height: 0;
	width: 100%;
	overflow: auto;
}

.tiptap-document-editor :deep(.tiptap-document-editor__prose) {
	outline: none;
	min-height: 100%;
	max-width: 100%;
	padding: 12px 16px 24px;
	font-size: 15px;
	line-height: 1.55;
	color: var(--text-primary, #0f172a);
}

.tiptap-document-editor--dark :deep(.tiptap-document-editor__prose) {
	color: var(--text-primary, #f1f5f9);
}

.tiptap-document-editor :deep(.tiptap-document-editor__prose p.is-editor-empty:first-child::before) {
	color: var(--text-tertiary, #94a3b8);
	content: attr(data-placeholder);
	float: left;
	height: 0;
	pointer-events: none;
}

.tiptap-document-editor :deep(.tiptap-document-editor__prose table),
.tiptap-document-editor :deep(table.tiptap-editor-table) {
	width: 100%;
	border-collapse: collapse;
	margin: 0.75rem 0;
}

.tiptap-document-editor :deep(.tiptap-document-editor__prose th),
.tiptap-document-editor :deep(.tiptap-document-editor__prose td) {
	border: 1px solid var(--border-color, #cbd5e1);
	padding: 6px 10px;
	vertical-align: top;
}

.tiptap-document-editor :deep(.tiptap-document-editor__prose th) {
	background: var(--bg-secondary, rgba(0, 0, 0, 0.05));
	font-weight: 600;
}

.tiptap-document-editor :deep(.tiptap-document-editor__prose ul[data-type='taskList']) {
	list-style: none;
	padding-left: 0;
}

.tiptap-document-editor :deep(.tiptap-document-editor__prose ul[data-type='taskList'] li) {
	display: flex;
	gap: 0.4rem;
}

.tiptap-document-editor :deep(.tiptap-document-editor__prose pre) {
	background: var(--bg-secondary, #f1f5f9);
	border: 1px solid var(--border-color, #e2e8f0);
	border-radius: 8px;
	padding: 12px;
	overflow-x: auto;
	font-size: 13px;
}

.tiptap-document-editor--dark :deep(.tiptap-document-editor__prose pre) {
	background: rgba(0, 0, 0, 0.25);
}
</style>
