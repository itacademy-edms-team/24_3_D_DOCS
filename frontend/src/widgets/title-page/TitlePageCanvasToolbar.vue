<template>
	<div class="toolbar">
		<button
			:class="{ active: tool === 'text' }"
			@click="handleToolClick('text')"
		>
			Текст
		</button>
		<button
			:class="{ active: tool === 'variable' }"
			@click="handleToolClick('variable')"
		>
			Переменная
		</button>
		<button
			:class="{ active: tool === 'line' }"
			@click="handleToolClick('line')"
		>
			Линия
		</button>
		<button
			:class="{ active: showGrid }"
			@click="onGridToggle"
			title="Toggle grid (G)"
		>
			Сетка
		</button>

		<!-- Zoom controls -->
		<div class="zoom-controls">
			<button @click="handleZoomOut">−</button>
			<span class="zoom-value">{{ Math.round(zoom * 100) }}%</span>
			<button @click="handleZoomIn">+</button>
			<button @click="handleZoomReset" class="reset-btn">Сброс</button>
		</div>
	</div>
</template>

<script setup lang="ts">
import type { TitlePageElementType } from '@/shared/types/titlePage';

interface Props {
	tool: TitlePageElementType | null;
	showGrid: boolean;
	zoom: number;
}

interface Emits {
	(e: 'tool-change', tool: TitlePageElementType | null): void;
	(e: 'grid-toggle'): void;
	(e: 'zoom-change', zoom: number): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

function handleToolClick(toolType: TitlePageElementType) {
	const newTool = props.tool === toolType ? null : toolType;
	emit('tool-change', newTool);
}

function handleZoomOut() {
	emit('zoom-change', Math.max(0.25, props.zoom - 0.1));
}

function handleZoomIn() {
	emit('zoom-change', Math.min(2, props.zoom + 0.1));
}

function handleZoomReset() {
	emit('zoom-change', 1);
}
</script>

<style scoped>
.toolbar {
	display: flex;
	gap: 0.5rem;
	padding: 0.5rem;
	border: 1px solid #ddd;
	border-radius: 4px;
	align-items: center;
	flex-wrap: wrap;
}

.toolbar button {
	padding: 0.5rem 1rem;
	background: #f0f0f0;
	color: #000;
	border: 1px solid #ddd;
	border-radius: 4px;
	cursor: pointer;
}

.toolbar button.active {
	background: #0066ff;
	color: #fff;
}

.zoom-controls {
	margin-left: auto;
	display: flex;
	gap: 0.5rem;
	align-items: center;
}

.zoom-controls button {
	padding: 0.25rem 0.5rem;
}

.zoom-value {
	min-width: 60px;
	text-align: center;
	font-size: 0.875rem;
}

.reset-btn {
	font-size: 0.75rem;
}
</style>
