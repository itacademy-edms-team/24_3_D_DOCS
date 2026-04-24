<template>
	<div class="tiptap-document-editor" :class="{ 'tiptap-document-editor--dark': isDark }">
		<input
			ref="imageFileInputRef"
			type="file"
			accept="image/*"
			multiple
			class="tiptap-document-editor__hidden-file-input"
			@change="onImageFileInputChange"
		/>
		<EditorContent v-if="editor" :editor="editor" class="tiptap-document-editor__surface" />
		<div v-if="editor" class="tiptap-document-editor__hotkeys-anchor">
			<button
				ref="hotkeysFabRef"
				type="button"
				class="tiptap-document-editor__hotkeys-fab"
				:class="{ 'tiptap-document-editor__hotkeys-fab--open': hotkeysPopoverOpen }"
				title="Горячие клавиши"
				aria-haspopup="dialog"
				:aria-expanded="hotkeysPopoverOpen"
				@click="hotkeysPopoverOpen = !hotkeysPopoverOpen"
			>
				<svg class="tiptap-document-editor__hotkeys-fab-icon" viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
					<path
						fill="currentColor"
						d="M4 5a2 2 0 012-2h12a2 2 0 012 2v4H4V5zm0 6h16v8a2 2 0 01-2 2H6a2 2 0 01-2-2v-8zm3 2v2h2v-2H7zm4 0v2h2v-2h-2zm4 0v2h2v-2h-2zM7 15v2h10v-2H7z"
					/>
				</svg>
			</button>
			<Transition name="tiptap-hotkeys-pop">
				<div
					v-show="hotkeysPopoverOpen"
					ref="hotkeysPopoverRef"
					class="tiptap-document-editor__hotkeys-popover"
					role="dialog"
					aria-label="Сочетания клавиш"
				>
					<div class="tiptap-document-editor__hotkeys-popover-inner">
						<div class="tiptap-document-editor__hotkeys-popover-head">
							<span class="tiptap-document-editor__hotkeys-popover-title">Сочетания клавиш</span>
							<button
								type="button"
								class="tiptap-document-editor__hotkeys-popover-close"
								aria-label="Закрыть"
								@click="hotkeysPopoverOpen = false"
							>
								×
							</button>
						</div>
						<div class="tiptap-document-editor__hotkeys-popover-scroll">
							<template v-for="section in hotkeyHintSections" :key="section.key">
								<p v-if="section.title" class="tiptap-document-editor__hotkeys-popover-section">
									{{ section.title }}
								</p>
								<ul class="tiptap-document-editor__hotkeys-popover-list">
									<li v-for="row in section.rows" :key="row.id" class="tiptap-document-editor__hotkeys-popover-row">
										<span class="tiptap-document-editor__hotkeys-popover-label">{{ row.label }}</span>
										<kbd class="tiptap-document-editor__hotkeys-popover-kbd">{{ row.combo }}</kbd>
									</li>
								</ul>
							</template>
						</div>
						<div class="tiptap-document-editor__hotkeys-popover-foot">
							<RouterLink
								class="tiptap-document-editor__hotkeys-popover-link"
								:to="{ name: 'settings-keyboard' }"
								@click="hotkeysPopoverOpen = false"
							>
								Изменить в настройках
								<span class="tiptap-document-editor__hotkeys-popover-link-arrow" aria-hidden="true">→</span>
							</RouterLink>
						</div>
					</div>
				</div>
			</Transition>
		</div>
		<FormulaBuilderModal
			v-model="formulaModalOpen"
			:initial-latex="formulaModalInitial"
			:is-block="formulaModalIsBlock"
			@apply="onFormulaModalApply"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, provide, shallowRef, onMounted, onUnmounted, computed } from 'vue';
import { RouterLink } from 'vue-router';
import { onClickOutside } from '@vueuse/core';
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
import { DocumentImageTipTap } from './documentImageTipTap';
import 'katex/dist/katex.min.css';
import { useTheme } from '@/app/composables/useTheme';
import { InlineMathTipTap, BlockMathTipTap } from './mathTipTap';
import { DocumentCaptionTipTap } from './documentCaptionTipTap';
import FormulaBuilderModal from './FormulaBuilderModal.vue';
import { formulaBuilderKey, type OpenFormulaBuilderFn } from './formulaBuilderContext';
import { EditorHotkeysTipTap } from './editorHotkeysTipTap';
import EditorHotkeysAPI from '@/shared/api/EditorHotkeysAPI';
import {
	catalogIdsForPayload,
	EDITOR_HOTKEY_SECTIONS,
	emptyBindingsPayload,
	formatHotkeyChord,
	type EditorHotkeyActionId,
	type EditorHotkeyChord,
} from '@/shared/constants/editorHotkeyCatalog';
import { normalizeEditorHotkeyApiBindings } from '@/shared/constants/effectiveEditorHotkeyBindings';
import UploadAPI from '@/shared/api/UploadAPI';

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

const imageFileInputRef = ref<HTMLInputElement | null>(null);
/** Режим выбран хоткеем; crop — отдельный UI позже, пока та же вставка. */
const pendingImageUploadMode = ref<'insert' | 'crop'>('insert');

/** Как в API: `null` = сброшено в настройках (подсказка показывает «—», не дефолт). */
const hotkeyBindingsRaw = shallowRef<Record<EditorHotkeyActionId, EditorHotkeyChord | null>>(
	emptyBindingsPayload() as Record<EditorHotkeyActionId, EditorHotkeyChord | null>,
);

const hotkeysPopoverOpen = ref(false);
const hotkeysPopoverRef = ref<HTMLElement | null>(null);
const hotkeysFabRef = ref<HTMLButtonElement | null>(null);

onClickOutside(
	hotkeysPopoverRef,
	() => {
		hotkeysPopoverOpen.value = false;
	},
	{ ignore: [hotkeysFabRef] },
);

function formatHotkeyForHint(id: EditorHotkeyActionId): string {
	const c = hotkeyBindingsRaw.value[id];
	return c ? formatHotkeyChord(c) : '—';
}

const hotkeyHintSections = computed(() =>
	EDITOR_HOTKEY_SECTIONS.map((section, si) => ({
		key: section.title ? `s-${si}-${section.title}` : `s-${si}`,
		title: section.title,
		rows: section.rows.map((row) => ({
			id: row.id,
			label: row.label,
			combo: formatHotkeyForHint(row.id),
		})),
	})),
);

function requestTipTapImageUpload(mode: 'insert' | 'crop') {
	pendingImageUploadMode.value = mode;
	imageFileInputRef.value?.click();
}

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
		DocumentImageTipTap.configure({
			inline: false,
			allowBase64: true,
			HTMLAttributes: { class: 'tiptap-document-editor__img' },
		}),
		InlineMathTipTap,
		BlockMathTipTap,
		Markdown.configure({
			markedOptions: { gfm: true },
		}),
		EditorHotkeysTipTap.configure({
			getBindings: () => hotkeyBindingsRaw.value,
			openFormulaBuilder,
			requestImageUpload: requestTipTapImageUpload,
			getDocumentId: () => props.documentId,
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

async function onImageFileInputChange(ev: Event) {
	const input = ev.target as HTMLInputElement;
	const files = input.files;
	input.value = '';
	const docId = props.documentId;
	const ed = editor.value;
	if (!files?.length || !docId || !ed) {
		return;
	}

	// «crop» — зарезервировано под отдельный UI; пока вставка как insert.
	void pendingImageUploadMode.value;

	try {
		const uploads = await Promise.all(
			Array.from(files).map((file) => UploadAPI.uploadAsset(docId, file)),
		);
		for (const result of uploads) {
			ed.chain().focus().setImage({ src: result.url, alt: '' }).run();
		}
	} catch (err) {
		console.error('TipTap image upload failed:', err);
	}
}

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

function onHotkeysEscape(e: KeyboardEvent) {
	if (e.key === 'Escape' && hotkeysPopoverOpen.value) {
		hotkeysPopoverOpen.value = false;
	}
}

async function fetchEditorHotkeysFromApi() {
	try {
		const res = await EditorHotkeysAPI.getEditorHotkeys();
		hotkeyBindingsRaw.value = normalizeEditorHotkeyApiBindings(res.bindings);
	} catch {
		/* не трогаем raw — остаётся последний успешный ответ */
	}
}

function onDocumentVisibilityForHotkeys() {
	if (document.visibilityState === 'visible') {
		void fetchEditorHotkeysFromApi();
	}
}

onMounted(() => {
	window.addEventListener('keydown', onHotkeysEscape);
	document.addEventListener('visibilitychange', onDocumentVisibilityForHotkeys);
	void fetchEditorHotkeysFromApi();
});

onUnmounted(() => {
	window.removeEventListener('keydown', onHotkeysEscape);
	document.removeEventListener('visibilitychange', onDocumentVisibilityForHotkeys);
});
</script>

<style scoped>
.tiptap-document-editor {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-height: 0;
	min-width: 0;
	position: relative;
}

.tiptap-document-editor__hidden-file-input {
	position: absolute;
	width: 0;
	height: 0;
	opacity: 0;
	pointer-events: none;
}

.tiptap-document-editor__surface {
	flex: 1;
	min-height: 0;
	width: 100%;
	overflow: auto;
}

/* Как у кнопок шапки редактора: var(--bg-primary), рамка, --radius-md */
.tiptap-document-editor__hotkeys-anchor {
	position: absolute;
	right: 14px;
	bottom: 14px;
	z-index: 20;
	display: flex;
	flex-direction: column-reverse;
	align-items: flex-end;
	gap: 10px;
	pointer-events: none;
}

.tiptap-document-editor__hotkeys-anchor > * {
	pointer-events: auto;
}

.tiptap-document-editor__hotkeys-fab {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 36px;
	height: 36px;
	padding: 0;
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	color: var(--text-secondary);
	cursor: pointer;
	transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
	box-shadow: var(--shadow-sm);
}

.tiptap-document-editor__hotkeys-fab:hover {
	background: var(--bg-secondary);
	color: var(--text-primary);
	border-color: var(--border-hover);
	box-shadow: var(--shadow-md);
	transform: translateY(-1px);
}

.tiptap-document-editor__hotkeys-fab:active {
	transform: translateY(0);
}

.tiptap-document-editor__hotkeys-fab--open {
	color: var(--accent);
	background: var(--accent-light);
	border-color: var(--accent);
	box-shadow: 0 2px 8px var(--accent-light);
}

.tiptap-document-editor__hotkeys-fab-icon {
	display: block;
}

.tiptap-document-editor__hotkeys-popover {
	position: relative;
	width: min(320px, calc(100vw - 48px));
	max-height: min(70vh, 420px);
	border-radius: var(--radius-lg);
	overflow: hidden;
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	box-shadow: var(--shadow-lg);
}

.tiptap-document-editor__hotkeys-popover-inner {
	display: flex;
	flex-direction: column;
	max-height: inherit;
	min-height: 0;
}

.tiptap-document-editor__hotkeys-popover-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	padding: 12px 14px 10px;
	border-bottom: 1px solid var(--border-color);
	flex-shrink: 0;
}

.tiptap-document-editor__hotkeys-popover-title {
	font-size: 13px;
	font-weight: 600;
	color: var(--text-primary);
}

.tiptap-document-editor__hotkeys-popover-close {
	width: 28px;
	height: 28px;
	display: flex;
	align-items: center;
	justify-content: center;
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	font-size: 18px;
	line-height: 1;
	color: var(--text-secondary);
	background: var(--bg-secondary);
	cursor: pointer;
	transition: all 0.2s ease;
}

.tiptap-document-editor__hotkeys-popover-close:hover {
	background: var(--bg-tertiary);
	border-color: var(--border-hover);
	color: var(--text-primary);
}

.tiptap-document-editor__hotkeys-popover-scroll {
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	padding: 8px 14px 6px;
	font-size: 12px;
	line-height: 1.45;
	color: var(--text-secondary);
}

.tiptap-document-editor__hotkeys-popover-section {
	margin: 10px 0 6px;
	font-size: 10px;
	font-weight: 600;
	text-transform: uppercase;
	letter-spacing: 0.06em;
	color: var(--text-tertiary);
}

.tiptap-document-editor__hotkeys-popover-list {
	list-style: none;
	margin: 0;
	padding: 0;
}

.tiptap-document-editor__hotkeys-popover-row {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	padding: 6px 0;
	border-bottom: 1px solid var(--border-color);
}

.tiptap-document-editor__hotkeys-popover-row:last-child {
	border-bottom: none;
}

.tiptap-document-editor__hotkeys-popover-label {
	flex: 1;
	min-width: 0;
	font-weight: 500;
	color: var(--text-primary);
}

.tiptap-document-editor__hotkeys-popover-kbd {
	flex-shrink: 0;
	font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
	font-size: 10px;
	font-weight: 600;
	padding: 3px 8px;
	border-radius: var(--radius-md);
	color: var(--text-primary);
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
}

.tiptap-document-editor__hotkeys-popover-foot {
	padding: 10px 14px 12px;
	border-top: 1px solid var(--border-color);
	background: var(--bg-secondary);
	flex-shrink: 0;
}

.tiptap-document-editor__hotkeys-popover-link {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 13px;
	font-weight: 500;
	color: var(--accent);
	text-decoration: none;
	transition: color 0.2s ease;
}

.tiptap-document-editor__hotkeys-popover-link:hover {
	color: var(--accent-hover);
	text-decoration: underline;
	text-underline-offset: 3px;
}

.tiptap-document-editor__hotkeys-popover-link-arrow {
	font-size: 14px;
	opacity: 0.85;
}

.tiptap-hotkeys-pop-enter-active,
.tiptap-hotkeys-pop-leave-active {
	transition:
		opacity 0.16s ease,
		transform 0.16s ease;
}

.tiptap-hotkeys-pop-enter-from,
.tiptap-hotkeys-pop-leave-to {
	opacity: 0;
	transform: translateY(8px) scale(0.98);
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
