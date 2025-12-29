<template>
	<div class="embedding-gutter" ref="gutterRef">
		<div
			v-for="(status, index) in lineStatuses"
			:key="index"
			class="gutter-line"
			:class="{
				'gutter-line--covered': status.isCovered,
				'gutter-line--empty': status.isEmpty,
			}"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { LineEmbeddingStatus } from '@/entities/document/types';

interface Props {
	lineStatuses: LineEmbeddingStatus[];
	lineHeight?: number; // Высота строки в пикселях
}

const props = withDefaults(defineProps<Props>(), {
	lineHeight: 24, // Примерная высота строки по умолчанию
});

const gutterRef = ref<HTMLElement>();
</script>

<style scoped>
.embedding-gutter {
	position: absolute;
	left: 0;
	top: 0;
	width: 3px;
	pointer-events: none;
	z-index: 1;
}

.gutter-line {
	width: 3px;
	height: var(--line-height, 24px);
	margin-bottom: 0;
	transition: background-color 0.2s ease;
}

.gutter-line--covered {
	background-color: #4ade80; /* green-400 */
}

.gutter-line--empty {
	background-color: transparent;
}

.gutter-line:not(.gutter-line--covered):not(.gutter-line--empty) {
	background-color: #9ca3af; /* gray-400 */
}
</style>
