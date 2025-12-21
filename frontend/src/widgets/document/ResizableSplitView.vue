<template>
	<div ref="containerRef" class="split-view">
		<!-- Left pane -->
		<div
			class="split-pane split-pane-left"
			:style="{ width: `${leftWidth}%` }"
		>
			<slot name="left" />
		</div>

		<!-- Resizer -->
		<div
			ref="resizerRef"
			class="split-resizer"
			:class="{ dragging: isDragging }"
			@mousedown="handleMouseDown"
		/>

		<!-- Right pane -->
		<div
			class="split-pane split-pane-right"
			:style="{ width: `${100 - leftWidth}%` }"
		>
			<slot name="right" />
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';

interface Props {
	initialLeftWidth?: number; // Percentage (0-100)
}

const props = withDefaults(defineProps<Props>(), {
	initialLeftWidth: 50,
});

const containerRef = ref<HTMLDivElement | null>(null);
const resizerRef = ref<HTMLDivElement | null>(null);
const leftWidth = ref(props.initialLeftWidth);
const isDragging = ref(false);

function handleMouseDown(e: MouseEvent) {
	e.preventDefault();
	isDragging.value = true;
	document.body.style.cursor = 'col-resize';
	document.body.style.userSelect = 'none';
}

function handleMouseMove(e: MouseEvent) {
	if (!isDragging.value || !containerRef.value) return;

	const containerRect = containerRef.value.getBoundingClientRect();
	const newLeftWidth = ((e.clientX - containerRect.left) / containerRect.width) * 100;

	// Clamp between 20% and 80%
	const clampedWidth = Math.max(20, Math.min(80, newLeftWidth));
	leftWidth.value = clampedWidth;
}

function handleMouseUp() {
	isDragging.value = false;
	document.body.style.cursor = '';
	document.body.style.userSelect = '';
}

onMounted(() => {
	document.addEventListener('mousemove', handleMouseMove);
	document.addEventListener('mouseup', handleMouseUp);
});

onUnmounted(() => {
	document.removeEventListener('mousemove', handleMouseMove);
	document.removeEventListener('mouseup', handleMouseUp);
	// Cleanup in case component unmounts while dragging
	if (isDragging.value) {
		document.body.style.cursor = '';
		document.body.style.userSelect = '';
	}
});
</script>

<style scoped>
.split-view {
	display: flex;
	flex: 1;
	overflow: hidden;
	position: relative;
	height: 100%;
	min-height: 0;
}

.split-pane {
	overflow: hidden;
	min-width: 0;
	height: 100%;
	display: flex;
	flex-direction: column;
}

.split-pane-left {
	flex-shrink: 0;
}

.split-pane-right {
	flex-shrink: 0;
}

.split-resizer {
	width: 4px;
	background: #27272a;
	cursor: col-resize;
	flex-shrink: 0;
	position: relative;
	z-index: 10;
	transition: background 0.2s;
}

.split-resizer:hover,
.split-resizer.dragging {
	background: #6366f1;
}
</style>
