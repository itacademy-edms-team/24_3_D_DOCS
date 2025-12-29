import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { applyStyles, generateElementId, getFinalStyle } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render highlighted text (mark elements) with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderHighlight(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('mark').forEach((el) => {
		const content = el.textContent || '';
		const elId = generateElementId('mark', content.slice(0, 50), usedIds);
		applyStyles(
			el,
			'highlight' as EntityType,
			elId,
			profile,
			overrides,
			selectable
		);

		// Apply highlight-specific colors if set in profile
		const style = getFinalStyle('highlight' as EntityType, elId, profile, overrides);
		const currentStyle = el.getAttribute('style') || '';
		const styleParts: string[] = [];

		// Use highlightColor/highlightBackgroundColor if set, otherwise use color/backgroundColor, otherwise defaults
		const textColor = style.highlightColor || style.color;
		const bgColor = style.highlightBackgroundColor || style.backgroundColor;

		if (textColor) {
			styleParts.push(`color: ${textColor}`);
		}

		if (bgColor) {
			styleParts.push(`background-color: ${bgColor}`);
		}

		// Apply default highlight styling if no colors are set
		if (!bgColor && !currentStyle.includes('background-color')) {
			styleParts.push('background-color: #ffeb3b');
		}

		// Add padding and border-radius for better appearance
		if (styleParts.length > 0) {
			styleParts.push('padding: 2px 4px; border-radius: 2px');
			el.setAttribute(
				'style',
				currentStyle ? `${currentStyle}; ${styleParts.join('; ')}` : styleParts.join('; ')
			);
		} else if (!currentStyle.includes('background-color')) {
			const defaultStyle = 'background-color: #ffeb3b; padding: 2px 4px; border-radius: 2px;';
			el.setAttribute('style', defaultStyle);
		}
	});
}
