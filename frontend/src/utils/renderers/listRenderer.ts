import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { getFinalStyle, styleToCSS, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Calculate nesting level for a list item (li)
 * Level 0 = top level, level 1 = nested once, etc.
 * Pure function - no side effects
 */
function calculateListItemLevel(li: Element): number {
	let level = 0;
	let parent: Element | null = li.parentElement;
	
	// Count how many ul/ol elements are ancestors
	while (parent && parent !== li.ownerDocument?.body) {
		if (parent.tagName === 'UL' || parent.tagName === 'OL') {
			level++;
		}
		parent = parent.parentElement;
	}
	
	return Math.max(0, level - 1); // Subtract 1 because the direct parent is already counted
}

/**
 * Calculate indent value for list items
 * Pure function - no side effects
 */
function calculateListIndent(
	style: EntityStyle,
	profile: ProfileData | null,
	nestingLevel: number
): number | null {
	// If useParagraphTextIndent is enabled, use textIndent from paragraph styles
	if (style.listUseParagraphTextIndent === true) {
		const paragraphStyle = profile?.entityStyles?.['paragraph'];
		if (paragraphStyle?.textIndent !== undefined && paragraphStyle.textIndent > 0) {
			// Convert cm to mm, then multiply by nesting level
			return (paragraphStyle.textIndent * 10) * nestingLevel;
		}
		return null;
	}
	
	// Otherwise use listAdditionalIndent
	if (style.listAdditionalIndent !== undefined) {
		return style.listAdditionalIndent * nestingLevel;
	}
	
	return null;
}

/**
 * Render unordered lists (ul) with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderUnorderedLists(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('ul').forEach((el) => {
		const content = el.textContent || '';
		const elId = generateElementId('ul', content.slice(0, 50), usedIds);
		const style = getFinalStyle('unordered-list' as EntityType, elId, profile, overrides);

		const listStyle: EntityStyle = { ...style, textIndent: undefined };

		el.id = elId;
		el.setAttribute('data-type', 'unordered-list');
		el.setAttribute('style', styleToCSS(listStyle));
		if (selectable) el.classList.add('element-selectable');

		// Apply indentation to list items based on nesting level
		const listItems = el.querySelectorAll('li');
		listItems.forEach((li) => {
			const nestingLevel = calculateListItemLevel(li);
			const indentValue = calculateListIndent(style, profile, nestingLevel);
			
			if (indentValue !== null && indentValue !== 0) {
				// Convert mm to pt (1 mm = 2.83465 pt)
				const indentPt = indentValue * 2.83465;
				const currentStyle = (li as HTMLElement).getAttribute('style') || '';
				(li as HTMLElement).setAttribute(
					'style',
					`${currentStyle}; margin-left: ${indentPt}pt;`.trim()
				);
			}
		});
	});
}

/**
 * Render ordered lists (ol) with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderOrderedLists(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	doc.querySelectorAll('ol').forEach((el) => {
		const content = el.textContent || '';
		const elId = generateElementId('ol', content.slice(0, 50), usedIds);
		const style = getFinalStyle('ordered-list' as EntityType, elId, profile, overrides);

		const listStyle: EntityStyle = { ...style, textIndent: undefined };

		el.id = elId;
		el.setAttribute('data-type', 'ordered-list');
		el.setAttribute('style', styleToCSS(listStyle));
		if (selectable) el.classList.add('element-selectable');

		// Apply indentation to list items based on nesting level
		const listItems = el.querySelectorAll('li');
		listItems.forEach((li) => {
			const nestingLevel = calculateListItemLevel(li);
			const indentValue = calculateListIndent(style, profile, nestingLevel);
			
			if (indentValue !== null && indentValue !== 0) {
				// Convert mm to pt (1 mm = 2.83465 pt)
				const indentPt = indentValue * 2.83465;
				const currentStyle = (li as HTMLElement).getAttribute('style') || '';
				(li as HTMLElement).setAttribute(
					'style',
					`${currentStyle}; margin-left: ${indentPt}pt;`.trim()
				);
			}
		});
	});
}

/**
 * Render task lists (ul with checkboxes) with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderTaskLists(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	// Task lists are typically rendered as ul with input checkboxes
	doc.querySelectorAll('ul').forEach((ul) => {
		const hasCheckboxes = ul.querySelector('li input[type="checkbox"]');
		if (hasCheckboxes) {
			// Apply same styles as unordered lists
			renderUnorderedLists(doc, usedIds, profile, overrides, selectable);
		}
	});
}
