<template>
	<NodeViewWrapper as="div" class="tiptap-math-block">
		<div v-if="isEditing" class="tiptap-math-block__edit">
			<div class="tiptap-math-block__fence">$$</div>
			<textarea
				ref="textareaRef"
				v-model="localFormula"
				class="tiptap-math-block__textarea"
				placeholder="Формула LaTeX…"
				@blur="commit"
				@keydown="onKeydown"
			/>
			<div class="tiptap-math-block__fence">$$</div>
			<p class="tiptap-math-block__hint">Ctrl+Enter — сохранить, Esc — отмена</p>
		</div>
		<div
			v-else
			ref="displayRef"
			class="tiptap-math-block__display"
			title="Двойной щелчок — правка; Alt+щелчок — конструктор формул"
			@click="onDisplayClick"
		/>
	</NodeViewWrapper>
</template>

<script setup lang="ts">
import { ref, watch, nextTick, onMounted, inject } from 'vue';
import { NodeViewWrapper, nodeViewProps } from '@tiptap/vue-3';
import katex from 'katex';
import { formulaBuilderKey } from './formulaBuilderContext';

const props = defineProps(nodeViewProps);

const openFormulaBuilder = inject(formulaBuilderKey, null);

const isEditing = ref(false);
const localFormula = ref((props.node.attrs.formula as string) || '');
const displayRef = ref<HTMLDivElement | null>(null);
const textareaRef = ref<HTMLTextAreaElement | null>(null);

watch(
	() => props.node.attrs.formula,
	(f) => {
		if (!isEditing.value) {
			localFormula.value = (f as string) || '';
		}
	},
);

function paint() {
	const el = displayRef.value;
	if (!el || isEditing.value) return;
	try {
		katex.render(localFormula.value || '\\text{ }', el, {
			throwOnError: false,
			displayMode: true,
			strict: 'ignore',
		});
	} catch {
		el.textContent = localFormula.value || '';
	}
}

watch([localFormula, isEditing], () => {
	void nextTick(() => paint());
});

onMounted(() => {
	paint();
});

function onDisplayClick(e: MouseEvent) {
	if (e.detail === 2) {
		startEdit();
		return;
	}
	if (e.detail === 1 && e.altKey) {
		const fn = openFormulaBuilder;
		if (!fn) return;
		fn({
			initialLatex: localFormula.value,
			isBlock: true,
			onApply: (latex) => {
				localFormula.value = latex;
				props.updateAttributes({ formula: latex });
				void nextTick(() => paint());
			},
		});
	}
}

function startEdit() {
	isEditing.value = true;
	void nextTick(() => {
		textareaRef.value?.focus();
		textareaRef.value?.select();
	});
}

function commit() {
	isEditing.value = false;
	props.updateAttributes({ formula: localFormula.value });
	void nextTick(() => paint());
}

function onKeydown(e: KeyboardEvent) {
	if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
		e.preventDefault();
		commit();
	}
	if (e.key === 'Escape') {
		isEditing.value = false;
		localFormula.value = (props.node.attrs.formula as string) || '';
		void nextTick(() => paint());
	}
}
</script>

<style scoped>
.tiptap-math-block__display {
	cursor: pointer;
	text-align: center;
	padding: 0.75rem 1rem;
	border-radius: 8px;
}
.tiptap-math-block__display:hover {
	background: var(--bg-secondary, rgba(0, 0, 0, 0.04));
}
.tiptap-math-block__edit {
	padding: 0.75rem;
	border: 1px solid var(--border-color, #cbd5e1);
	border-radius: 8px;
	background: var(--bg-secondary, #f8fafc);
}
.tiptap-math-block__fence {
	font: 11px ui-monospace, monospace;
	color: var(--text-tertiary, #64748b);
}
.tiptap-math-block__textarea {
	width: 100%;
	min-height: 5rem;
	margin: 6px 0;
	padding: 8px;
	font: 13px ui-monospace, monospace;
	border: 1px solid var(--border-color, #cbd5e1);
	border-radius: 6px;
	background: var(--bg-primary, #fff);
	color: var(--text-primary, #111);
	resize: vertical;
}
.tiptap-math-block__hint {
	margin: 6px 0 0;
	font-size: 11px;
	color: var(--text-tertiary, #64748b);
}
</style>
