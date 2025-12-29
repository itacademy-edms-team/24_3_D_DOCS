import { ref, type Ref, nextTick } from 'vue';
import { Canvas, type FabricObject } from 'fabric';
import { A4_WIDTH_PX, A4_HEIGHT_PX, MM_TO_PX } from '@/entities/title-page/constants';
import { serializeElement, deserializeElement, constrainToBounds, type SerializedElement } from '@/entities/title-page/utils/elementUtils';

/**
 * Composable for managing clipboard operations
 */
export function useTitlePageClipboard(
	canvas: Ref<Canvas | null>,
	selectedElement: Ref<FabricObject | null>,
	onHistorySave?: () => void,
	onSave?: () => void
) {
	const clipboard = ref<SerializedElement[]>([]);

	/**
	 * Copy selected elements to clipboard
	 */
	const copySelectedElements = () => {
		if (!canvas.value) return;
		
		// Get all selected objects
		const activeObjects = canvas.value.getActiveObjects();
		const objectsToCopy = activeObjects.length > 0 
			? activeObjects 
			: (selectedElement.value ? [selectedElement.value] : []);
		
		if (objectsToCopy.length === 0) return;
		
		const elementsData: SerializedElement[] = [];
		for (const obj of objectsToCopy) {
			const elementData = serializeElement(obj);
			if (elementData) {
				elementsData.push(elementData);
			}
		}
		
		if (elementsData.length > 0) {
			clipboard.value = elementsData;
		}
	};

	/**
	 * Cut selected elements to clipboard
	 */
	const cutSelectedElements = () => {
		if (!canvas.value) return;
		
		// Get all selected objects
		const activeObjects = canvas.value.getActiveObjects();
		const objectsToCut = activeObjects.length > 0 
			? activeObjects 
			: (selectedElement.value ? [selectedElement.value] : []);
		
		if (objectsToCut.length === 0) return;
		
		// Copy to clipboard
		copySelectedElements();
		
		// Clear selection BEFORE removing objects
		canvas.value.discardActiveObject();
		selectedElement.value = null;
		
		// Remove objects
		objectsToCut.forEach((obj) => {
			canvas.value!.remove(obj);
		});
		
		// Use nextTick for guaranteed DOM update
		nextTick(() => {
			if (canvas.value) {
				canvas.value.requestRenderAll();
				onSave?.();
			}
		});
	};

	/**
	 * Paste elements from clipboard
	 */
	const pasteElements = () => {
		if (!canvas.value || clipboard.value.length === 0) return;
		
		// Find minimum Y coordinate of the element group
		const minTop = Math.min(...clipboard.value.map(item => {
			if (item.type === 'line') {
				return Math.min(item.data.y1, item.data.y2);
			}
			return item.data.top || 0;
		}));
		
		// Offset only downward (no horizontal offset)
		const offsetX = 0;
		const offsetY = 10 * MM_TO_PX; // 10mm down
		
		const newElements: FabricObject[] = [];
		
		// Temporarily disable automatic history saving when adding elements
		// Save history once after adding all elements
		canvas.value.off('object:added');
		
		for (const item of clipboard.value) {
			const element = deserializeElement(item, offsetX, offsetY);
			if (element) {
				canvas.value.add(element);
				// Apply bounds constraints to each element
				constrainToBounds(element);
				newElements.push(element);
			}
		}
		
		// Restore event handler
		canvas.value.on('object:added', () => {
			onHistorySave?.();
			onSave?.();
		});
		
		if (newElements.length > 0) {
			canvas.value.setActiveObject(newElements[0]);
			selectedElement.value = newElements[0];
			canvas.value.renderAll();
			// Save history once after adding all elements
			onHistorySave?.();
			onSave?.();
		}
	};

	/**
	 * Duplicate selected elements
	 */
	const duplicateSelectedElements = () => {
		if (!canvas.value) return;
		
		// Get all selected objects
		const activeObjects = canvas.value.getActiveObjects();
		const objectsToDuplicate = activeObjects.length > 0 
			? activeObjects 
			: (selectedElement.value ? [selectedElement.value] : []);
		
		if (objectsToDuplicate.length === 0) return;
		
		// Save current selected objects to temporary clipboard
		const tempClipboard = clipboard.value;
		copySelectedElements();
		pasteElements();
		// Restore clipboard
		clipboard.value = tempClipboard;
	};

	/**
	 * Select all elements
	 */
	const selectAllElements = () => {
		if (!canvas.value) return;
		
		const objects = canvas.value.getObjects();
		if (objects.length > 0) {
			// Select all objects
			canvas.value.setActiveObjects(objects);
			selectedElement.value = objects[0];
			canvas.value.renderAll();
		}
	};

	return {
		clipboard,
		copySelectedElements,
		cutSelectedElements,
		pasteElements,
		duplicateSelectedElements,
		selectAllElements,
	};
}
