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
		if (text.match(/^\[(TABLE|FORMULA)-CAPTION:/)) return;

		const content = text;
		const elId = generateElementId('p', content, usedIds);
		applyStyles(el, 'paragraph', elId, profile, overrides, selectable);
	});
}
