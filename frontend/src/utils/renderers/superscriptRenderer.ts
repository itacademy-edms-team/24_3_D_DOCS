import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { applyStyles, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render superscript text (sup elements) with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderSuperscript(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('sup').forEach((el) => {
		const content = el.textContent || '';
		const elId = generateElementId('sup', content.slice(0, 50), usedIds);
		applyStyles(
			el,
			'superscript' as EntityType,
			elId,
			profile,
			overrides,
			selectable
		);

		// Apply default superscript styling if not overridden in profile
		const currentStyle = el.getAttribute('style') || '';
		if (!currentStyle.includes('vertical-align')) {
			const defaultStyle = 'vertical-align: super; font-size: 0.8em;';
			el.setAttribute(
				'style',
				currentStyle ? `${currentStyle}; ${defaultStyle}` : defaultStyle
			);
		}
	});
}
