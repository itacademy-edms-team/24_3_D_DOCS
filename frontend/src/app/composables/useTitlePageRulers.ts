import { computed, type Ref } from 'vue';
import { A4_WIDTH_MM, A4_HEIGHT_MM, MM_TO_PX, type RulerMark, type MousePosition } from '@/entities/title-page/constants';

/**
 * Composable for managing title page rulers
 */
export function useTitlePageRulers() {
	/**
	 * Calculate horizontal ruler marks
	 */
	const horizontalRulerMarks = computed<RulerMark[]>(() => {
		const marks: RulerMark[] = [];
		const step = 10; // 10mm step
		for (let i = 0; i <= A4_WIDTH_MM; i += step) {
			marks.push({
				value: i,
				position: i * MM_TO_PX,
				isMajor: i % 50 === 0 || i === 0,
			});
		}
		return marks;
	});

	/**
	 * Calculate vertical ruler marks
	 */
	const verticalRulerMarks = computed<RulerMark[]>(() => {
		const marks: RulerMark[] = [];
		const step = 10; // 10mm step
		for (let i = 0; i <= A4_HEIGHT_MM; i += step) {
			marks.push({
				value: i,
				position: i * MM_TO_PX,
				isMajor: i % 50 === 0 || i === 0,
			});
		}
		return marks;
	});

	return {
		horizontalRulerMarks,
		verticalRulerMarks,
	};
}
