import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { applyStyles, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render links with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderLinks(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('a').forEach((el) => {
		const href = el.getAttribute('href') || '';
		const text = el.textContent || '';
		const elId = generateElementId('link', href + text, usedIds);
		applyStyles(
			el,
			'link' as EntityType,
			elId,
			profile,
			overrides,
			selectable
		);

		// Apply default link styling if not overridden
		const currentStyle = el.getAttribute('style') || '';
		if (!currentStyle.includes('color')) {
			el.setAttribute(
				'style',
				`${currentStyle}; color: #0066cc; text-decoration: underline;`.trim()
			);
		}
	});
}
