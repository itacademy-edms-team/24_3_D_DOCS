<template>
	<div class="title-page-position" v-if="selectedElement">
		<h3 class="title-page-position__title">Позиция</h3>
		<div class="title-page-position__content">
			<!-- Position inputs -->
			<div class="title-page-position__group">
				<label class="title-page-position__label">Позиция (мм)</label>
				<div class="title-page-position__row">
					<input
						type="number"
						class="title-page-position__input"
						:value="elementX"
						@input="updateElementPosition('x', $event)"
						step="0.1"
						placeholder="X"
					/>
					<input
						type="number"
						class="title-page-position__input"
						:value="elementY"
						@input="updateElementPosition('y', $event)"
						step="0.1"
						placeholder="Y"
					/>
				</div>
			</div>

			<!-- Arrow buttons -->
			<div class="title-page-position__group">
				<label class="title-page-position__label">Перемещение</label>
				<div class="title-page-position__arrows">
					<button
						class="title-page-position__arrow-btn"
						@click="handleMove('up')"
						@mousedown="startMove('up', $event)"
						@mouseup="stopMove"
						@mouseleave="stopMove"
						title="Вверх (↑, Shift+↑: 10мм, Ctrl+↑: 1px)"
					>
						↑
					</button>
					<div class="title-page-position__arrows-row">
						<button
							class="title-page-position__arrow-btn"
							@click="handleMove('left')"
							@mousedown="startMove('left', $event)"
							@mouseup="stopMove"
							@mouseleave="stopMove"
							title="Влево (←, Shift+←: 10мм, Ctrl+←: 1px)"
						>
							←
						</button>
						<button
							class="title-page-position__arrow-btn"
							@click="handleMove('right')"
							@mousedown="startMove('right', $event)"
							@mouseup="stopMove"
							@mouseleave="stopMove"
							title="Вправо (→, Shift+→: 10мм, Ctrl+→: 1px)"
						>
							→
						</button>
					</div>
					<button
						class="title-page-position__arrow-btn"
						@click="handleMove('down')"
						@mousedown="startMove('down', $event)"
						@mouseup="stopMove"
						@mouseleave="stopMove"
						title="Вниз (↓, Shift+↓: 10мм, Ctrl+↓: 1px)"
					>
						↓
					</button>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, type Ref } from 'vue';
import { Canvas, Line, type FabricObject } from 'fabric';
import { MM_TO_PX } from '@/entities/title-page/constants';
import { constrainToBounds } from '@/entities/title-page/utils/elementUtils';

interface Props {
	selectedElement: FabricObject | null;
	canvas: Canvas | null;
	onSave?: () => void;
}

const props = defineProps<Props>();

const selectedElementRef = ref<FabricObject | null>(props.selectedElement);
const canvasRef = ref<Canvas | null>(props.canvas);

// Sync props with refs
watch(() => props.selectedElement, (val) => {
	selectedElementRef.value = val;
}, { immediate: true });

watch(() => props.canvas, (val) => {
	canvasRef.value = val;
}, { immediate: true });

// Computed properties for position
const elementX = ref(0);
const elementY = ref(0);

const updatePositionValues = () => {
	if (!selectedElementRef.value) {
		elementX.value = 0;
		elementY.value = 0;
		return;
	}

	if (selectedElementRef.value.type === 'line') {
		const lineObj = selectedElementRef.value as Line;
		const x1 = lineObj.x1! / MM_TO_PX;
		const y1 = lineObj.y1! / MM_TO_PX;
		elementX.value = Math.round(x1 * 10) / 10;
		elementY.value = Math.round(y1 * 10) / 10;
	} else {
		const x = selectedElementRef.value.left! / MM_TO_PX;
		const y = selectedElementRef.value.top! / MM_TO_PX;
		elementX.value = Math.round(x * 10) / 10;
		elementY.value = Math.round(y * 10) / 10;
	}
};

watch(selectedElementRef, () => {
	updatePositionValues();
}, { immediate: true, deep: true });

// Watch for position changes during drag/move
watch(() => {
	if (!selectedElementRef.value) return null;
	if (selectedElementRef.value.type === 'line') {
		const lineObj = selectedElementRef.value as Line;
		return { x1: lineObj.x1, y1: lineObj.y1, x2: lineObj.x2, y2: lineObj.y2 };
	}
	return { left: selectedElementRef.value.left, top: selectedElementRef.value.top };
}, () => {
	updatePositionValues();
}, { deep: true });

// Update position from input
const updateElementPosition = (axis: 'x' | 'y', event: Event) => {
	if (!selectedElementRef.value || !canvasRef.value) return;
	const input = event.target as HTMLInputElement;
	const value = parseFloat(input.value);
	if (isNaN(value)) return;
	
	const newPos = value * MM_TO_PX;
	if (selectedElementRef.value.type === 'line') {
		const lineObj = selectedElementRef.value as Line;
		const deltaX = axis === 'x' ? newPos - lineObj.x1! : 0;
		const deltaY = axis === 'y' ? newPos - lineObj.y1! : 0;
		lineObj.set({
			x1: lineObj.x1! + deltaX,
			y1: lineObj.y1! + deltaY,
			x2: lineObj.x2! + deltaX,
			y2: lineObj.y2! + deltaY,
		});
	} else {
		if (axis === 'x') {
			selectedElementRef.value.set('left', newPos);
		} else {
			selectedElementRef.value.set('top', newPos);
		}
	}
	constrainToBounds(selectedElementRef.value);
	canvasRef.value.renderAll();
	updatePositionValues();
	props.onSave?.();
};

// Move element
const moveElement = (direction: 'up' | 'down' | 'left' | 'right', shiftKey: boolean, ctrlKey: boolean) => {
	if (!selectedElementRef.value || !canvasRef.value) return;

	let deltaX = 0;
	let deltaY = 0;
	let deltaXPx = 0;
	let deltaYPx = 0;

	if (ctrlKey) {
		// Pixel movement
		const moveAmount = shiftKey ? 10 : 1;
		if (direction === 'left') deltaXPx = -moveAmount;
		else if (direction === 'right') deltaXPx = moveAmount;
		else if (direction === 'up') deltaYPx = -moveAmount;
		else if (direction === 'down') deltaYPx = moveAmount;
	} else {
		// Millimeter movement
		const moveAmount = shiftKey ? 10 : 1;
		if (direction === 'left') deltaX = -moveAmount;
		else if (direction === 'right') deltaX = moveAmount;
		else if (direction === 'up') deltaY = -moveAmount;
		else if (direction === 'down') deltaY = moveAmount;
		deltaXPx = deltaX * MM_TO_PX;
		deltaYPx = deltaY * MM_TO_PX;
	}

	if (selectedElementRef.value.type === 'line') {
		const lineObj = selectedElementRef.value as Line;
		lineObj.set({
			x1: lineObj.x1! + deltaXPx,
			y1: lineObj.y1! + deltaYPx,
			x2: lineObj.x2! + deltaXPx,
			y2: lineObj.y2! + deltaYPx,
		});
	} else {
		selectedElementRef.value.set({
			left: selectedElementRef.value.left! + deltaXPx,
			top: selectedElementRef.value.top! + deltaYPx,
		});
	}
	constrainToBounds(selectedElementRef.value);
	canvasRef.value.renderAll();
	updatePositionValues();
	props.onSave?.();
};

// Handle button click
const handleMove = (direction: 'up' | 'down' | 'left' | 'right') => {
	moveElement(direction, false, false);
};

// Handle mouse down for continuous movement
let moveInterval: number | null = null;
let currentDirection: 'up' | 'down' | 'left' | 'right' | null = null;
let currentShift = false;
let currentCtrl = false;

const startMove = (direction: 'up' | 'down' | 'left' | 'right', event: MouseEvent) => {
	currentDirection = direction;
	currentShift = event.shiftKey;
	currentCtrl = event.ctrlKey || event.metaKey;
	
	// Move immediately
	moveElement(direction, currentShift, currentCtrl);
	
	// Then move continuously after delay
	moveInterval = window.setInterval(() => {
		if (currentDirection) {
			moveElement(currentDirection, currentShift, currentCtrl);
		}
	}, 100);
};

const stopMove = () => {
	if (moveInterval !== null) {
		clearInterval(moveInterval);
		moveInterval = null;
	}
	currentDirection = null;
};
</script>

<style scoped>
.title-page-position {
	width: 100%;
	height: 100%;
	padding: var(--spacing-lg);
	background: var(--bg-secondary);
	overflow-y: auto;
	display: flex;
	flex-direction: column;
}

.title-page-position__title {
	font-size: 16px;
	font-weight: 600;
	color: var(--text-primary);
	margin: 0 0 var(--spacing-md) 0;
}

.title-page-position__content {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
}

.title-page-position__group {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-xs);
}

.title-page-position__label {
	font-size: 13px;
	color: var(--text-primary);
	font-weight: 500;
}

.title-page-position__row {
	display: flex;
	gap: var(--spacing-sm);
}

.title-page-position__input {
	width: 100%;
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 14px;
}

.title-page-position__input:focus {
	outline: none;
	border-color: var(--accent);
}

.title-page-position__arrows {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-xs);
	align-items: center;
}

.title-page-position__arrows-row {
	display: flex;
	gap: var(--spacing-xs);
}

.title-page-position__arrow-btn {
	width: 40px;
	height: 40px;
	padding: 0;
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 20px;
	cursor: pointer;
	transition: all 0.2s ease;
	display: flex;
	align-items: center;
	justify-content: center;
	user-select: none;
}

.title-page-position__arrow-btn:hover {
	background: var(--bg-tertiary);
	border-color: var(--accent);
}

.title-page-position__arrow-btn:active {
	background: var(--accent-light);
	color: var(--accent);
}
</style>
