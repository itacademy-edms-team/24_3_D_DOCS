<template>
	<div class="title-page-editor">
		<!-- Toolbar -->
		<div class="title-page-editor__toolbar">
			<button class="title-page-editor__back-btn" @click="handleBack" title="Назад">
				<Icon name="arrow_back" size="18" ariaLabel="Назад" />
				<span class="title-page-editor__back-text">Назад</span>
			</button>
			<input
				v-model="titlePageName"
				@input="handleNameChange"
				class="title-page-editor__title"
				type="text"
				placeholder="Название титульника"
			/>
			<div class="title-page-editor__tools">
				<button
					v-for="tool in TOOLS"
					:key="tool.value"
					:class="['title-page-editor__tool', { 'title-page-editor__tool--active': activeTool === tool.value }]"
					@click="activeTool = tool.value"
					:title="tool.label"
				>
					<Icon 
						:name="tool.value === 'select' ? 'cursor' : tool.value === 'text' ? 'text_fields' : tool.value === 'variable' ? 'code' : 'horizontal_rule'" 
						size="16" 
						:ariaLabel="tool.label"
					/>
					<span class="title-page-editor__tool-label">{{ tool.label }}</span>
				</button>
			</div>
			<div class="title-page-editor__controls">
				<button 
					class="title-page-editor__control-btn"
					@click="saveTitlePage"
					title="Сохранить (Ctrl+S)"
				>
					<Icon name="save" size="16" ariaLabel="Сохранить" />
					<span class="title-page-editor__control-label">Сохранить</span>
				</button>
				<button 
					class="title-page-editor__control-btn"
					@click="clipboard.copySelectedElements"
					title="Копировать (Ctrl+C)"
				>
					<Icon name="copy" size="16" ariaLabel="Копировать" />
					<span class="title-page-editor__control-label">Копировать</span>
				</button>
				<button 
					class="title-page-editor__control-btn"
					@click="clipboard.pasteElements"
					title="Вставить (Ctrl+V)"
				>
					<Icon name="paste" size="16" ariaLabel="Вставить" />
					<span class="title-page-editor__control-label">Вставить</span>
				</button>
				<button 
					class="title-page-editor__control-btn"
					@click="clipboard.cutSelectedElements"
					title="Вырезать (Ctrl+X)"
				>
					<Icon name="cut" size="16" ariaLabel="Вырезать" />
					<span class="title-page-editor__control-label">Вырезать</span>
				</button>
				<button 
					class="title-page-editor__control-btn"
					@click="clipboard.duplicateSelectedElements"
					title="Дублировать (Ctrl+D)"
				>
					<Icon name="content_copy" size="16" ariaLabel="Дублировать" />
					<span class="title-page-editor__control-label">Дублировать</span>
				</button>
				<button 
					class="title-page-editor__control-btn"
					@click="clipboard.selectAllElements"
					title="Выделить все (Ctrl+A)"
				>
					<Icon name="select_all" size="16" ariaLabel="Выделить все" />
					<span class="title-page-editor__control-label">Выделить все</span>
				</button>
				<ZoomDropdown
					v-model="zoom"
					:options="zoomOptions as any"
				/>
				<label class="title-page-editor__checkbox">
					<input type="checkbox" v-model="showGrid" />
					<Icon name="grid_on" size="16" ariaLabel="Сетка" />
					<span class="title-page-editor__checkbox-label">Сетка</span>
				</label>
				<button 
					class="title-page-editor__control-btn"
					@click="history.undo" 
					:disabled="!history.canUndo"
					title="Отменить (Ctrl+Z)"
				>
					<Icon name="redo" size="16" ariaLabel="Отменить" style="transform: scaleX(-1);" />
				</button>
				<button 
					class="title-page-editor__control-btn"
					@click="history.redo" 
					:disabled="!history.canRedo"
					title="Повторить (Ctrl+Shift+Z)"
				>
					<Icon name="redo" size="16" ariaLabel="Повторить" />
				</button>
			</div>
		</div>

		<div class="title-page-editor__main">
			<!-- Canvas Area (always full width) -->
			<div class="title-page-editor__canvas-area">
				<div class="title-page-editor__canvas-wrapper">
					<div 
						class="title-page-editor__scaled-content"
						:style="{ 
							transform: `scale(${zoom / 100})`, 
							transformOrigin: 'top left',
							width: `${20 + A4_WIDTH_PX}px`,
							minWidth: `${20 + A4_WIDTH_PX}px`
						}"
					>
						<!-- Rulers -->
						<TitlePageRulers
							:width="A4_WIDTH_PX"
							:height="A4_HEIGHT_PX"
							:horizontal-marks="horizontalRulerMarks"
							:vertical-marks="verticalRulerMarks"
							:mouse-pos="mousePos"
						/>

						<!-- Canvas -->
						<div class="title-page-editor__canvas-container">
							<canvas
								ref="canvasRef"
								class="title-page-editor__canvas"
								:style="{ width: `${A4_WIDTH_PX}px`, height: `${A4_HEIGHT_PX}px` }"
							/>
						</div>
					</div>
				</div>
			</div>

			<!-- Position Panel (absolute positioned) -->
			<Transition name="panel-left">
				<div 
					v-if="selectedElement"
					class="title-page-editor__position-panel-wrapper"
					:style="{ width: `${positionPanelWidth}px` }"
				>
					<TitlePagePositionPanel
						:selected-element="selectedElement as any"
						:canvas="canvas as any"
						:on-save="debouncedSaveTitlePage"
					/>
					<div 
						class="title-page-editor__resizer title-page-editor__resizer--right"
						@mousedown="startResize('position', $event)"
					></div>
				</div>
			</Transition>

			<!-- Properties Panel (absolute positioned) -->
			<Transition name="panel-right">
				<div 
					v-if="selectedElement"
					class="title-page-editor__properties-panel-wrapper"
					:style="{ width: `${propertiesPanelWidth}px` }"
				>
					<div 
						class="title-page-editor__resizer title-page-editor__resizer--left"
						@mousedown="startResize('properties', $event)"
					></div>
					<TitlePagePropertiesPanel
						:selected-element="selectedElement as any"
						:canvas="canvas as any"
						:on-save="debouncedSaveTitlePage"
						@delete="handleDeleteElement"
					/>
				</div>
			</Transition>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, nextTick, computed } from 'vue';
import { useStorage, useDebounceFn } from '@vueuse/core';
import { useRoute, useRouter } from 'vue-router';
import { Canvas, type FabricObject } from 'fabric';
import ZoomDropdown from '@/shared/ui/ZoomDropdown/ZoomDropdown.vue';
import Icon from '@/components/Icon.vue';
import TitlePageRulers from '@/widgets/title-page-rulers/TitlePageRulers.vue';
import TitlePagePropertiesPanel from '@/widgets/title-page-properties/TitlePagePropertiesPanel.vue';
import TitlePagePositionPanel from '@/widgets/title-page-position/TitlePagePositionPanel.vue';
import { A4_WIDTH_PX, A4_HEIGHT_PX, TOOLS, type ToolType, type MousePosition } from '@/entities/title-page/constants';
import { useTitlePageRulers } from '@/app/composables/useTitlePageRulers';
import { useTitlePageZoom } from '@/app/composables/useTitlePageZoom';
import { useTitlePageCanvas } from '@/app/composables/useTitlePageCanvas';
import { useTitlePageSnapGuides } from '@/app/composables/useTitlePageSnapGuides';
import { useTitlePageHistory } from '@/app/composables/useTitlePageHistory';
import { useTitlePageClipboard } from '@/app/composables/useTitlePageClipboard';
import { useTitlePageKeyboard } from '@/app/composables/useTitlePageKeyboard';
import { useTitlePageSave } from '@/app/composables/useTitlePageSave';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';

const route = useRoute();
const router = useRouter();
const titlePageId = route.params.id as string;

const canvasRef = ref<HTMLCanvasElement>();
const selectedElement = ref<FabricObject | null>(null);
const activeTool = ref<ToolType>('select');
const mousePos = ref<MousePosition | null>(null);
const titlePageName = ref<string>('');
const currentTitlePage = ref<any>(null);

// Panel widths with persistence
const panelWidths = useStorage<{ position: number; properties: number }>('title-page-editor-panel-widths', {
	position: 200,
	properties: 320,
});

const positionPanelWidth = computed({
	get: () => panelWidths.value.position,
	set: (val) => { panelWidths.value.position = val; }
});

const propertiesPanelWidth = computed({
	get: () => panelWidths.value.properties,
	set: (val) => { panelWidths.value.properties = val; }
});

// Resize handling
let resizeTarget: 'position' | 'properties' | null = null;
let startX = 0;
let startWidth = 0;

function startResize(target: 'position' | 'properties', e: MouseEvent) {
	e.preventDefault();
	resizeTarget = target;
	startX = e.clientX;
	startWidth = target === 'position' ? panelWidths.value.position : panelWidths.value.properties;
	
	document.addEventListener('mousemove', handleResize);
	document.addEventListener('mouseup', stopResize);
}

function handleResize(e: MouseEvent) {
	if (!resizeTarget) return;
	
	const deltaX = resizeTarget === 'position' ? e.clientX - startX : startX - e.clientX;
	const newWidth = Math.max(150, Math.min(500, startWidth + deltaX));
	
	if (resizeTarget === 'position') {
		panelWidths.value.position = newWidth;
	} else {
		panelWidths.value.properties = newWidth;
	}
}

function stopResize() {
	resizeTarget = null;
	document.removeEventListener('mousemove', handleResize);
	document.removeEventListener('mouseup', stopResize);
}

// Composables
const { horizontalRulerMarks, verticalRulerMarks } = useTitlePageRulers();
const { zoom, showGrid, toggleGrid, zoomOptions } = useTitlePageZoom();
const canvas = ref<Canvas | null>(null);
const isAltPressed = ref(false);

// Create temporary refs for composables that need canvas
const tempSnapGuides = useTitlePageSnapGuides(
	canvas as any,
	isAltPressed,
	selectedElement as any
);

const tempHistory = useTitlePageHistory(canvas as any, {
	canvasRef,
	selectedElement: selectedElement as any,
	elementDistances: tempSnapGuides.elementDistances,
});

const { loadTitlePage, saveTitlePage, debouncedSaveTitlePage } = useTitlePageSave(
	canvas as any,
	titlePageId,
	() => {
		tempHistory.initHistory();
	}
);

const handleBack = () => {
	router.push('/dashboard');
};

const handleNameChange = useDebounceFn(async () => {
	if (!titlePageId) return;
	
	const newName = titlePageName.value.trim() || 'Новый титульник';
	const previousName = currentTitlePage.value?.name || '';
	
	// Пропускаем если название не изменилось
	if (newName === previousName) return;
	
	try {
		await TitlePageAPI.update(titlePageId, {
			name: newName,
		});
		// Обновляем локальное состояние после успешного сохранения
		if (currentTitlePage.value) {
			currentTitlePage.value.name = newName;
		}
	} catch (error) {
		console.error('Failed to update title page name:', error);
		// Откатываем к предыдущему значению при ошибке
		titlePageName.value = previousName;
		alert('Ошибка при сохранении названия титульника');
	}
}, 1000);

const loadTitlePageData = async () => {
	if (!titlePageId) return;
	try {
		const titlePage = await TitlePageAPI.getById(titlePageId);
		currentTitlePage.value = titlePage;
		titlePageName.value = titlePage.name || '';
	} catch (error) {
		console.error('Failed to load title page data:', error);
	}
};

// Handle delete element
function handleDeleteElement() {
	if (!canvas.value) return;
	
	const activeObjects = canvas.value.getActiveObjects();
	const objectsToDelete = activeObjects.length > 0 
		? activeObjects 
		: (selectedElement.value ? [selectedElement.value] : []);
	
	if (objectsToDelete.length === 0) return;
	
	objectsToDelete.forEach((obj) => {
		canvas.value!.remove(obj as any);
	});
	
	canvas.value.discardActiveObject();
	selectedElement.value = null;
	canvas.value.renderAll();
	debouncedSaveTitlePage();
}

const clipboard = useTitlePageClipboard(
	canvas as any,
	selectedElement as any,
	tempHistory.saveHistoryState,
	debouncedSaveTitlePage
);

// Initialize canvas composable with all handlers
const canvasComposable = useTitlePageCanvas(canvasRef, {
	showGrid,
	activeTool,
	activeSnapGuides: tempSnapGuides.activeSnapGuides,
	elementDistances: tempSnapGuides.elementDistances,
	selectedElement: selectedElement as any,
	mousePos,
	onObjectMoving: (obj, excludeId) => {
		tempSnapGuides.handleObjectMoving(obj, excludeId);
	},
	onObjectModified: () => {
		tempSnapGuides.clearGuides();
		tempSnapGuides.updateSelectedElementDistances();
		tempHistory.saveHistoryState();
		debouncedSaveTitlePage();
	},
	onObjectMoved: () => {
		tempSnapGuides.clearGuides();
	},
	onSelectionCreated: (e) => {
		selectedElement.value = (e.selected?.[0] || null) as any;
		tempSnapGuides.updateSelectedElementDistances();
	},
	onSelectionUpdated: (e) => {
		selectedElement.value = (e.selected?.[0] || null) as any;
		tempSnapGuides.updateSelectedElementDistances();
	},
	onSelectionCleared: () => {
		selectedElement.value = null;
		tempSnapGuides.elementDistances.value = null;
		tempSnapGuides.activeSnapGuides.value = [];
		tempSnapGuides.alignmentGuides.value = [];
	},
	onObjectAdded: () => {
		tempHistory.saveHistoryState();
		debouncedSaveTitlePage();
	},
	onObjectRemoved: () => {
		tempHistory.saveHistoryState();
		debouncedSaveTitlePage();
	},
	onMouseDown: (e) => {
		// Create elements only if creation tool is active and click was on empty space
		if (activeTool.value === 'select' || e.target) {
			return;
		}

		if (!canvasComposable.canvas.value) return;
		const pointer = canvasComposable.canvas.value.getPointer(e.e);

		if (activeTool.value === 'text') {
			const textObj = canvasComposable.createTextElement(pointer.x, pointer.y);
			if (textObj) {
				selectedElement.value = textObj as any;
			}
			activeTool.value = 'select';
		} else if (activeTool.value === 'variable') {
			const textObj = canvasComposable.createVariableElement(pointer.x, pointer.y);
			if (textObj) {
				selectedElement.value = textObj as any;
			}
			activeTool.value = 'select';
		} else if (activeTool.value === 'line') {
			const lineObj = canvasComposable.createLineElement(pointer.x, pointer.y);
			if (lineObj) {
				selectedElement.value = lineObj as any;
			}
			activeTool.value = 'select';
		}
	},
	onHistorySave: () => {
		tempHistory.saveHistoryState();
	},
	onSave: debouncedSaveTitlePage,
});

// Sync canvas ref with canvasComposable canvas
watch(canvasComposable.canvas, (newCanvas) => {
	canvas.value = newCanvas;
}, { immediate: true });

// Export for use in template
const snapGuides = tempSnapGuides;
const history = tempHistory;

// Keyboard shortcuts
const keyboard = useTitlePageKeyboard({
	canvas: canvas as any,
	activeTool,
	selectedElement: selectedElement as any,
	showGrid,
	toggleGrid,
	onUndo: () => history.undo(),
	onRedo: () => history.redo(),
	onCopy: () => clipboard.copySelectedElements(),
	onPaste: () => clipboard.pasteElements(),
	onCut: () => clipboard.cutSelectedElements(),
	onDuplicate: () => clipboard.duplicateSelectedElements(),
	onSelectAll: () => clipboard.selectAllElements(),
	onDelete: handleDeleteElement,
	onSave: () => saveTitlePage(),
});

// Sync keyboard Alt state with snap guides
watch(keyboard.isAltPressed, (isPressed) => {
	isAltPressed.value = isPressed ?? false;
	if (!isPressed) {
		snapGuides.clearGuides();
	}
});

// Watch grid changes for re-render
watch(showGrid, () => {
	if (canvas.value) {
		nextTick(() => {
			canvas.value?.renderAll();
		});
	}
});

onMounted(async () => {
	canvasComposable.initCanvas();
	canvas.value = canvasComposable.canvas.value;
	await loadTitlePageData();
	loadTitlePage();
});

onUnmounted(() => {
	if (canvas.value) {
		canvas.value.off();
		canvas.value.dispose();
		canvas.value = null;
	}
});
</script>

<style scoped>
.title-page-editor {
	display: flex;
	flex-direction: column;
	height: 100vh;
	background: var(--bg-primary);
}

.title-page-editor__toolbar {
	display: flex;
	align-items: center;
	gap: var(--spacing-md);
	justify-content: space-between;
	padding: 12px 20px;
	border-bottom: 1px solid var(--border-color);
	flex-shrink: 0;
	position: relative;
	z-index: 10;
	background: var(--bg-primary);
	flex-wrap: wrap;
}

.title-page-editor__back-btn {
	display: flex;
	align-items: center;
	gap: 6px;
	background: transparent;
	border: none;
	color: var(--text-secondary);
	cursor: pointer;
	font-size: 14px;
	padding: 8px 12px;
	border-radius: 8px;
	transition: all 0.2s ease;
	white-space: nowrap;
}

.title-page-editor__back-btn:hover {
	color: var(--text-primary);
	background: var(--bg-secondary);
}

.title-page-editor__back-text {
	font-weight: 500;
}

.title-page-editor__title {
	font-size: 16px;
	font-weight: 600;
	color: var(--text-primary);
	margin: 0;
	background: transparent;
	border: 1px solid transparent;
	border-radius: 8px;
	padding: 8px 12px;
	transition: all 0.2s ease;
	min-width: 200px;
	max-width: 300px;
	font-family: inherit;
}

.title-page-editor__title:hover {
	border-color: var(--border-hover);
	background: var(--bg-secondary);
}

.title-page-editor__title:focus {
	outline: none;
	border-color: var(--accent);
	background: var(--bg-secondary);
	box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}

/* Адаптивность */
@media (max-width: 1200px) {
	.title-page-editor__toolbar {
		padding: 10px 16px;
		gap: 12px;
	}
	
	.title-page-editor__control-label {
		display: none;
	}
	
	.title-page-editor__tool-label {
		display: none;
	}
	
	.title-page-editor__back-text {
		display: none;
	}
	
	.title-page-editor__title {
		min-width: 150px;
		max-width: 200px;
	}
}

@media (max-width: 768px) {
	.title-page-editor__toolbar {
		padding: 8px 12px;
		gap: 8px;
	}
	
	.title-page-editor__tools {
		order: 3;
		width: 100%;
		justify-content: center;
		margin-top: 8px;
	}
	
	.title-page-editor__controls {
		order: 2;
		flex: 1;
		justify-content: flex-end;
	}
	
	.title-page-editor__title {
		min-width: 120px;
		max-width: 150px;
		font-size: 14px;
	}
}

.title-page-editor__tools {
	display: flex;
	gap: 8px;
	flex: 1;
	flex-wrap: wrap;
	min-width: 0;
}

.title-page-editor__tool {
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 8px 12px;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: 8px;
	color: var(--text-primary);
	font-size: 13px;
	cursor: pointer;
	transition: all 0.2s ease;
	white-space: nowrap;
}

.title-page-editor__tool:hover {
	background: var(--bg-tertiary);
	border-color: var(--border-hover);
	transform: translateY(-1px);
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.title-page-editor__tool--active {
	background: var(--accent-light);
	color: var(--accent);
	border-color: var(--accent);
	font-weight: 500;
}

.title-page-editor__tool-label {
	font-weight: inherit;
}

.title-page-editor__controls {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
}

.title-page-editor__control-btn {
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 8px 12px;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: 8px;
	color: var(--text-primary);
	font-size: 13px;
	cursor: pointer;
	transition: all 0.2s ease;
	white-space: nowrap;
}

.title-page-editor__control-btn:hover:not(:disabled) {
	background: var(--bg-tertiary);
	border-color: var(--border-hover);
	transform: translateY(-1px);
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.title-page-editor__control-btn:active:not(:disabled) {
	transform: translateY(0);
}

.title-page-editor__control-btn:disabled {
	opacity: 0.5;
	cursor: not-allowed;
	transform: none;
}

.title-page-editor__control-label {
	font-weight: 500;
}

.title-page-editor__checkbox {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 13px;
	color: var(--text-primary);
	cursor: pointer;
	padding: 8px 12px;
	border-radius: 8px;
	transition: all 0.2s ease;
	user-select: none;
}

.title-page-editor__checkbox:hover {
	background: var(--bg-secondary);
}

.title-page-editor__checkbox input[type="checkbox"] {
	margin: 0;
	cursor: pointer;
}

.title-page-editor__checkbox-label {
	font-weight: 500;
}

.title-page-editor__main {
	flex: 1;
	overflow: hidden;
	position: relative;
}

.title-page-editor__canvas-area {
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	overflow: auto;
	background: var(--bg-tertiary);
	padding: var(--spacing-lg);
	display: flex;
	justify-content: center;
}

.title-page-editor__position-panel-wrapper {
	position: absolute;
	top: 0;
	left: 0;
	bottom: 0;
	background: var(--bg-secondary);
	border-right: 1px solid var(--border-color);
	overflow: hidden;
	z-index: 5;
	transition: width 0.2s ease, opacity 0.2s ease;
}

.title-page-editor__properties-panel-wrapper {
	position: absolute;
	top: 0;
	right: 0;
	bottom: 0;
	background: var(--bg-secondary);
	border-left: 1px solid var(--border-color);
	overflow: hidden;
	z-index: 5;
	transition: width 0.2s ease, opacity 0.2s ease;
}

.title-page-editor__resizer {
	position: absolute;
	top: 0;
	bottom: 0;
	width: 4px;
	cursor: col-resize;
	background: transparent;
	z-index: 10;
	transition: background 0.2s ease;
}

.title-page-editor__resizer:hover {
	background: var(--accent);
}

.title-page-editor__resizer--left {
	left: 0;
}

.title-page-editor__resizer--right {
	right: 0;
}

/* Panel animations */
.panel-left-enter-active,
.panel-left-leave-active {
	transition: opacity 0.2s ease, transform 0.2s ease;
}

.panel-left-enter-from {
	opacity: 0;
	transform: translateX(-20px);
}

.panel-left-leave-to {
	opacity: 0;
	transform: translateX(-20px);
}

.panel-right-enter-active,
.panel-right-leave-active {
	transition: opacity 0.2s ease, transform 0.2s ease;
}

.panel-right-enter-from {
	opacity: 0;
	transform: translateX(20px);
}

.panel-right-leave-to {
	opacity: 0;
	transform: translateX(20px);
}

.title-page-editor__canvas-wrapper {
	position: relative;
	display: inline-block;
}

.title-page-editor__scaled-content {
	position: relative;
	display: inline-block;
}

.title-page-editor__canvas-container {
	position: relative;
	margin-left: 20px;
	margin-top: 0;
	display: inline-block;
}

.title-page-editor__canvas {
	border: 1px solid var(--border-color);
	box-shadow: var(--shadow-lg);
	display: block;
}

/* Apply thin plus cursor to Fabric.js upper canvas */
.title-page-editor__canvas-container :deep(.upper-canvas) {
	cursor: url("data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0Ij4KCTxsaW5lIHgxPSIxMiIgeTE9IjIiIHgyPSIxMiIgeTI9IjIyIiBzdHJva2U9IiNmZmZmZmYiIHN0cm9rZS13aWR0aD0iMi41IiBzdHJva2UtbGluZWNhcD0icm91bmQiLz4KCTxsaW5lIHgxPSIyIiB5MT0iMTIiIHgyPSIyMiIgeTI9IjEyIiBzdHJva2U9IiNmZmZmZmYiIHN0cm9rZS13aWR0aD0iMi41IiBzdHJva2UtbGluZWNhcD0icm91bmQiLz4KCTxsaW5lIHgxPSIxMiIgeTE9IjIiIHgyPSIxMiIgeTI9IjIyIiBzdHJva2U9IiMwMDAwMDAiIHN0cm9rZS13aWR0aD0iMS41IiBzdHJva2UtbGluZWNhcD0icm91bmQiLz4KCTxsaW5lIHgxPSIyIiB5MT0iMTIiIHgyPSIyMiIgeTI9IjEyIiBzdHJva2U9IiMwMDAwMDAiIHN0cm9rZS13aWR0aD0iMS41IiBzdHJva2UtbGluZWNhcD0icm91bmQiLz4KPC9zdmc+"), auto !important;
}
</style>
