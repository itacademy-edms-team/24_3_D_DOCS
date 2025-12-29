import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { applyStyles, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render subscript text (sub elements) with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderSubscript(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('sub').forEach((el) => {
		const content = el.textContent || '';
		const elId = generateElementId('sub', content.slice(0, 50), usedIds);
		applyStyles(
			el,
			'subscript' as EntityType,
			elId,
			profile,
			overrides,
			selectable
		);

		// Apply default subscript styling if not overridden in profile
		const currentStyle = el.getAttribute('style') || '';
		if (!currentStyle.includes('vertical-align')) {
			const defaultStyle = 'vertical-align: sub; font-size: 0.8em;';
			el.setAttribute(
				'style',
				currentStyle ? `${currentStyle}; ${defaultStyle}` : defaultStyle
			);
		}
	});
}
