import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { applyStyles, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render paragraphs with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderParagraphs(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('p').forEach((el) => {
		// Skip paragraphs inside figures or other containers
		if (el.closest('figure')) return;

		const text = el.textContent || '';
		// Skip table/formula/image captions
		if (text.match(/^\[(IMAGE|TABLE|FORMULA)-CAPTION:/)) return;

		const content = text;
		const elId = generateElementId('p', content, usedIds);
		applyStyles(
			el,
			'paragraph' as EntityType,
			elId,
			profile,
			overrides,
			selectable
		);
	});
}
