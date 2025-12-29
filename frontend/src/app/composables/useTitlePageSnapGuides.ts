import { ref, type Ref } from 'vue';
import { Canvas, type FabricObject } from 'fabric';
import { A4_WIDTH_PX, A4_HEIGHT_PX, MM_TO_PX, type AlignmentGuide, type ActiveSnapGuide, type ElementDistances } from '@/entities/title-page/constants';
import { findAlignmentGuides, applySnap } from '@/entities/title-page/utils/snapUtils';
import { getElementBounds } from '@/entities/title-page/utils/elementUtils';

/**
 * Composable for managing snap guides and element distances
 */
export function useTitlePageSnapGuides(
	canvas: Ref<Canvas | null>,
	isAltPressed: Ref<boolean>,
	selectedElement: Ref<FabricObject | null>
) {
	const alignmentGuides = ref<AlignmentGuide[]>([]);
	const activeSnapGuides = ref<ActiveSnapGuide[]>([]);
	const elementDistances = ref<ElementDistances | null>(null);

	/**
	 * Calculate element distances from page edges
	 */
	const calculateElementDistances = (obj: FabricObject) => {
		const bounds = getElementBounds(obj);
		elementDistances.value = {
			top: bounds.top / MM_TO_PX,
			bottom: (A4_HEIGHT_PX - bounds.bottom) / MM_TO_PX,
			left: bounds.left / MM_TO_PX,
			right: (A4_WIDTH_PX - bounds.right) / MM_TO_PX,
		};
	};

	/**
	 * Find and apply snap guides for a moving object
	 */
	const handleObjectMoving = (obj: FabricObject, excludeId?: string) => {
		if (!canvas.value || isAltPressed.value) {
			alignmentGuides.value = [];
			activeSnapGuides.value = [];
			elementDistances.value = null;
			return;
		}

		const guides = findAlignmentGuides(canvas.value, obj, isAltPressed.value, excludeId);
		alignmentGuides.value = guides;

		// Calculate distances to page edges
		calculateElementDistances(obj);

		if (guides.length > 0) {
			const snapped = applySnap(obj, guides);
			if (obj.type === 'line') {
				// For lines, already updated via set
			} else {
				obj.set({
					left: snapped.x,
					top: snapped.y,
				});
			}
			activeSnapGuides.value = snapped.activeGuides;
		} else {
			activeSnapGuides.value = [];
		}
	};

	/**
	 * Clear guides after object modification
	 */
	const clearGuides = () => {
		alignmentGuides.value = [];
		activeSnapGuides.value = [];
	};

	/**
	 * Update distances for selected element
	 */
	const updateSelectedElementDistances = () => {
		if (selectedElement.value) {
			calculateElementDistances(selectedElement.value);
		} else {
			elementDistances.value = null;
		}
	};

	return {
		alignmentGuides,
		activeSnapGuides,
		elementDistances,
		handleObjectMoving,
		clearGuides,
		updateSelectedElementDistances,
		calculateElementDistances,
	};
}
