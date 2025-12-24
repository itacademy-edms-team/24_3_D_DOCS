import { ref, computed, watch, onMounted, onUnmounted, type Ref } from 'vue';
import type { TitlePageElement, TitlePageElementType } from '@/shared/types/titlePage';
import {
	getCanvasCoords,
	pxToMm,
	getDistanceToElement,
	findAlignmentGuides,
	applySnap,
	calculateElementDistances,
	type AlignmentGuide,
} from '@/shared/utils/canvasUtils';
import {
	findElementAtPoint,
	calculateLineHitbox,
	calculateTextHitbox,
} from './canvasHitDetection';
import type { ElementDistances } from './canvasRenderer';

interface UseTitlePageCanvasProps {
	elements: Ref<TitlePageElement[]> | TitlePageElement[];
	selectedElementIds: Ref<string[]> | string[];
	onElementSelect: (ids: string[]) => void;
	onElementToggle: (id: string) => void;
	onElementMove: (id: string, x: number, y: number) => void;
	onElementsMove: (ids: string[], deltaX: number, deltaY: number) => void;
	onElementAdd: (type: TitlePageElementType, x: number, y: number) => void;
	onMoveEnd?: () => void;
	onToolChange?: (tool: TitlePageElementType | null) => void;
	initialTool?: TitlePageElementType | null;
}

export function useTitlePageCanvas({
	elements: elementsProp,
	selectedElementIds: selectedElementIdsProp,
	onElementSelect,
	onElementToggle,
	onElementMove,
	onElementsMove,
	onElementAdd,
	onMoveEnd,
	onToolChange,
	initialTool,
}: UseTitlePageCanvasProps) {
	const canvasRef = ref<HTMLCanvasElement | null>(null);
	const isDragging = ref(false);
	const dragStart = ref<{
		x: number;
		y: number;
		elementIds: string[];
		elementPositions: Map<string, { x: number; y: number }>;
	} | null>(null);
	const isSelecting = ref(false);
	const selectionStart = ref<{ x: number; y: number } | null>(null);
	const selectionEnd = ref<{ x: number; y: number } | null>(null);
	const tool = ref<TitlePageElementType | null>(initialTool || null);
	const zoom = ref(1.0);
	const mousePos = ref<{ x: number; y: number } | null>(null);
	const hoveredElementId = ref<string | null>(null);
	const alignmentGuides = ref<AlignmentGuide[]>([]);
	const isAltPressed = ref(false);
	const elementDistances = ref<ElementDistances | null>(null);

	// Convert props to refs if needed
	const elements = computed(() =>
		Array.isArray(elementsProp) ? elementsProp : elementsProp.value
	);
	const selectedElementIds = computed(() =>
		Array.isArray(selectedElementIdsProp)
			? selectedElementIdsProp
			: selectedElementIdsProp.value
	);

	// Sync tool with prop
	watch(
		() => initialTool,
		(newTool) => {
			if (newTool !== undefined) {
				tool.value = newTool;
			}
		},
		{ immediate: true }
	);

	// Alt key tracking
	const handleKeyDown = (e: KeyboardEvent) => {
		if (e.key === 'Alt') {
			isAltPressed.value = true;
		}
	};

	const handleKeyUp = (e: KeyboardEvent) => {
		if (e.key === 'Alt') {
			isAltPressed.value = false;
		}
	};

	onMounted(() => {
		window.addEventListener('keydown', handleKeyDown);
		window.addEventListener('keyup', handleKeyUp);
	});

	onUnmounted(() => {
		window.removeEventListener('keydown', handleKeyDown);
		window.removeEventListener('keyup', handleKeyUp);
	});

	const handleToolChange = (newTool: TitlePageElementType | null) => {
		tool.value = newTool;
		onToolChange?.(newTool);
	};

	const handleZoomChange = (newZoom: number) => {
		zoom.value = newZoom;
	};

	const handleMouseDown = (e: MouseEvent) => {
		const canvas = canvasRef.value;
		if (!canvas) return;

		const ctx = canvas.getContext('2d');
		if (!ctx) return;

		const coords = getCanvasCoords(e, canvas, zoom.value);
		const x = pxToMm(coords.x);
		const y = pxToMm(coords.y);

		const clickedElement = findElementAtPoint(
			x,
			y,
			elements.value,
			ctx,
			getDistanceToElement
		);

		if (tool.value) {
			// Add new element
			onElementAdd(tool.value, x, y);
			tool.value = null;
			handleToolChange(null);
		} else if (clickedElement) {
			const isCtrlPressed = e.ctrlKey || e.metaKey;

			if (isCtrlPressed) {
				// Toggle selection with Ctrl
				onElementToggle(clickedElement.id);
				// If element is now selected, prepare for dragging
				if (!selectedElementIds.value.includes(clickedElement.id)) {
					const newSelection = [...selectedElementIds.value, clickedElement.id];
					const elementPositions = new Map<string, { x: number; y: number }>();
					newSelection.forEach((id) => {
						const el = elements.value.find((e) => e.id === id);
						if (el) elementPositions.set(id, { x: el.x, y: el.y });
					});
					isDragging.value = true;
					dragStart.value = {
						x: coords.x,
						y: coords.y,
						elementIds: newSelection,
						elementPositions,
					};
				} else {
					// Element was deselected, don't start dragging
					isDragging.value = false;
					dragStart.value = null;
				}
			} else {
				// Normal click - select element(s)
				if (selectedElementIds.value.includes(clickedElement.id)) {
					// Already selected, prepare for dragging all selected
					const elementPositions = new Map<string, { x: number; y: number }>();
					selectedElementIds.value.forEach((id) => {
						const el = elements.value.find((e) => e.id === id);
						if (el) elementPositions.set(id, { x: el.x, y: el.y });
					});
					isDragging.value = true;
					dragStart.value = {
						x: coords.x,
						y: coords.y,
						elementIds: selectedElementIds.value,
						elementPositions,
					};
				} else {
					// Select only this element
					onElementSelect([clickedElement.id]);
					const elementPositions = new Map<string, { x: number; y: number }>();
					const el = elements.value.find((e) => e.id === clickedElement.id);
					if (el)
						elementPositions.set(clickedElement.id, { x: el.x, y: el.y });
					isDragging.value = true;
					dragStart.value = {
						x: coords.x,
						y: coords.y,
						elementIds: [clickedElement.id],
						elementPositions,
					};
				}
			}
		} else {
			// Click on empty space - start selection box or deselect
			if (e.ctrlKey || e.metaKey) {
				// Ctrl+click on empty space - keep selection, start selection box
				isSelecting.value = true;
				selectionStart.value = { x: coords.x, y: coords.y };
				selectionEnd.value = { x: coords.x, y: coords.y };
			} else {
				// Normal click on empty space - deselect and start selection box
				onElementSelect([]);
				isSelecting.value = true;
				selectionStart.value = { x: coords.x, y: coords.y };
				selectionEnd.value = { x: coords.x, y: coords.y };
			}
		}
	};

	const handleMouseMove = (e: MouseEvent) => {
		const canvas = canvasRef.value;
		if (!canvas) return;

		const ctx = canvas.getContext('2d');
		if (!ctx) return;

		const coords = getCanvasCoords(e, canvas, zoom.value);
		const mmX = pxToMm(coords.x);
		const mmY = pxToMm(coords.y);
		mousePos.value = { x: mmX, y: mmY };

		// Handle selection box
		if (isSelecting.value && selectionStart.value) {
			selectionEnd.value = { x: coords.x, y: coords.y };
		}

		// Update hover state if not dragging and not selecting
		if (!isDragging.value && !isSelecting.value) {
			const hoveredElement = findElementAtPoint(
				mmX,
				mmY,
				elements.value,
				ctx,
				getDistanceToElement
			);
			hoveredElementId.value = hoveredElement?.id || null;
		}

		// Handle dragging
		if (isDragging.value && dragStart.value) {
			const deltaX = pxToMm(coords.x - dragStart.value.x);
			const deltaY = pxToMm(coords.y - dragStart.value.y);

			// Move all selected elements
			if (dragStart.value.elementIds.length === 1) {
				const element = elements.value.find(
					(el) => el.id === dragStart.value!.elementIds[0]
				);
				if (element) {
					const originalPos = dragStart.value.elementPositions.get(element.id);
					if (originalPos) {
						let newX = originalPos.x + deltaX;
						let newY = originalPos.y + deltaY;

						// Apply snap if Alt is not pressed
						if (!isAltPressed.value) {
							const tempElement = { ...element, x: newX, y: newY };
							// Exclude the moving element itself from alignment check
							const guides = findAlignmentGuides(
								tempElement,
								elements.value,
								ctx,
								true,
								[element.id]
							);
							alignmentGuides.value = guides;

							if (guides.length > 0) {
								const snapped = applySnap(element, newX, newY, guides, ctx);
								newX = snapped.x;
								newY = snapped.y;
							}
						} else {
							alignmentGuides.value = [];
						}

						// Calculate and set distances for the element at new position
						const tempElementForDistances = { ...element, x: newX, y: newY };
						const distances = calculateElementDistances(
							tempElementForDistances,
							ctx
						);
						elementDistances.value = distances;

						onElementMove(element.id, newX, newY);
					}
				}
			} else {
				// Multiple elements - move all relative to their original positions
				const firstElement = elements.value.find(
					(el) => el.id === dragStart.value!.elementIds[0]
				);
				if (firstElement && !isAltPressed.value) {
					const originalPos = dragStart.value.elementPositions.get(
						firstElement.id
					);
					if (originalPos) {
						let newX = originalPos.x + deltaX;
						let newY = originalPos.y + deltaY;

						const tempElement = { ...firstElement, x: newX, y: newY };
						// Exclude all elements in the moving group from alignment check
						const guides = findAlignmentGuides(
							tempElement,
							elements.value,
							ctx,
							true,
							dragStart.value.elementIds
						);
						alignmentGuides.value = guides;

						if (guides.length > 0) {
							const snapped = applySnap(firstElement, newX, newY, guides, ctx);
							const adjustedDeltaX = snapped.x - originalPos.x;
							const adjustedDeltaY = snapped.y - originalPos.y;
							onElementsMove(
								dragStart.value.elementIds,
								adjustedDeltaX,
								adjustedDeltaY
							);
						} else {
							onElementsMove(dragStart.value.elementIds, deltaX, deltaY);
						}
					} else {
						onElementsMove(dragStart.value.elementIds, deltaX, deltaY);
					}
				} else {
					alignmentGuides.value = [];
					onElementsMove(dragStart.value.elementIds, deltaX, deltaY);
				}
			}
		}
	};

	const handleCanvasMouseMove = (e: MouseEvent) => {
		const canvas = canvasRef.value;
		if (!canvas) return;

		const rect = canvas.getBoundingClientRect();
		const coords = {
			x: (e.clientX - rect.left) / zoom.value,
			y: (e.clientY - rect.top) / zoom.value,
		};
		const mmX = pxToMm(coords.x);
		const mmY = pxToMm(coords.y);
		mousePos.value = { x: mmX, y: mmY };
	};

	const handleMouseUp = (e: MouseEvent) => {
		alignmentGuides.value = [];
		elementDistances.value = null;

		// Notify that move ended
		if (isDragging.value) {
			onMoveEnd?.();
		}
		const canvas = canvasRef.value;
		if (!canvas) return;

		const ctx = canvas.getContext('2d');
		if (!ctx) return;

		// Handle selection box completion
		if (isSelecting.value && selectionStart.value && selectionEnd.value) {
			const minX = Math.min(selectionStart.value.x, selectionEnd.value.x);
			const maxX = Math.max(selectionStart.value.x, selectionEnd.value.x);
			const minY = Math.min(selectionStart.value.y, selectionEnd.value.y);
			const maxY = Math.max(selectionStart.value.y, selectionEnd.value.y);

			// Convert selection box to mm for comparison
			const minXmm = pxToMm(minX);
			const maxXmm = pxToMm(maxX);
			const minYmm = pxToMm(minY);
			const maxYmm = pxToMm(maxY);

			const selectedIds: string[] = [];
			elements.value.forEach((el) => {
				// Check if element bounding box intersects with selection box
				let hitbox;
				if (el.type === 'line') {
					hitbox = calculateLineHitbox(el, 0);
				} else {
					hitbox = calculateTextHitbox(el, ctx, 0);
				}

				// Check if element bounding box intersects with selection box
				const intersects = !(
					hitbox.maxX < minXmm ||
					hitbox.minX > maxXmm ||
					hitbox.maxY < minYmm ||
					hitbox.minY > maxYmm
				);

				if (intersects) {
					selectedIds.push(el.id);
				}
			});

			if (selectedIds.length > 0) {
				onElementSelect(selectedIds);
			} else if (!e.ctrlKey && !e.metaKey) {
				// If no elements selected and not Ctrl, deselect all
				onElementSelect([]);
			}
		}

		isDragging.value = false;
		dragStart.value = null;
		isSelecting.value = false;
		selectionStart.value = null;
		selectionEnd.value = null;
	};

	const handleMouseLeave = () => {
		isDragging.value = false;
		dragStart.value = null;
		isSelecting.value = false;
		selectionStart.value = null;
		selectionEnd.value = null;
		alignmentGuides.value = [];
		elementDistances.value = null;
		mousePos.value = null;
		hoveredElementId.value = null;
	};

	return {
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
		handleToolChange,
		handleZoomChange,
		handleMouseDown,
		handleMouseMove,
		handleCanvasMouseMove,
		handleMouseUp,
		handleMouseLeave,
	};
}
