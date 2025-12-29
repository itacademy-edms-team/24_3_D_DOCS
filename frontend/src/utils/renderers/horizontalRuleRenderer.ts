import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { applyStyles, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render horizontal rules (hr) with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderHorizontalRules(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('hr').forEach((el) => {
		const elId = generateElementId('hr', `hr-${usedIds.size}`, usedIds);
		applyStyles(
			el,
			'horizontal-rule' as EntityType,
			elId,
			profile,
			overrides,
			selectable
		);

		// Apply default hr styling if not overridden
		const currentStyle = el.getAttribute('style') || '';
		if (!currentStyle.includes('border')) {
			el.setAttribute(
				'style',
				`${currentStyle}; border: none; border-top: 1px solid #ccc; margin: 1em 0;`.trim()
			);
		}
	});
}
