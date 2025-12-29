import { ref, type Ref, onMounted, onUnmounted } from 'vue';
import { useKeyModifier } from '@vueuse/core';
import { useEventListener } from '@vueuse/core';
import { Canvas, Line, type FabricObject } from 'fabric';
import { MM_TO_PX, type ToolType } from '@/entities/title-page/constants';
import { constrainToBounds } from '@/entities/title-page/utils/elementUtils';

interface UseTitlePageKeyboardOptions {
	canvas: Ref<Canvas | null>;
	activeTool: Ref<ToolType>;
	selectedElement: Ref<FabricObject | null>;
	showGrid: Ref<boolean>;
	toggleGrid: () => void;
	onUndo: () => void;
	onRedo: () => void;
	onCopy: () => void;
	onPaste: () => void;
	onCut: () => void;
	onDuplicate: () => void;
	onSelectAll: () => void;
	onDelete: () => void;
	onSave: () => void;
}

/**
 * Composable for managing keyboard shortcuts
 */
export function useTitlePageKeyboard(options: UseTitlePageKeyboardOptions) {
	const isAltPressed = useKeyModifier('Alt', { events: ['keydown', 'keyup'] });

	/**
	 * Move selected element
	 * @param deltaX - delta in mm (will be converted to px)
	 * @param deltaY - delta in mm (will be converted to px)
	 * @param inPixels - if true, deltaX and deltaY are already in pixels
	 */
	const moveSelectedElement = (deltaX: number, deltaY: number, inPixels: boolean = false) => {
		if (!options.canvas.value) return;
		
		// Convert to pixels if needed
		const deltaXPx = inPixels ? deltaX : deltaX * MM_TO_PX;
		const deltaYPx = inPixels ? deltaY : deltaY * MM_TO_PX;
		
		// Get all selected objects
		const activeObjects = options.canvas.value.getActiveObjects();
		if (activeObjects.length === 0 && options.selectedElement.value) {
			// If no active objects but selectedElement exists, use it
			const obj = options.selectedElement.value;
			if (obj.type === 'line') {
				const lineObj = obj as Line;
				lineObj.set({
					x1: lineObj.x1! + deltaXPx,
					y1: lineObj.y1! + deltaYPx,
					x2: lineObj.x2! + deltaXPx,
					y2: lineObj.y2! + deltaYPx,
				});
			} else {
				obj.set({
					left: obj.left! + deltaXPx,
					top: obj.top! + deltaYPx,
				});
			}
			constrainToBounds(obj);
		} else {
			// Move all selected objects
			activeObjects.forEach((obj) => {
				if (obj.type === 'line') {
					const lineObj = obj as Line;
					lineObj.set({
						x1: lineObj.x1! + deltaXPx,
						y1: lineObj.y1! + deltaYPx,
						x2: lineObj.x2! + deltaXPx,
						y2: lineObj.y2! + deltaYPx,
					});
				} else {
					obj.set({
						left: obj.left! + deltaXPx,
						top: obj.top! + deltaYPx,
					});
				}
				constrainToBounds(obj);
			});
		}
		options.canvas.value.renderAll();
	};

	/**
	 * Handle keyboard shortcuts
	 */
	const handleHotkeys = (e: KeyboardEvent) => {
		// Don't handle shortcuts if user is typing in input/textarea/select
		const target = e.target as HTMLElement;
		const tagName = target.tagName?.toUpperCase();
		const isEditable = target.isContentEditable;
		const isInput = tagName === 'INPUT' || tagName === 'TEXTAREA' || tagName === 'SELECT';
		const isInsideEditable = target.closest('input, textarea, select, [contenteditable="true"]');
		
		if (isInput || isEditable || isInsideEditable) {
			// Allow only some shortcuts in input fields (Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+A, Ctrl+Z)
			const isCtrl = e.ctrlKey || e.metaKey;
			if (isCtrl && (e.code === 'KeyC' || e.code === 'KeyV' || e.code === 'KeyX' || e.code === 'KeyA' || e.code === 'KeyZ')) {
				return; // Allow default browser behavior
			}
			// Block other shortcuts
			return;
		}

		const isCtrl = e.ctrlKey || e.metaKey;
		const isShift = e.shiftKey;
		const isAlt = e.altKey;

		// Tool shortcuts
		if (!isCtrl && !isAlt) {
			// V or 1 - Select
			if (e.code === 'KeyV' || e.code === 'Digit1') {
				e.preventDefault();
				e.stopPropagation();
				options.activeTool.value = 'select';
				return;
			}
			
			// T - Text
			if (e.code === 'KeyT') {
				e.preventDefault();
				e.stopPropagation();
				options.activeTool.value = options.activeTool.value === 'text' ? 'select' : 'text';
				return;
			}
			
			// L - Line
			if (e.code === 'KeyL') {
				e.preventDefault();
				e.stopPropagation();
				options.activeTool.value = options.activeTool.value === 'line' ? 'select' : 'line';
				return;
			}
			
			// G - Toggle Grid
			if (e.code === 'KeyG') {
				e.preventDefault();
				e.stopPropagation();
				options.toggleGrid();
				return;
			}
			
			// Escape - Deselect
			if (e.code === 'Escape') {
				e.preventDefault();
				e.stopPropagation();
				if (options.canvas.value) {
					options.canvas.value.discardActiveObject();
					options.selectedElement.value = null;
					options.canvas.value.renderAll();
				}
				options.activeTool.value = 'select';
				return;
			}
			
			// Delete/Backspace - Delete selected elements
			if (e.code === 'Delete' || e.code === 'Backspace') {
				const activeObjects = options.canvas.value?.getActiveObjects() || [];
				const hasSelection = activeObjects.length > 0 || options.selectedElement.value;
				if (hasSelection) {
					e.preventDefault();
					e.stopPropagation();
					options.onDelete();
					return;
				}
			}
			
			// Arrow keys - Move elements
			const activeObjects = options.canvas.value?.getActiveObjects() || [];
			const hasSelection = activeObjects.length > 0 || options.selectedElement.value;
			if (hasSelection && (e.code === 'ArrowUp' || e.code === 'ArrowDown' || e.code === 'ArrowLeft' || e.code === 'ArrowRight')) {
				e.preventDefault();
				e.stopPropagation();
				const moveAmount = isShift ? 10 : 1; // Shift = 10mm, normal = 1mm
				let deltaX = 0;
				let deltaY = 0;
				
				if (e.code === 'ArrowLeft') {
					deltaX = -moveAmount;
				} else if (e.code === 'ArrowRight') {
					deltaX = moveAmount;
				} else if (e.code === 'ArrowUp') {
					deltaY = -moveAmount;
				} else if (e.code === 'ArrowDown') {
					deltaY = moveAmount;
				}
				
				if (deltaX !== 0 || deltaY !== 0) {
					moveSelectedElement(deltaX, deltaY);
				}
				return;
			}
		}

		// Ctrl/Cmd shortcuts
		if (isCtrl && !isAlt) {
			// Ctrl+Arrow keys - Move elements pixel by pixel
			const activeObjects = options.canvas.value?.getActiveObjects() || [];
			const hasSelection = activeObjects.length > 0 || options.selectedElement.value;
			if (hasSelection && (e.code === 'ArrowUp' || e.code === 'ArrowDown' || e.code === 'ArrowLeft' || e.code === 'ArrowRight')) {
				e.preventDefault();
				e.stopPropagation();
				const moveAmount = isShift ? 10 : 1; // Shift = 10px, normal = 1px
				let deltaX = 0;
				let deltaY = 0;
				
				if (e.code === 'ArrowLeft') {
					deltaX = -moveAmount;
				} else if (e.code === 'ArrowRight') {
					deltaX = moveAmount;
				} else if (e.code === 'ArrowUp') {
					deltaY = -moveAmount;
				} else if (e.code === 'ArrowDown') {
					deltaY = moveAmount;
				}
				
				if (deltaX !== 0 || deltaY !== 0) {
					moveSelectedElement(deltaX, deltaY, true); // true = pixels, not mm
				}
				return;
			}
			
			// Ctrl+Z - Undo
			if (e.code === 'KeyZ' && !isShift) {
				e.preventDefault();
				e.stopPropagation();
				options.onUndo();
				return;
			}
			
			// Ctrl+Shift+Z or Ctrl+Y - Redo
			if ((e.code === 'KeyZ' && isShift) || e.code === 'KeyY') {
				e.preventDefault();
				e.stopPropagation();
				options.onRedo();
				return;
			}
			
			// Ctrl+C - Copy
			if (e.code === 'KeyC') {
				e.preventDefault();
				e.stopPropagation();
				options.onCopy();
				return;
			}
			
			// Ctrl+V - Paste
			if (e.code === 'KeyV') {
				e.preventDefault();
				e.stopPropagation();
				options.onPaste();
				return;
			}
			
			// Ctrl+X - Cut
			if (e.code === 'KeyX') {
				e.preventDefault();
				e.stopPropagation();
				options.onCut();
				return;
			}
			
			// Ctrl+D - Duplicate
			if (e.code === 'KeyD') {
				e.preventDefault();
				e.stopPropagation();
				options.onDuplicate();
				return;
			}
			
			// Ctrl+A - Select All
			if (e.code === 'KeyA') {
				e.preventDefault();
				e.stopPropagation();
				options.onSelectAll();
				return;
			}
			
			// Ctrl+S - Save
			if (e.code === 'KeyS') {
				e.preventDefault();
				e.stopPropagation();
				options.onSave();
				return;
			}
		}
	};

	// Use capture phase to catch events early
	useEventListener(window, 'keydown', handleHotkeys, { capture: true });

	return {
		isAltPressed,
	};
}
