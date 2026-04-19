<template>
	<NodeViewWrapper as="span" class="tiptap-math-inline">
		<input
			v-if="isEditing"
			ref="inputRef"
			v-model="localFormula"
			type="text"
			class="tiptap-math-inline__input"
			placeholder="LaTeX…"
			@blur="commit"
			@keydown="onKeydown"
		/>
		<span
			v-else
			ref="displayRef"
			class="tiptap-math-inline__display"
			title="Щелчок — конструктор, двойной щелчок — правка вручную"
			@click="onDisplayClick"
		/>
	</NodeViewWrapper>
</template>

<script setup lang="ts">
import { ref, watch, nextTick, onMounted, onBeforeUnmount, inject } from 'vue';
import { NodeViewWrapper, nodeViewProps } from '@tiptap/vue-3';
import katex from 'katex';
import { formulaBuilderKey } from './formulaBuilderContext';

const props = defineProps(nodeViewProps);

const openFormulaBuilder = inject(formulaBuilderKey, null);

const isEditing = ref(false);
const localFormula = ref((props.node.attrs.formula as string) || '');
const displayRef = ref<HTMLSpanElement | null>(null);
const inputRef = ref<HTMLInputElement | null>(null);

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
		katex.render(localFormula.value || '?', el, {
			throwOnError: false,
			displayMode: false,
			strict: 'ignore',
		});
	} catch {
		el.textContent = localFormula.value || '?';
	}
}

watch([localFormula, isEditing], () => {
	void nextTick(() => paint());
});

onMounted(() => {
	paint();
});

let displayClickTimer: ReturnType<typeof setTimeout> | null = null;

function clearDisplayClickTimer() {
	if (displayClickTimer !== null) {
		clearTimeout(displayClickTimer);
		displayClickTimer = null;
	}
}

function onDisplayClick(e: MouseEvent) {
	if (e.detail === 2) {
		clearDisplayClickTimer();
		startEdit();
		return;
	}
	if (e.detail !== 1) return;
	const fn = openFormulaBuilder;
	if (!fn) return;
	clearDisplayClickTimer();
	displayClickTimer = setTimeout(() => {
		displayClickTimer = null;
		fn({
			initialLatex: localFormula.value,
			isBlock: false,
			onApply: (latex) => {
				localFormula.value = latex;
				props.updateAttributes({ formula: latex });
				void nextTick(() => paint());
			},
		});
	}, 280);
}

function startEdit() {
	isEditing.value = true;
	void nextTick(() => {
		inputRef.value?.focus();
		inputRef.value?.select();
	});
}

onBeforeUnmount(() => {
	clearDisplayClickTimer();
});

function commit() {
	isEditing.value = false;
	props.updateAttributes({ formula: localFormula.value });
	void nextTick(() => paint());
}

function onKeydown(e: KeyboardEvent) {
	if (e.key === 'Enter') {
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
.tiptap-math-inline__display {
	cursor: pointer;
	padding: 0 2px;
	border-radius: 4px;
}
.tiptap-math-inline__display:hover {
	background: var(--bg-secondary, rgba(0, 0, 0, 0.06));
}
.tiptap-math-inline__input {
	min-width: 4rem;
	max-width: 100%;
	font: 12px/1.3 ui-monospace, monospace;
	padding: 2px 6px;
	border: 1px solid var(--border-color, #cbd5e1);
	border-radius: 4px;
	background: var(--bg-primary, #fff);
	color: var(--text-primary, #111);
}
</style>
