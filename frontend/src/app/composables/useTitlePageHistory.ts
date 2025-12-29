import { ref, type Ref, nextTick } from 'vue';
import { useDebounceFn } from '@vueuse/core';
import { Canvas, Line, type FabricObject } from 'fabric';
import { MAX_HISTORY_SIZE, A4_WIDTH_PX, A4_HEIGHT_PX, MM_TO_PX } from '@/entities/title-page/constants';
import { getElementBounds } from '@/entities/title-page/utils/elementUtils';
import type { ElementDistances } from '@/entities/title-page/constants';

interface UseTitlePageHistoryOptions {
	canvasRef: Ref<HTMLCanvasElement | undefined>;
	selectedElement: Ref<FabricObject | null>;
	elementDistances: Ref<ElementDistances | null>;
}

/**
 * Composable for managing title page history (undo/redo)
 */
export function useTitlePageHistory(
	canvas: Ref<Canvas | null>,
	options: UseTitlePageHistoryOptions
) {
	const historyState = ref<Array<string>>([]);
	const historyIndex = ref<number>(-1);
	const isHistoryUpdate = ref(false);
	const canUndo = ref(false);
	const canRedo = ref(false);

	/**
	 * Update undo/redo state
	 */
	const updateUndoRedoState = () => {
		canUndo.value = historyIndex.value > 0;
		canRedo.value = historyIndex.value < historyState.value.length - 1;
	};

	/**
	 * Save history state with debounce
	 */
	const saveHistoryStateDebounced = useDebounceFn(() => {
		if (!canvas.value || isHistoryUpdate.value) {
			return;
		}
		
		try {
			// Save canvas state with important properties
			const canvasState = canvas.value.toJSON(['selectable', 'evented']);
			
			// Add custom isVariable and customWidth properties for each object
			// Also explicitly save line coordinates (x1, y1, x2, y2)
			if (canvasState.objects && Array.isArray(canvasState.objects)) {
				const objects = canvas.value.getObjects();
				canvasState.objects.forEach((obj: any, index: number) => {
					const fabricObj = objects[index];
					if (!fabricObj) return;
					
					// Save isVariable property
					if ((fabricObj as any).isVariable !== undefined) {
						obj.isVariable = (fabricObj as any).isVariable;
					}
					
					// Save customWidth for text objects
					if ((fabricObj.type === 'text' || fabricObj.type === 'i-text' || fabricObj.type === 'textbox') 
						&& (fabricObj as any).customWidth !== undefined) {
						obj.customWidth = (fabricObj as any).customWidth;
					}
					
					// Explicitly save line coordinates (x1, y1, x2, y2) for line objects
					if (fabricObj.type === 'line') {
						const lineObj = fabricObj as Line;
						// Validate line object before saving coordinates
						if (lineObj && 
							typeof lineObj.x1 === 'number' && !isNaN(lineObj.x1) &&
							typeof lineObj.y1 === 'number' && !isNaN(lineObj.y1) &&
							typeof lineObj.x2 === 'number' && !isNaN(lineObj.x2) &&
							typeof lineObj.y2 === 'number' && !isNaN(lineObj.y2)) {
							// Ensure x1, y1, x2, y2 are saved in the serialized state
							obj.x1 = lineObj.x1;
							obj.y1 = lineObj.y1;
							obj.x2 = lineObj.x2;
							obj.y2 = lineObj.y2;
						} else {
							console.warn('Invalid line object at index', index, 'skipping coordinate save', {
								x1: lineObj?.x1,
								y1: lineObj?.y1,
								x2: lineObj?.x2,
								y2: lineObj?.y2
							});
						}
					}
				});
			}
			
			// Add important canvas properties that might be lost on load
			const stateWithProps = {
				...canvasState,
				width: canvas.value.width,
				height: canvas.value.height,
				backgroundColor: canvas.value.backgroundColor,
			};
			
			const json = JSON.stringify(stateWithProps);
			
			// Remove all states after current index (if making new action after undo)
			if (historyIndex.value < historyState.value.length - 1) {
				historyState.value = historyState.value.slice(0, historyIndex.value + 1);
			}
			
			// Add new state
			historyState.value.push(json);
			
			// Limit history size
			if (historyState.value.length > MAX_HISTORY_SIZE) {
				historyState.value.shift();
			} else {
				historyIndex.value = historyState.value.length - 1;
			}
			
			updateUndoRedoState();
		} catch (error) {
			console.error('Error saving history state:', error);
		}
	}, 300);

	/**
	 * Save history state (wrapper for debounced function)
	 */
	const saveHistoryState = () => {
		saveHistoryStateDebounced();
	};

	/**
	 * Load history state
	 */
	const loadHistoryState = async (index: number) => {
		if (!canvas.value || index < 0 || index >= historyState.value.length) return;
		
		// Protection against multiple calls
		if (isHistoryUpdate.value) {
			console.warn('loadHistoryState already in progress, skipping. Index:', index);
			return;
		}
		
		// Check if index actually changed
		if (historyIndex.value === index) {
			console.warn('loadHistoryState: index unchanged, skipping. Index:', index);
			return;
		}
		
		isHistoryUpdate.value = true;
		historyIndex.value = index; // Update index immediately to prevent repeated calls
		
		try {
			const json = historyState.value[index];
			const state = JSON.parse(json);
			
			// Restore important canvas properties before loading
			if (state.width !== undefined) {
				canvas.value.setWidth(state.width);
				if (options.canvasRef.value) {
					options.canvasRef.value.width = state.width;
				}
			}
			if (state.height !== undefined) {
				canvas.value.setHeight(state.height);
				if (options.canvasRef.value) {
					options.canvasRef.value.height = state.height;
				}
			}
			if (state.backgroundColor !== undefined) {
				canvas.value.backgroundColor = state.backgroundColor;
			}
			
			// Save dimensions before loadFromJSON, as it might overwrite them
			const savedWidth = state.width !== undefined ? state.width : canvas.value.width;
			const savedHeight = state.height !== undefined ? state.height : canvas.value.height;
			const savedBackgroundColor = state.backgroundColor !== undefined ? state.backgroundColor : canvas.value.backgroundColor;
			
			// Remove width and height from JSON before loading, so loadFromJSON doesn't overwrite them
			const jsonObj = JSON.parse(json);
			delete jsonObj.width;
			delete jsonObj.height;
			const jsonWithoutDimensions = JSON.stringify(jsonObj);
			
			// Save reference to current historyIndex for check after loading
			const expectedHistoryIndex = historyIndex.value;
			
			// Use Promise API (correct approach for Fabric.js v6)
			await canvas.value.loadFromJSON(jsonWithoutDimensions);
			
			// Check that index didn't change (protection against race condition)
			if (historyIndex.value !== expectedHistoryIndex) {
				console.warn('loadHistoryState: historyIndex changed during loadFromJSON, aborting. Expected:', expectedHistoryIndex, 'Actual:', historyIndex.value);
				isHistoryUpdate.value = false;
				return;
			}
			
			// Check that canvas still exists after await
			if (!canvas.value) {
				console.warn('loadHistoryState: canvas.value is null after loadFromJSON');
				isHistoryUpdate.value = false;
				return;
			}
			
			// IMPORTANT: Restore dimensions AFTER loadFromJSON, as it might overwrite them
			// Use setDimensions for atomic dimension setting
			canvas.value.setDimensions({ width: savedWidth, height: savedHeight });
			if (options.canvasRef.value) {
				options.canvasRef.value.width = savedWidth;
				options.canvasRef.value.height = savedHeight;
			}
			canvas.value.backgroundColor = savedBackgroundColor;
			
			// Restore isVariable and customWidth for objects
			// Also explicitly restore line coordinates (x1, y1, x2, y2)
			const objects = canvas.value.getObjects();
			const originalObjects = jsonObj.objects || [];
			objects.forEach((obj: any, i: number) => {
				const originalObj = originalObjects[i];
				if (!originalObj) return;
				
				// Restore isVariable property
				if (originalObj.isVariable !== undefined) {
					obj.isVariable = originalObj.isVariable;
				}
				
				// Restore customWidth for text objects
				if ((obj.type === 'text' || obj.type === 'i-text' || obj.type === 'textbox') 
					&& originalObj.customWidth !== undefined) {
					obj.customWidth = originalObj.customWidth;
					// Apply the width to the object
					obj.set({ width: originalObj.customWidth, scaleX: 1 });
					obj.setCoords();
				}
				
				// Explicitly restore line coordinates (x1, y1, x2, y2) for line objects
				if (obj.type === 'line' && originalObj.type === 'line') {
					const lineObj = obj as Line;
					// Verify that we have a valid line object
					if (!lineObj || typeof lineObj.set !== 'function') {
						console.warn('Invalid line object at index', i, 'skipping coordinate restoration');
						return;
					}
					
					// Restore coordinates from the original JSON state with safety checks
					const x1 = originalObj.x1;
					const y1 = originalObj.y1;
					const x2 = originalObj.x2;
					const y2 = originalObj.y2;
					
					// Validate that all coordinates are valid numbers
					if (x1 !== undefined && x1 !== null && typeof x1 === 'number' &&
						y1 !== undefined && y1 !== null && typeof y1 === 'number' &&
						x2 !== undefined && x2 !== null && typeof x2 === 'number' &&
						y2 !== undefined && y2 !== null && typeof y2 === 'number' &&
						!isNaN(x1) && !isNaN(y1) && !isNaN(x2) && !isNaN(y2)) {
						try {
							lineObj.set({
								x1: x1,
								y1: y1,
								x2: x2,
								y2: y2,
							});
							// Update coords to recalculate bounding box
							lineObj.setCoords();
						} catch (error) {
							console.error('Error restoring line coordinates at index', i, ':', error);
						}
					} else {
						console.warn('Invalid line coordinates in history state at index', i, {
							x1, y1, x2, y2
						});
					}
				}
			});
			
			// Use nextTick for guaranteed DOM update
			await nextTick();
			
			if (!canvas.value) {
				isHistoryUpdate.value = false;
				return;
			}
			
			// Check that canvas element is still in DOM
			const canvasEl = options.canvasRef.value;
			const isInDOM = canvasEl && document.body.contains(canvasEl);
			
			// If canvas is not in DOM or has zero dimensions, try to restore
			if (!isInDOM || canvas.value.width === 0 || canvas.value.height === 0) {
				console.error('Canvas disappeared or has zero dimensions!', {
					isInDOM,
					canvasWidth: canvas.value.width,
					canvasHeight: canvas.value.height,
					canvasRefWidth: options.canvasRef.value?.width,
					canvasRefHeight: options.canvasRef.value?.height,
					savedWidth,
					savedHeight
				});
				
				// Try to restore dimensions
				if (savedWidth && savedHeight) {
					canvas.value.setDimensions({ width: savedWidth, height: savedHeight });
					if (options.canvasRef.value) {
						options.canvasRef.value.width = savedWidth;
						options.canvasRef.value.height = savedHeight;
					}
				}
			}
			
			canvas.value.renderAll();
			
			// Check state after renderAll
			const afterIsInDOM = canvasEl && document.body.contains(canvasEl);
			
			// If canvas still has problems, log and try to restore again
			if (!afterIsInDOM || canvas.value.width === 0 || canvas.value.height === 0) {
				console.error('Canvas still has problems after renderAll!', {
					afterIsInDOM,
					canvasWidth: canvas.value.width,
					canvasHeight: canvas.value.height,
					canvasRefWidth: options.canvasRef.value?.width,
					canvasRefHeight: options.canvasRef.value?.height,
					savedWidth,
					savedHeight
				});
				
				// Another attempt to restore dimensions
				if (savedWidth && savedHeight) {
					canvas.value.setDimensions({ width: savedWidth, height: savedHeight });
					if (options.canvasRef.value) {
						options.canvasRef.value.width = savedWidth;
						options.canvasRef.value.height = savedHeight;
					}
					canvas.value.renderAll();
				}
			}
			
			// Update selectedElement after loading
			const activeObject = canvas.value.getActiveObject();
			options.selectedElement.value = activeObject || null;
			
			// Update distances for selected element
			if (options.selectedElement.value) {
				const bounds = getElementBounds(options.selectedElement.value);
				options.elementDistances.value = {
					top: bounds.top / MM_TO_PX,
					bottom: (A4_HEIGHT_PX - bounds.bottom) / MM_TO_PX,
					left: bounds.left / MM_TO_PX,
					right: (A4_WIDTH_PX - bounds.right) / MM_TO_PX,
				};
			} else {
				options.elementDistances.value = null;
			}
			
			isHistoryUpdate.value = false;
			updateUndoRedoState();
		} catch (error) {
			console.error('Error loading history state:', error);
			isHistoryUpdate.value = false;
		}
	};

	/**
	 * Undo
	 */
	const undo = () => {
		if (historyIndex.value > 0 && !isHistoryUpdate.value) {
			const newIndex = historyIndex.value - 1;
			loadHistoryState(newIndex);
		}
	};

	/**
	 * Redo
	 */
	const redo = () => {
		if (historyIndex.value < historyState.value.length - 1 && !isHistoryUpdate.value) {
			const newIndex = historyIndex.value + 1;
			loadHistoryState(newIndex);
		}
	};

	/**
	 * Initialize history
	 */
	const initHistory = () => {
		historyState.value = [];
		historyIndex.value = -1;
		saveHistoryState();
	};

	return {
		canUndo,
		canRedo,
		saveHistoryState,
		loadHistoryState,
		undo,
		redo,
		initHistory,
		isHistoryUpdate,
	};
}
