<template>
	<NormalToolbar :title="title" :onClick="handleClick" :disabled="disabled">
		<svg
			xmlns="http://www.w3.org/2000/svg"
			width="24"
			height="24"
			viewBox="0 0 24 24"
			fill="none"
			stroke="currentColor"
			stroke-width="2"
			stroke-linecap="round"
			stroke-linejoin="round"
			class="highlight-icon"
		>
			<path d="m9 11-6 6v3h3l6-6m-3-3 7-7a2 2 0 0 1 2 2l-7 7-2-2z" />
		</svg>
		<span v-if="showToolbarName" class="toolbar-item-name">{{ title }}</span>
	</NormalToolbar>
</template>

<script setup lang="ts">
import { NormalToolbar } from 'md-editor-v3';
import type { InsertContentGenerator } from 'md-editor-v3/es/types';

interface Props {
	title?: string;
	disabled?: boolean;
	showToolbarName?: boolean;
	insert?: (generate: InsertContentGenerator) => void;
}

const props = withDefaults(defineProps<Props>(), {
	title: 'Выделить текст',
	disabled: false,
	showToolbarName: false,
});

const handleClick = () => {
	if (!props.insert || props.disabled) return;

	// Use insert function to wrap selected text in ==text==
	props.insert((selectedText: string) => {
		const text = selectedText ? `==${selectedText}==` : '==выделенный текст==';
		return {
			targetValue: text,
			select: !selectedText, // Select placeholder if no text selected
			deviationStart: selectedText ? 2 : 2,
			deviationEnd: selectedText ? -2 : -17, // -17 to select "выделенный текст"
		};
	});
};
</script>

<style scoped>
.highlight-icon {
	width: 24px;
	height: 24px;
	padding: 2px;
	color: currentColor;
}

.toolbar-item-name {
	font-size: 12px;
	margin-top: 2px;
}
</style>
