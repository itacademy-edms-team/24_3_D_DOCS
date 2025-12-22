import type { Profile } from '@/entities/profile/types';
import type { EntityStyle } from '../renderUtils';
import { applyStyles, generateElementId } from '../renderUtils';

export function renderParagraphs(
	doc: Document,
	usedIds: Set<string>,
	profile: Profile | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('p').forEach((el) => {
		if (el.closest('figure')) return;

		const text = el.textContent || '';
		// Skip TABLE-CAPTION and FORMULA-CAPTION markers (they are processed by tableRenderer and formulaRenderer)
		// But remove any that weren't processed (orphaned captions)
		if (text.match(/^\[(TABLE|FORMULA)-CAPTION:\s*.+\]$/)) {
			// Check if this is still in the document (wasn't processed)
			el.remove();
			return;
		}

		const content = text;
		const elId = generateElementId('p', content, usedIds);
		applyStyles(el, 'paragraph', elId, profile, overrides, selectable);
	});
}
