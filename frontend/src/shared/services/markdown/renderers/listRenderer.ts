import type { Profile } from '@/entities/profile/types';
import type { EntityStyle } from '../renderUtils';
import { getFinalStyle, styleToCSS, generateElementId } from '../renderUtils';

export function renderUnorderedLists(
	doc: Document,
	usedIds: Set<string>,
	profile: Profile | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('ul').forEach((el) => {
		const content = el.textContent || '';
		const elId = generateElementId('ul', content.slice(0, 50), usedIds);
		const style = getFinalStyle('unordered-list', elId, profile, overrides);

		const listStyle: EntityStyle = { ...style, textIndent: undefined };

		el.id = elId;
		el.setAttribute('data-type', 'unordered-list');
		const currentStyle = el.getAttribute('style') || '';
		el.setAttribute('style', `${currentStyle}; ${styleToCSS(listStyle)}; list-style-type: none;`.trim());
		if (selectable) el.classList.add('element-selectable');
		
		// Add custom markers using ::before (we'll style this in CSS)
		el.classList.add('custom-dash-list');

		if (style.textIndent !== undefined && style.textIndent > 0) {
			const textIndentPt = style.textIndent * 28.35;
			const listItems = el.querySelectorAll('li');
			listItems.forEach((li) => {
				const currentStyle = (li as HTMLElement).getAttribute('style') || '';
				(li as HTMLElement).setAttribute('style', `${currentStyle}; margin-left: ${textIndentPt}pt;`.trim());
			});
		}
	});
}

export function renderOrderedLists(
	doc: Document,
	usedIds: Set<string>,
	profile: Profile | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('ol').forEach((el) => {
		const content = el.textContent || '';
		const elId = generateElementId('ol', content.slice(0, 50), usedIds);
		const style = getFinalStyle('ordered-list', elId, profile, overrides);

		const listStyle: EntityStyle = { ...style, textIndent: undefined };

		el.id = elId;
		el.setAttribute('data-type', 'ordered-list');
		el.setAttribute('style', styleToCSS(listStyle));
		if (selectable) el.classList.add('element-selectable');

		if (style.textIndent !== undefined && style.textIndent > 0) {
			const textIndentPt = style.textIndent * 28.35;
			const listItems = el.querySelectorAll('li');
			listItems.forEach((li) => {
				const currentStyle = (li as HTMLElement).getAttribute('style') || '';
				(li as HTMLElement).setAttribute('style', `${currentStyle}; margin-left: ${textIndentPt}pt;`.trim());
			});
		}
	});
}
