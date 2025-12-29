import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { applyStyles, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render blockquotes with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderBlockquotes(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('blockquote').forEach((el) => {
		const content = el.textContent || '';
		const elId = generateElementId('blockquote', content.slice(0, 50), usedIds);
		applyStyles(
			el,
			'blockquote' as EntityType,
			elId,
			profile,
			overrides,
			selectable
		);

		// Apply default blockquote styling if not overridden
		const currentStyle = el.getAttribute('style') || '';
		if (!currentStyle.includes('border-left')) {
			el.setAttribute(
				'style',
				`${currentStyle}; border-left: 3px solid #ccc; padding-left: 1em; margin: 1em 0;`.trim()
			);
		}
	});
}
