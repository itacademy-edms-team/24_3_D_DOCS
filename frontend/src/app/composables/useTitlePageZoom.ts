import { ref } from 'vue';
import { useToggle } from '@vueuse/core';
import { ZOOM_OPTIONS } from '@/entities/title-page/constants';

/**
 * Composable for managing zoom and grid visibility
 */
export function useTitlePageZoom() {
	const zoom = ref(100);
	const [showGrid, toggleGrid] = useToggle(false);

	return {
		zoom,
		showGrid,
		toggleGrid,
		zoomOptions: ZOOM_OPTIONS,
	};
}
