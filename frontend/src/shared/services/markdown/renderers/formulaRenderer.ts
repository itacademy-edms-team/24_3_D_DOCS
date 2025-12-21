import type { Profile } from '@/entities/profile/types';
import type { EntityStyle } from '../renderUtils';
import { getFinalStyle, styleToCSS, generateElementId } from '../renderUtils';

export function renderFormulas(
	doc: Document,
	usedIds: Set<string>,
	profile: Profile | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('.formula-block').forEach((formulaBlock) => {
		const content = formulaBlock.textContent || '';
		const elId = generateElementId('formula', content.slice(0, 50), usedIds);
		const style = getFinalStyle('formula', elId, profile, overrides);

		let captionText: string | null = null;
		const nextSibling = formulaBlock.nextElementSibling;
		if (nextSibling && nextSibling.tagName === 'P') {
			const pText = nextSibling.textContent || '';
			const match = pText.match(/^\[FORMULA-CAPTION:\s*(.+)\]$/);
			if (match) {
				captionText = match[1].trim();
				nextSibling.remove();
			}
		}

		const formulaWrapper = doc.createElement('div');
		formulaWrapper.id = elId;
		formulaWrapper.setAttribute('data-type', 'formula');
		formulaWrapper.setAttribute('style', styleToCSS(style));
		if (selectable) formulaWrapper.classList.add('element-selectable');

		const parent = formulaBlock.parentNode;
		if (parent) {
			parent.insertBefore(formulaWrapper, formulaBlock);
			formulaWrapper.appendChild(formulaBlock);

			const formulaFinalStyle: EntityStyle = {
				...style,
				marginTop: undefined,
				marginBottom: undefined,
				marginLeft: undefined,
				marginRight: undefined,
			};
			formulaBlock.setAttribute('style', styleToCSS(formulaFinalStyle));
			if (selectable) (formulaBlock as HTMLElement).classList.add('element-selectable');

			if (captionText) {
				const captionId = generateElementId('formula-caption', captionText, usedIds);
				const captionStyle = getFinalStyle('formula-caption', captionId, profile, overrides);

				const caption = doc.createElement('div');
				caption.id = captionId;
				caption.setAttribute('data-type', 'formula-caption');
				caption.setAttribute('style', styleToCSS(captionStyle));
				caption.textContent = captionText;
				if (selectable) caption.classList.add('element-selectable');

				parent.insertBefore(caption, formulaWrapper.nextSibling);
			}
		}
	});
}
