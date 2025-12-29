import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { applyStyles, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render strikethrough text (del elements) with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderStrikethrough(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('del, s').forEach((el) => {
		const content = el.textContent || '';
		const elId = generateElementId('del', content.slice(0, 50), usedIds);
		applyStyles(
			el,
			'strikethrough' as EntityType,
			elId,
			profile,
			overrides,
			selectable
		);

		// Apply default strikethrough styling if not overridden in profile
		const currentStyle = el.getAttribute('style') || '';
		if (!currentStyle.includes('text-decoration')) {
			const defaultStyle = 'text-decoration: line-through;';
			el.setAttribute(
				'style',
				currentStyle ? `${currentStyle}; ${defaultStyle}` : defaultStyle
			);
		}
	});
}
