<template>
	<div class="tiptap-document-editor" :class="{ 'tiptap-document-editor--dark': isDark }">
		<EditorContent v-if="editor" :editor="editor" class="tiptap-document-editor__surface" />
		<FormulaBuilderModal
			v-model="formulaModalOpen"
			:initial-latex="formulaModalInitial"
			:is-block="formulaModalIsBlock"
			@apply="onFormulaModalApply"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, provide } from 'vue';
import { useEditor, EditorContent } from '@tiptap/vue-3';
import StarterKit from '@tiptap/starter-kit';
import Placeholder from '@tiptap/extension-placeholder';
import TaskList from '@tiptap/extension-task-list';
import TaskItem from '@tiptap/extension-task-item';
import Highlight from '@tiptap/extension-highlight';
import { Markdown } from '@tiptap/markdown';
import { Table } from '@tiptap/extension-table';
import { TableRow } from '@tiptap/extension-table-row';
import { TableCell } from '@tiptap/extension-table-cell';
import { TableHeader } from '@tiptap/extension-table-header';
import Image from '@tiptap/extension-image';
import 'katex/dist/katex.min.css';
import { useTheme } from '@/app/composables/useTheme';
import { InlineMathTipTap, BlockMathTipTap } from './mathTipTap';
import { DocumentCaptionTipTap } from './documentCaptionTipTap';
import FormulaBuilderModal from './FormulaBuilderModal.vue';
import { formulaBuilderKey, type OpenFormulaBuilderFn } from './formulaBuilderContext';

function canonicalMarkdown(s: string): string {
	return s
		.replace(/\r\n/g, '\n')
		.replace(/\n{3,}/g, '\n\n')
		.trimEnd();
}

function normalizeMarkdownForCompare(a: string, b: string): boolean {
	return canonicalMarkdown(a) === canonicalMarkdown(b);
}

function protectFencedCodeBlocks(md: string): { text: string; blocks: string[] } {
	const blocks: string[] = [];
	const text = md.replace(/```[\s\S]*?```/g, (block) => {
		const i = blocks.length;
		blocks.push(block);
		return `\uE000CODE${i}\uE001`;
	});
	return { text, blocks };
}

function restoreFencedCodeBlocks(text: string, blocks: string[]): string {
	let out = text;
	blocks.forEach((block, i) => {
		out = out.replace(`\uE000CODE${i}\uE001`, block);
	});
	return out;
}

/** Строка-разделитель колонок GFM (`| --- | :---: |`). */
function isGfmTableDelimiterRow(line: string): boolean {
	const t = line.trim();
	if (!t.includes('|') || !t.includes('-')) {
		return false;
	}
	const cells = t
		.split('|')
		.map((c) => c.trim())
		.filter((c) => c.length > 0);
	if (cells.length < 2) {
		return false;
	}
	return cells.every((c) => /^:?-{2,}:?$/.test(c));
}

/** Строка похожа на строку pipe-таблицы (не пустая, есть `|`). */
function isGfmTableRowLine(line: string): boolean {
	const t = line.trim();
	return t.length > 0 && t.includes('|');
}

/**
 * Временно убирает GFM-таблицы из строки.
 * Иначе подстановки блочного HTML (`$$` → div, подписи) ломают разбор ячеек:
 * @tiptap/markdown кладёт результат parseHTMLToken в inline ячейки, а blockMath/documentCaption — block-узлы → невалидный doc и «contentMatchAt on invalid content».
 */
function protectGfmPipeTables(md: string): { text: string; blocks: string[] } {
	const lines = md.replace(/\r\n/g, '\n').split('\n');
	const blocks: string[] = [];
	const out: string[] = [];
	let i = 0;
	while (i < lines.length) {
		if (i + 1 < lines.length && isGfmTableRowLine(lines[i]) && isGfmTableDelimiterRow(lines[i + 1])) {
			const chunk: string[] = [];
			let j = i;
			while (j < lines.length && lines[j].trim() !== '' && isGfmTableRowLine(lines[j])) {
				chunk.push(lines[j]);
				j += 1;
			}
			const idx = blocks.length;
			blocks.push(chunk.join('\n'));
			out.push(`\uE000TABLE${idx}\uE001`);
			i = j;
			continue;
		}
		out.push(lines[i]);
		i += 1;
	}
	return { text: out.join('\n'), blocks };
}

function restoreGfmPipeTables(text: string, blocks: string[]): string {
	let out = text;
	blocks.forEach((block, i) => {
		out = out.replace(`\uE000TABLE${i}\uE001`, block);
	});
	return out;
}

function markdownToEditorInput(md: string): string {
	const { text: withoutCode, blocks: codeBlocks } = protectFencedCodeBlocks(md);
	const { text: withoutTables, blocks: tableBlocks } = protectGfmPipeTables(withoutCode);
	let t = withoutTables;
	/* Блочная математика только вне таблиц — в ячейках оставляем сырой `$$`, иначе HTML попадает в parseInlineTokens и даёт block-узлы внутри paragraph. */
	t = t.replace(/\$\$([\s\S]+?)\$\$/g, (_, formula: string) => {
		const enc = encodeURIComponent(formula.trim());
		return `<div data-type="block-math" data-formula="${enc}"></div>`;
	});
	t = restoreGfmPipeTables(t, tableBlocks);
	t = t.replace(/\[(IMAGE|TABLE|FORMULA)-CAPTION:\s*([^\]]+)\]/g, (_, type: string, cap: string) => {
		const enc = encodeURIComponent(String(cap).trim());
		return `<div data-ddoc-caption="${type}" data-ddoc-text="${enc}"></div>`;
	});
	t = t.replace(/\$([^$\n]+)\$/g, (_, formula: string) => {
		const enc = encodeURIComponent(formula.trim());
		return `<span data-type="inline-math" data-formula="${enc}"></span>`;
	});
	return restoreFencedCodeBlocks(t, codeBlocks);
}

function htmlContainsTable(html: string): boolean {
	if (typeof DOMParser === 'undefined') {
		return /<table\b/i.test(html);
	}
	try {
		const doc = new DOMParser().parseFromString(html, 'text/html');
		return Boolean(doc.body.querySelector('table'));
	} catch {
		return /<table\b/i.test(html);
	}
}

/** В буфере обычно только текст — без HTML-разметки, чтобы в content не попадали «чужие» теги. */
function clipboardHtmlToEditorHtml(html: string): string {
	if (typeof document === 'undefined') {
		return '<p></p>';
	}
	if (htmlContainsTable(html)) {
		return html;
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

const formulaModalOpen = ref(false);
const formulaModalInitial = ref('');
const formulaModalIsBlock = ref(false);
let formulaModalOnApply: ((latex: string) => void) | null = null;

const openFormulaBuilder: OpenFormulaBuilderFn = (opts) => {
	formulaModalInitial.value = opts.initialLatex;
	formulaModalIsBlock.value = opts.isBlock;
	formulaModalOnApply = opts.onApply;
	formulaModalOpen.value = true;
};

provide(formulaBuilderKey, openFormulaBuilder);

function onFormulaModalApply(latex: string) {
	formulaModalOnApply?.(latex);
	formulaModalOnApply = null;
}

watch(formulaModalOpen, (open) => {
	if (!open) {
		formulaModalOnApply = null;
	}
});

let isApplyingExternalUpdate = false;
/** Пока false — игнорируем onUpdate (исключаем ложный emit после setContent). */
const editorReady = ref(false);

function getEditorMarkdown(ed: NonNullable<typeof editor.value>): string {
	return canonicalMarkdown(ed.getMarkdown());
}

function emitIfChanged(ed: NonNullable<typeof editor.value>) {
	const md = getEditorMarkdown(ed);
	if (normalizeMarkdownForCompare(md, props.modelValue)) {
		return;
	}
	/* Синхронно с редактором; API save debounced в ContentEditor. */
	emit('update:modelValue', md);
}

const editor = useEditor({
	extensions: [
		StarterKit.configure({
			heading: { levels: [1, 2, 3, 4, 5, 6] },
			link: {
				openOnClick: false,
				autolink: true,
				HTMLAttributes: {
					class: 'tiptap-document-editor__link',
				},
			},
		}),
		DocumentCaptionTipTap,
		Placeholder.configure({
			placeholder: 'Начните ввод…',
		}),
		Highlight.configure({ multicolor: true }),
		TaskList,
		TaskItem.configure({ nested: true }),
		Table.configure({
			resizable: true,
			HTMLAttributes: { class: 'tiptap-editor-table' },
		}),
		TableRow,
		TableHeader,
		TableCell,
		/* Top-level `![](url)` from markdown must be a block; inline image is not valid under doc block+. */
		Image.configure({
			inline: false,
			allowBase64: true,
			HTMLAttributes: { class: 'tiptap-document-editor__img' },
		}),
		InlineMathTipTap,
		BlockMathTipTap,
		Markdown.configure({
			markedOptions: { gfm: true },
		}),
	],
	content: markdownToEditorInput(props.modelValue),
	contentType: 'markdown',
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
		emitIfChanged(ed);
	},
});

watch(
	() => props.modelValue,
	(md) => {
		const ed = editor.value;
		if (!ed) {
			return;
		}
		const current = getEditorMarkdown(ed);
		if (normalizeMarkdownForCompare(current, md)) {
			return;
		}
		isApplyingExternalUpdate = true;
		ed.commands.setContent(markdownToEditorInput(md), {
			contentType: 'markdown',
			emitUpdate: false,
		});
		queueMicrotask(() => {
			isApplyingExternalUpdate = false;
		});
	},
);
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

.tiptap-document-editor :deep(img.tiptap-document-editor__img) {
	max-width: 100%;
	height: auto;
	vertical-align: middle;
	object-fit: contain;
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

/* Пустые/узкие ячейки: зона клика для курсора (иначе «не вводится» в колонку вроде «Объём |  | $…$»). */
.tiptap-document-editor :deep(.tiptap-document-editor__prose th > p),
.tiptap-document-editor :deep(.tiptap-document-editor__prose td > p) {
	min-height: 1.35em;
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
