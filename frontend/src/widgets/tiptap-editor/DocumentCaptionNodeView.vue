<template>
	<NodeViewWrapper as="div" class="ddoc-caption-node" data-ddoc-caption-ui>
		<div class="ddoc-caption-node__row">
			<span class="ddoc-caption-node__label">[{{ kind }}-CAPTION:</span>
			<input
				v-if="editing"
				ref="inputRef"
				v-model="localText"
				type="text"
				class="ddoc-caption-node__input"
				@blur="commit"
				@keydown="onKeydown"
			/>
			<span
				v-else
				class="ddoc-caption-node__text"
				title="Двойной щелчок — правка текста подписи"
				@dblclick="startEdit"
			>{{ displayText }}</span>
			<span class="ddoc-caption-node__close">]</span>
		</div>
	</NodeViewWrapper>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue';
import { NodeViewWrapper, nodeViewProps } from '@tiptap/vue-3';

const props = defineProps(nodeViewProps);

const editing = ref(false);
const localText = ref(String(props.node.attrs.text ?? ''));
const inputRef = ref<HTMLInputElement | null>(null);

const kind = computed(() => String(props.node.attrs.kind ?? 'TABLE'));

const displayText = computed(() => localText.value || '…');

watch(
	() => props.node.attrs.text,
	(t) => {
		if (!editing.value) {
			localText.value = String(t ?? '');
		}
	},
);

function startEdit() {
	editing.value = true;
	void nextTick(() => {
		inputRef.value?.focus();
		inputRef.value?.select();
	});
}

function commit() {
	editing.value = false;
	props.updateAttributes({ text: localText.value });
}

function onKeydown(e: KeyboardEvent) {
	if (e.key === 'Enter') {
		e.preventDefault();
		commit();
	}
	if (e.key === 'Escape') {
		editing.value = false;
		localText.value = String(props.node.attrs.text ?? '');
	}
}
</script>

<style scoped>
.ddoc-caption-node {
	margin: 0.35rem 0;
	padding: 6px 10px;
	border: 1px dashed var(--border-color, #94a3b8);
	border-radius: 6px;
	background: var(--bg-secondary, rgba(148, 163, 184, 0.12));
	font: 13px/1.4 ui-monospace, SFMono-Regular, Menlo, monospace;
	color: var(--text-primary, #0f172a);
}
.ddoc-caption-node__row {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 4px;
}
.ddoc-caption-node__label,
.ddoc-caption-node__close {
	color: var(--text-secondary, #64748b);
	white-space: pre;
}
.ddoc-caption-node__text {
	cursor: pointer;
	min-width: 2ch;
	word-break: break-word;
}
.ddoc-caption-node__input {
	flex: 1;
	min-width: 8rem;
	max-width: 100%;
	font: inherit;
	padding: 2px 6px;
	border: 1px solid var(--border-color, #cbd5e1);
	border-radius: 4px;
	background: var(--bg-primary, #fff);
	color: inherit;
}
</style>
