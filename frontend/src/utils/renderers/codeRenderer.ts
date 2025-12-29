import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { applyStyles, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render code blocks (pre > code) with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderCodeBlocks(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('pre > code, pre code').forEach((codeElement) => {
		const preElement = codeElement.parentElement;
		if (!preElement || preElement.tagName !== 'PRE') return;

		const content = codeElement.textContent || '';
		const elId = generateElementId('code', content.slice(0, 50), usedIds);

		// Apply styles to pre element (code block wrapper)
		applyStyles(
			preElement,
			'code' as EntityType,
			elId,
			profile,
			overrides,
			selectable
		);
	});
}

/**
 * Render inline code (code elements not in pre) with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderInlineCode(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('code').forEach((codeElement) => {
		// Skip code elements that are inside pre (those are handled by renderCodeBlocks)
		if (codeElement.parentElement?.tagName === 'PRE') return;

		const content = codeElement.textContent || '';
		const elId = generateElementId('code-inline', content.slice(0, 50), usedIds);

		// For inline code, we can apply styles directly, but typically we keep default styling
		// or apply minimal styles from profile
		codeElement.id = elId;
		codeElement.setAttribute('data-type', 'code-inline');
		if (selectable) codeElement.classList.add('element-selectable');

		// Apply basic styles if defined in profile for inline code
		const style = profile?.entityStyles['code-inline'] || {};
		if (style.backgroundColor) {
			codeElement.setAttribute(
				'style',
				`background-color: ${style.backgroundColor}; padding: 2px 4px; border-radius: 3px;`
			);
		}
	});
}
