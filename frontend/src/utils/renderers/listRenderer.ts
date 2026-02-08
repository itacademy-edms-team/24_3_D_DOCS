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
 * Get text-indent value (red line) in cm for the FIRST list item only.
 * Used for typographic red line: first line of first element indents, rest do not.
 */
function getFirstItemTextIndentCm(
	style: EntityStyle,
	profile: ProfileData | null
): number {
	if (style.listUseParagraphTextIndent === true) {
		const paragraphStyle = profile?.entityStyles?.['paragraph'];
		if (paragraphStyle?.textIndent !== undefined && paragraphStyle.textIndent > 0) {
			return paragraphStyle.textIndent;
		}
		return 0;
	}
	if (style.textIndent !== undefined && style.textIndent > 0) {
		return style.textIndent;
	}
	return 0;
}

/** Block-level tags: when li's first child is one of these, marker and content split onto separate lines. */
const BLOCK_TAGS = new Set([
	'P', 'DIV', 'BLOCKQUOTE', 'PRE', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6',
	'UL', 'OL', 'TABLE', 'FIGURE', 'HR',
]);

/**
 * Inject explicit marker into first block child of li for loose list items.
 * Returns true if injection was done (so caller can set list-style: none).
 */
function injectMarkerIntoFirstBlock(li: Element, markerText: string): boolean {
	const firstChild = li.firstElementChild;
	if (!firstChild || !BLOCK_TAGS.has(firstChild.tagName)) {
		return false;
	}
	// Skip task list items (input/label structure)
	if (firstChild.tagName === 'INPUT' || firstChild.tagName === 'LABEL') {
		return false;
	}
	const span = li.ownerDocument.createElement('span');
	span.className = 'list-item-marker';
	span.textContent = markerText;
	firstChild.insertBefore(span, firstChild.firstChild);
	return true;
}

/**
 * Calculate margin-left for nested list items (not for red line).
 * Pure function - no side effects
 */
function calculateListIndent(
	style: EntityStyle,
	profile: ProfileData | null,
	nestingLevel: number
): number | null {
	if (style.listUseParagraphTextIndent === true) {
		const paragraphStyle = profile?.entityStyles?.['paragraph'];
		if (paragraphStyle?.textIndent !== undefined && paragraphStyle.textIndent > 0) {
			return (paragraphStyle.textIndent * 10) * nestingLevel;
		}
		return null;
	}
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

		// Red line (text-indent): only for the first list item
		const firstItemTextIndentCm = getFirstItemTextIndentCm(style, profile);

		// Apply indentation and inject markers for loose items (first child is block)
		const listItems = Array.from(el.querySelectorAll(':scope > li'));
		let hasInjectedMarker = false;
		listItems.forEach((li, index) => {
			const injected = injectMarkerIntoFirstBlock(li, '\u2022 ');
			if (injected) hasInjectedMarker = true;
			const nestingLevel = calculateListItemLevel(li);
			const parts: string[] = [];

			// Margin-left for nested lists
			const indentValue = calculateListIndent(style, profile, nestingLevel);
			if (indentValue !== null && indentValue !== 0) {
				const indentPt = indentValue * 2.83465;
				parts.push(`margin-left: ${indentPt}pt`);
			}

			// Красная строка: только первый элемент
			if (index === 0 && firstItemTextIndentCm > 0) {
				parts.push(`text-indent: ${firstItemTextIndentCm}cm`);
			} else {
				parts.push('text-indent: 0');
			}

			const currentStyle = (li as HTMLElement).getAttribute('style') || '';
			const newStyle = [currentStyle, ...parts].filter(Boolean).join('; ').replace(/;+/g, ';').trim();
			(li as HTMLElement).setAttribute('style', newStyle);
		});

		const listCss =
			styleToCSS(listStyle) +
			(hasInjectedMarker ? '; list-style: none' : '; list-style-position: inside');
		el.setAttribute('style', listCss);
		if (selectable) el.classList.add('element-selectable');
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

		// Red line (text-indent): only for the first list item
		const firstItemTextIndentCm = getFirstItemTextIndentCm(style, profile);

		const start = (el as HTMLOListElement).start !== undefined ? (el as HTMLOListElement).start : 1;

		// Apply indentation and inject markers for loose items (first child is block)
		const listItems = Array.from(el.querySelectorAll(':scope > li'));
		let hasInjectedMarker = false;
		listItems.forEach((li, index) => {
			const markerNum = start + index;
			const injected = injectMarkerIntoFirstBlock(li, `${markerNum}. `);
			if (injected) hasInjectedMarker = true;

			const nestingLevel = calculateListItemLevel(li);
			const parts: string[] = [];

			// Margin-left for nested lists
			const indentValue = calculateListIndent(style, profile, nestingLevel);
			if (indentValue !== null && indentValue !== 0) {
				const indentPt = indentValue * 2.83465;
				parts.push(`margin-left: ${indentPt}pt`);
			}

			// Красная строка: только первый элемент
			if (index === 0 && firstItemTextIndentCm > 0) {
				parts.push(`text-indent: ${firstItemTextIndentCm}cm`);
			} else {
				parts.push('text-indent: 0');
			}

			const currentStyle = (li as HTMLElement).getAttribute('style') || '';
			const newStyle = [currentStyle, ...parts].filter(Boolean).join('; ').replace(/;+/g, ';').trim();
			(li as HTMLElement).setAttribute('style', newStyle);
		});

		const listCss =
			styleToCSS(listStyle) +
			(hasInjectedMarker ? '; list-style: none' : '; list-style-position: inside');
		el.setAttribute('style', listCss);
		if (selectable) el.classList.add('element-selectable');
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
