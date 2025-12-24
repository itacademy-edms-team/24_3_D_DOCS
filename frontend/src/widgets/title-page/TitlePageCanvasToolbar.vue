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
	zoom: number;
}

interface Emits {
	(e: 'tool-change', tool: TitlePageElementType | null): void;
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
	border: 1px solid #27272a;
	border-radius: 6px;
	align-items: center;
	flex-wrap: wrap;
	background: #18181b;
}

.toolbar button {
	padding: 0.5rem 1rem;
	background: #27272a;
	color: #e4e4e7;
	border: 1px solid #3f3f46;
	border-radius: 6px;
	cursor: pointer;
	font-size: 14px;
	font-weight: 500;
	transition: all 0.2s;
}

.toolbar button:hover {
	background: #3f3f46;
}

.toolbar button.active {
	background: #6366f1;
	color: #fff;
	border-color: #6366f1;
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
	color: #e4e4e7;
}

.reset-btn {
	font-size: 0.75rem;
}
</style>
