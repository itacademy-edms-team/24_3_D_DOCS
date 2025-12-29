import { ref, type Ref, watch, nextTick } from 'vue';
import { Canvas, Text, Line, type FabricObject } from 'fabric';
import { A4_WIDTH_PX, A4_HEIGHT_PX, GRID_SIZE_MM, MM_TO_PX, PT_TO_PX, type ToolType, type MousePosition, type ActiveSnapGuide, type ElementDistances } from '@/entities/title-page/constants';
import { constrainToBounds, getElementBounds } from '@/entities/title-page/utils/elementUtils';

/**
 * Custom thin plus sign cursor as SVG data URI
 * 24x24 pixels, thin 1.5px stroke, centered at 12,12
 * White outline for visibility on dark backgrounds, black fill for light backgrounds
 */
const THIN_PLUS_CURSOR = `data:image/svg+xml;base64,${btoa(`
<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
	<line x1="12" y1="2" x2="12" y2="22" stroke="#ffffff" stroke-width="2.5" stroke-linecap="round"/>
	<line x1="2" y1="12" x2="22" y2="12" stroke="#ffffff" stroke-width="2.5" stroke-linecap="round"/>
	<line x1="12" y1="2" x2="12" y2="22" stroke="#000000" stroke-width="1.5" stroke-linecap="round"/>
	<line x1="2" y1="12" x2="22" y2="12" stroke="#000000" stroke-width="1.5" stroke-linecap="round"/>
</svg>
`.trim())}`;

interface UseTitlePageCanvasOptions {
	showGrid: Ref<boolean>;
	activeTool: Ref<ToolType>;
	activeSnapGuides: Ref<ActiveSnapGuide[]>;
	elementDistances: Ref<ElementDistances | null>;
	selectedElement: Ref<FabricObject | null>;
	mousePos: Ref<MousePosition | null>;
	onObjectMoving?: (obj: FabricObject, excludeId?: string) => void;
	onObjectModified?: () => void;
	onObjectMoved?: () => void;
	onSelectionCreated?: (e: any) => void;
	onSelectionUpdated?: (e: any) => void;
	onSelectionCleared?: () => void;
	onObjectAdded?: () => void;
	onObjectRemoved?: () => void;
	onMouseDown?: (e: any) => void;
	onHistorySave?: () => void;
	onSave?: () => void;
}

/**
 * Composable for managing title page canvas
 */
export function useTitlePageCanvas(
	canvasRef: Ref<HTMLCanvasElement | undefined>,
	options: UseTitlePageCanvasOptions
) {
	const canvas = ref<Canvas | null>(null);
	const isInitialized = ref(false);

	/**
	 * Initialize canvas
	 */
	const initCanvas = () => {
		if (!canvasRef.value || isInitialized.value) return;
		isInitialized.value = true;

		// Set canvas element dimensions
		if (canvasRef.value) {
			canvasRef.value.width = A4_WIDTH_PX;
			canvasRef.value.height = A4_HEIGHT_PX;
		}

		canvas.value = new Canvas(canvasRef.value, {
			width: A4_WIDTH_PX,
			height: A4_HEIGHT_PX,
			backgroundColor: 'white',
		});

		// Helper function to apply cursor to upper canvas
		const applyCursorToUpperCanvas = (cursor: string) => {
			// Try to get upper canvas from Fabric.js instance
			if (canvas.value && (canvas.value as any).upperCanvasEl) {
				const upperCanvas = (canvas.value as any).upperCanvasEl as HTMLCanvasElement;
				if (upperCanvas) {
					upperCanvas.style.cursor = cursor;
					return;
				}
			}
			
			// Fallback: find upper canvas by class name
			if (canvasRef.value) {
				const container = canvasRef.value.parentElement;
				if (container) {
					const upperCanvas = container.querySelector('.upper-canvas') as HTMLCanvasElement;
					if (upperCanvas) {
						upperCanvas.style.cursor = cursor;
					}
				}
			}
		};

		// Set initial cursor to thin plus sign
		canvas.value.defaultCursor = THIN_PLUS_CURSOR;
		canvas.value.hoverCursor = THIN_PLUS_CURSOR;

		// Apply cursor directly to upper canvas element (Fabric.js creates this for event handling)
		// Use multiple approaches to ensure cursor is applied
		const applyCursor = () => {
			applyCursorToUpperCanvas(THIN_PLUS_CURSOR);
		};
		
		// Try immediately
		applyCursor();
		
		// Try after next tick
		nextTick(() => {
			applyCursor();
		});
		
		// Try after canvas is fully rendered
		canvas.value.on('after:render', () => {
			applyCursor();
		}, { once: true });

		// Render grid and guides after render (on top of everything)
		let isRenderingOverlay = false;
		canvas.value.on('after:render', () => {
			if (isRenderingOverlay) return; // Prevent recursion
			isRenderingOverlay = true;
			
			const ctx = canvas.value!.getContext();
			
			// Render grid
			if (options.showGrid.value) {
				ctx.save();
				ctx.strokeStyle = '#e0e0e0';
				ctx.lineWidth = 0.5;
				const gridSizePx = GRID_SIZE_MM * MM_TO_PX;
				
				// Vertical lines
				for (let x = 0; x <= A4_WIDTH_PX; x += gridSizePx) {
					ctx.beginPath();
					ctx.moveTo(x, 0);
					ctx.lineTo(x, A4_HEIGHT_PX);
					ctx.stroke();
				}
				
				// Horizontal lines
				for (let y = 0; y <= A4_HEIGHT_PX; y += gridSizePx) {
					ctx.beginPath();
					ctx.moveTo(0, y);
					ctx.lineTo(A4_WIDTH_PX, y);
					ctx.stroke();
				}
				ctx.restore();
			}
			
			// Render active snap guides
			if (options.activeSnapGuides.value.length > 0) {
				ctx.save();
				ctx.strokeStyle = '#0066ff';
				ctx.lineWidth = 1;
				ctx.setLineDash([4, 4]);
				ctx.globalAlpha = 0.7;
				
				for (const guide of options.activeSnapGuides.value) {
					if (guide.type === 'vertical') {
						ctx.beginPath();
						ctx.moveTo(guide.position, 0);
						ctx.lineTo(guide.position, A4_HEIGHT_PX);
						ctx.stroke();
					} else {
						ctx.beginPath();
						ctx.moveTo(0, guide.position);
						ctx.lineTo(A4_WIDTH_PX, guide.position);
						ctx.stroke();
					}
				}
				
				ctx.restore();
			}
			
			// Render distance lines from element to page edges
			if (options.elementDistances.value && options.selectedElement.value) {
				const bounds = getElementBounds(options.selectedElement.value);
				const distances = options.elementDistances.value;
				
				ctx.save();
				ctx.strokeStyle = '#666666';
				ctx.lineWidth = 1;
				ctx.setLineDash([2, 2]);
				ctx.globalAlpha = 0.6;
				ctx.font = '10px Arial';
				ctx.fillStyle = '#333333';
				ctx.textAlign = 'center';
				ctx.textBaseline = 'middle';
				
				// Top line
				ctx.beginPath();
				ctx.moveTo(bounds.left, bounds.top);
				ctx.lineTo(bounds.left, 0);
				ctx.stroke();
				ctx.fillText(
					`${distances.top.toFixed(1)}mm`,
					bounds.left,
					bounds.top / 2
				);
				
				// Bottom line
				ctx.beginPath();
				ctx.moveTo(bounds.left, bounds.bottom);
				ctx.lineTo(bounds.left, A4_HEIGHT_PX);
				ctx.stroke();
				ctx.fillText(
					`${distances.bottom.toFixed(1)}mm`,
					bounds.left,
					bounds.bottom + (A4_HEIGHT_PX - bounds.bottom) / 2
				);
				
				// Left line
				ctx.beginPath();
				ctx.moveTo(bounds.left, bounds.top);
				ctx.lineTo(0, bounds.top);
				ctx.stroke();
				ctx.textAlign = 'right';
				ctx.fillText(
					`${distances.left.toFixed(1)}mm`,
					bounds.left / 2,
					bounds.top
				);
				
				// Right line
				ctx.beginPath();
				ctx.moveTo(bounds.right, bounds.top);
				ctx.lineTo(A4_WIDTH_PX, bounds.top);
				ctx.stroke();
				ctx.textAlign = 'left';
				ctx.fillText(
					`${distances.right.toFixed(1)}mm`,
					bounds.right + (A4_WIDTH_PX - bounds.right) / 2,
					bounds.top
				);
				
				ctx.restore();
			}
			
			isRenderingOverlay = false;
		});

		// Handle mouse movement for ruler and cursor updates
		canvas.value.on('mouse:move', (e) => {
			if (!canvas.value) return;
			const pointer = canvas.value.getPointer(e.e);
			options.mousePos.value = {
				x: pointer.x / MM_TO_PX,
				y: pointer.y / MM_TO_PX,
			};
			
			// Update cursor based on whether we're over an object
			if (options.activeTool.value === 'select') {
				if (e.target) {
					applyCursorToUpperCanvas('move');
				} else {
					applyCursorToUpperCanvas(THIN_PLUS_CURSOR);
				}
			}
		});

		canvas.value.on('mouse:out', () => {
			options.mousePos.value = null;
			// Reset cursor when mouse leaves canvas
			if (options.activeTool.value === 'select') {
				applyCursorToUpperCanvas(THIN_PLUS_CURSOR);
			} else if (options.activeTool.value === 'line') {
				applyCursorToUpperCanvas(THIN_PLUS_CURSOR);
			}
		});

		// Handle object moving with snap
		let isRendering = false;
		// Store initial line coordinates when moving starts
		const lineInitialCoords = new Map<string, { x1: number; y1: number; x2: number; y2: number }>();
		
		canvas.value.on('object:moving', (e) => {
			const obj = e.target;
			if (!obj) return;
			
			// Handle line movement specially
			if (obj.type === 'line') {
				const lineObj = obj as Line;
				const lineId = (obj as any).id;
				
				// Store initial coordinates on first move
				if (!lineInitialCoords.has(lineId)) {
					lineInitialCoords.set(lineId, {
						x1: lineObj.x1!,
						y1: lineObj.y1!,
						x2: lineObj.x2!,
						y2: lineObj.y2!,
					});
				}
				
				const initial = lineInitialCoords.get(lineId)!;
				// Calculate delta from current left/top (which Fabric.js updates during drag)
				// to initial bounding box position
				const initialLeft = Math.min(initial.x1, initial.x2);
				const initialTop = Math.min(initial.y1, initial.y2);
				const deltaX = obj.left! - initialLeft;
				const deltaY = obj.top! - initialTop;
				
				// Update line coordinates based on delta
				lineObj.set({
					x1: initial.x1 + deltaX,
					y1: initial.y1 + deltaY,
					x2: initial.x2 + deltaX,
					y2: initial.y2 + deltaY,
				});
				// Update coords to recalculate bounding box
				lineObj.setCoords();
			}
			
			if (options.onObjectMoving) {
				options.onObjectMoving(obj, (obj as any).id);
			}
			
			// Apply bounds constraint
			constrainToBounds(obj);
			
			// Render only if not already rendering
			if (!isRendering) {
				isRendering = true;
				requestAnimationFrame(() => {
					if (canvas.value) {
						canvas.value.renderAll();
					}
					isRendering = false;
				});
			}
		});
		
		// Clear initial coordinates when move ends
		canvas.value.on('object:moved', (e) => {
			const obj = e.target;
			if (obj && obj.type === 'line') {
				const lineId = (obj as any).id;
				lineInitialCoords.delete(lineId);
			}
			
			if (options.onObjectMoved) {
				options.onObjectMoved();
			}
		});

		canvas.value.on('object:modified', () => {
			if (options.onObjectModified) {
				options.onObjectModified();
			}
		});
		
		canvas.value.on('selection:created', (e) => {
			if (options.onSelectionCreated) {
				options.onSelectionCreated(e);
			}
		});

		canvas.value.on('selection:updated', (e) => {
			if (options.onSelectionUpdated) {
				options.onSelectionUpdated(e);
			}
		});

		canvas.value.on('selection:cleared', () => {
			if (options.onSelectionCleared) {
				options.onSelectionCleared();
			}
		});


		// Set cursor based on active tool
		watch(options.activeTool, (tool) => {
			if (!canvas.value) return;
			
			if (tool === 'select') {
				// Use thin plus cursor for better precision when selecting
				canvas.value.defaultCursor = THIN_PLUS_CURSOR;
				canvas.value.hoverCursor = 'move';
				applyCursorToUpperCanvas(THIN_PLUS_CURSOR);
			} else if (tool === 'text' || tool === 'variable') {
				canvas.value.defaultCursor = 'text';
				canvas.value.hoverCursor = 'text';
				applyCursorToUpperCanvas('text');
			} else if (tool === 'line') {
				// Use thin plus cursor for line tool for better precision
				canvas.value.defaultCursor = THIN_PLUS_CURSOR;
				canvas.value.hoverCursor = THIN_PLUS_CURSOR;
				applyCursorToUpperCanvas(THIN_PLUS_CURSOR);
			}
		}, { immediate: true });

		canvas.value.on('object:added', () => {
			if (options.onObjectAdded) {
				options.onObjectAdded();
			}
		});

		canvas.value.on('object:removed', () => {
			if (options.onObjectRemoved) {
				options.onObjectRemoved();
			}
		});

		// Handle element creation on click
		canvas.value.on('mouse:down', (e) => {
			if (options.onMouseDown) {
				options.onMouseDown(e);
			}
		});
	};

	/**
	 * Create text element
	 */
	const createTextElement = (x: number, y: number, text: string = 'Новый текст') => {
		if (!canvas.value) return null;
		// Convert fontSize from points to pixels (16pt → px)
		const fontSizePt = 16;
		const fontSizePx = fontSizePt * PT_TO_PX;
		const textObj = new Text(text, {
			left: x,
			top: y,
			fontSize: fontSizePx,
			fontFamily: 'Arial',
		});
		(textObj as any).id = crypto.randomUUID();
		canvas.value.add(textObj);
		canvas.value.setActiveObject(textObj);
		canvas.value.renderAll();
		return textObj;
	};

	/**
	 * Create variable element
	 */
	const createVariableElement = (x: number, y: number, text: string = '{Переменная}') => {
		if (!canvas.value) return null;
		// Convert fontSize from points to pixels (16pt → px)
		const fontSizePt = 16;
		const fontSizePx = fontSizePt * PT_TO_PX;
		const textObj = new Text(text, {
			left: x,
			top: y,
			fontSize: fontSizePx,
			fontFamily: 'Arial',
			fontStyle: 'italic',
			fill: '#0066cc',
		});
		(textObj as any).id = crypto.randomUUID();
		(textObj as any).isVariable = true;
		canvas.value.add(textObj);
		canvas.value.setActiveObject(textObj);
		canvas.value.renderAll();
		return textObj;
	};

	/**
	 * Create line element
	 */
	const createLineElement = (x: number, y: number) => {
		if (!canvas.value) return null;
		const line = new Line([x, y, x + 100 * MM_TO_PX, y], {
			stroke: '#000000',
			strokeWidth: 1 * MM_TO_PX,
			perPixelTargetFind: true,
			padding: 20,
		});
		(line as any).id = crypto.randomUUID();
		canvas.value.add(line);
		canvas.value.setActiveObject(line);
		canvas.value.renderAll();
		return line;
	};

	return {
		canvas,
		isInitialized,
		initCanvas,
		createTextElement,
		createVariableElement,
		createLineElement,
	};
}
