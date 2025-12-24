<template>
	<div class="canvas-container">
		<!-- Toolbar -->
		<TitlePageCanvasToolbar
			:tool="tool"
			:zoom="zoom"
			@tool-change="handleToolChange"
			@zoom-change="handleZoomChange"
		/>

		<!-- Canvas with Rulers -->
		<div class="canvas-wrapper">
			<div
				class="canvas-zoom-container"
				:style="{ transform: `scale(${zoom})`, transformOrigin: 'top left' }"
			>
				<!-- Horizontal Ruler -->
				<TitlePageCanvasRuler
					orientation="horizontal"
					:mouse-pos="mousePos"
				/>

				<!-- Vertical Ruler -->
				<TitlePageCanvasRuler
					orientation="vertical"
					:mouse-pos="mousePos"
				/>

				<!-- Corner -->
				<div class="ruler-corner" />

				<!-- Canvas container with mouse tracking -->
				<div
					class="canvas-mouse-tracker"
					@mousemove="handleCanvasMouseMove"
					@mouseleave="handleMouseLeave"
				>
					<canvas
						ref="canvasRef"
						:style="canvasStyle"
						@mousedown="handleMouseDown"
						@mousemove="handleMouseMove"
						@mouseup="handleMouseUp"
					/>

					<!-- Selection box overlay -->
					<div
						v-if="isSelecting && selectionStart && selectionEnd"
						class="selection-box"
						:style="selectionBoxStyle"
					/>
				</div>

				<!-- Position indicator -->
				<div
					v-if="mousePos"
					class="position-indicator"
				>
					X: {{ mousePos.x.toFixed(1) }}mm Y: {{ mousePos.y.toFixed(1) }}mm
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, nextTick } from 'vue';
import type { TitlePageElement, TitlePageElementType } from '@/shared/types/titlePage';
import { mmToPx, PAGE_WIDTH_MM, PAGE_HEIGHT_MM } from '@/shared/utils/canvasUtils';
import {
	drawPageBackground,
	drawElement,
	drawHover,
	drawSelection,
	drawAlignmentGuides,
	drawDistanceLines,
} from './canvasRenderer';
import { useTitlePageCanvas } from './useTitlePageCanvas';
import TitlePageCanvasToolbar from './TitlePageCanvasToolbar.vue';
import TitlePageCanvasRuler from './TitlePageCanvasRuler.vue';

interface Props {
	elements: TitlePageElement[];
	selectedElementIds: string[];
	initialTool?: TitlePageElementType | null;
}

interface Emits {
	(e: 'element-select', ids: string[]): void;
	(e: 'element-toggle', id: string): void;
	(e: 'element-move', id: string, x: number, y: number): void;
	(e: 'elements-move', ids: string[], deltaX: number, deltaY: number): void;
	(e: 'element-add', type: TitlePageElementType, x: number, y: number): void;
	(e: 'move-end'): void;
	(e: 'tool-change', tool: TitlePageElementType | null): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const elementsRef = ref([...props.elements]);
const selectedElementIdsRef = ref([...props.selectedElementIds]);

watch(
	() => props.elements,
	(newElements) => {
		elementsRef.value = [...newElements]; // Создаем новый массив для реактивности
		nextTick(() => {
			draw();
		});
	},
	{ deep: true }
);

watch(
	() => props.selectedElementIds,
	(newIds) => {
		selectedElementIdsRef.value = [...newIds];
		nextTick(() => {
			draw();
		});
	},
	{ deep: true }
);

const {
	canvasRef,
	tool,
	zoom,
	mousePos,
	hoveredElementId,
	isDragging,
	isSelecting,
	selectionStart,
	selectionEnd,
	alignmentGuides,
	elementDistances,
	handleToolChange: handleToolChangeInternal,
	handleZoomChange: handleZoomChangeInternal,
	handleMouseDown,
	handleMouseMove,
	handleCanvasMouseMove,
	handleMouseUp,
	handleMouseLeave,
} = useTitlePageCanvas({
	elements: elementsRef,
	selectedElementIds: selectedElementIdsRef,
	onElementSelect: (ids) => emit('element-select', ids),
	onElementToggle: (id) => emit('element-toggle', id),
	onElementMove: (id, x, y) => emit('element-move', id, x, y),
	onElementsMove: (ids, deltaX, deltaY) =>
		emit('elements-move', ids, deltaX, deltaY),
	onElementAdd: (type, x, y) => emit('element-add', type, x, y),
	onMoveEnd: () => emit('move-end'),
	onToolChange: (tool) => emit('tool-change', tool),
	initialTool: props.initialTool,
});

const canvasStyle = computed(() => ({
	cursor: tool.value ? 'crosshair' : isDragging.value ? 'move' : 'default',
	display: 'block',
}));

const selectionBoxStyle = computed(() => {
	if (!selectionStart.value || !selectionEnd.value) return {};
	const minX = Math.min(selectionStart.value.x, selectionEnd.value.x);
	const minY = Math.min(selectionStart.value.y, selectionEnd.value.y);
	return {
		position: 'absolute',
		left: `${minX * zoom.value}px`,
		top: `${minY * zoom.value}px`,
		width: `${Math.abs(selectionEnd.value.x - selectionStart.value.x) * zoom.value}px`,
		height: `${Math.abs(selectionEnd.value.y - selectionStart.value.y) * zoom.value}px`,
		border: '1px dashed #0066ff',
		background: 'rgba(0, 102, 255, 0.1)',
		pointerEvents: 'none',
	};
});

function handleToolChange(tool: TitlePageElementType | null) {
	handleToolChangeInternal(tool);
}

function handleZoomChange(newZoom: number) {
	handleZoomChangeInternal(newZoom);
	nextTick(() => {
		updateCanvasSize();
		draw();
	});
}

function updateCanvasSize() {
	const canvas = canvasRef.value;
	if (!canvas) return;

	const width = mmToPx(PAGE_WIDTH_MM);
	const height = mmToPx(PAGE_HEIGHT_MM);

	canvas.width = width;
	canvas.height = height;
	canvas.style.width = `${width}px`;
	canvas.style.height = `${height}px`;
}

function draw() {
	const canvas = canvasRef.value;
	if (!canvas) return;

	const ctx = canvas.getContext('2d');
	if (!ctx) return;

	// Clear canvas
	ctx.clearRect(0, 0, canvas.width, canvas.height);

	// Draw page background and border
	drawPageBackground(ctx);

	// Draw elements
	elementsRef.value.forEach((element) => {
		drawElement(element, ctx);

		// Draw hover border (lighter than selection)
		const isHovered =
			element.id === hoveredElementId.value &&
			!selectedElementIdsRef.value.includes(element.id);
		if (isHovered) {
			drawHover(element, ctx);
		}

		// Draw selection border
		if (selectedElementIdsRef.value.includes(element.id)) {
			drawSelection(element, ctx);
		}
	});

	// Draw alignment guides (on top of everything)
	if (alignmentGuides.value.length > 0) {
		drawAlignmentGuides(alignmentGuides.value, ctx);
	}

	// Draw distance lines if dragging a single element
	if (
		isDragging.value &&
		elementDistances.value &&
		selectedElementIdsRef.value.length === 1
	) {
		const draggedElement = elementsRef.value.find(
			(el) => el.id === selectedElementIdsRef.value[0]
		);
		if (draggedElement) {
			drawDistanceLines(draggedElement, elementDistances.value, ctx);
		}
	}
}

watch(
	() => elementsRef.value,
	() => {
		nextTick(() => {
			draw();
		});
	},
	{ deep: true, immediate: false }
);

watch(
	[
		() => selectedElementIdsRef.value,
		hoveredElementId,
		alignmentGuides,
		isDragging,
		elementDistances,
	],
	() => {
		nextTick(() => {
			draw();
		});
	},
	{ deep: true }
);

onMounted(() => {
	updateCanvasSize();
	draw();
});
</script>

<style scoped>
.canvas-container {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.canvas-wrapper {
	border: 1px solid #27272a;
	display: inline-block;
	background: #18181b;
	padding: 20px;
	overflow: auto;
	max-width: 100%;
	border-radius: 8px;
}

.canvas-zoom-container {
	display: inline-block;
	position: relative;
}

.ruler-corner {
	position: absolute;
	top: -20px;
	left: -20px;
	width: 20px;
	height: 20px;
	background: #27272a;
	border-right: 1px solid #3f3f46;
	border-bottom: 1px solid #3f3f46;
}

.canvas-mouse-tracker {
	margin-top: 20px;
	margin-left: 20px;
	position: relative;
}

.position-indicator {
	position: absolute;
	top: -20px;
	right: 0;
	background: #6366f1;
	color: #fff;
	padding: 2px 6px;
	font-size: 10px;
	border-radius: 2px;
	pointer-events: none;
	white-space: nowrap;
}

.selection-box {
	position: absolute;
}
</style>
