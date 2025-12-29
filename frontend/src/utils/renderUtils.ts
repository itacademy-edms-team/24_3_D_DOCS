import type { ProfileData, EntityStyle } from '@/entities/profile/types';

/**
 * Entity types that can be styled
 */
export type EntityType =
	| 'paragraph'
	| 'heading'
	| 'heading-1'
	| 'heading-2'
	| 'heading-3'
	| 'heading-4'
	| 'heading-5'
	| 'heading-6'
	| 'unordered-list'
	| 'ordered-list'
	| 'table'
	| 'image'
	| 'formula'
	| 'code'
	| 'blockquote'
	| 'link'
	| 'horizontal-rule'
	| 'table-caption'
	| 'image-caption'
	| 'formula-caption'
	| 'highlight'
	| 'superscript'
	| 'subscript'
	| 'strikethrough';

/**
 * Generate unique element ID based on type and content
 * Pure function - no side effects
 */
export function generateElementId(
	type: string,
	content: string,
	usedIds: Set<string>
): string {
	const baseId = `${type}-${content.slice(0, 50).replace(/[^a-z0-9]/gi, '-')}`;
	let id = baseId;
	let counter = 0;

	while (usedIds.has(id)) {
		counter++;
		id = `${baseId}-${counter}`;
	}

	usedIds.add(id);
	return id;
}

/**
 * Get final style for an element, merging profile style with overrides
 * Pure function - no side effects
 */
export function getFinalStyle(
	entityType: EntityType,
	elementId: string,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>
): EntityStyle {
	// Get base style from profile
	// For heading levels (heading-1, heading-2, etc.), try specific style first, then fallback to general 'heading'
	let baseStyle = profile?.entityStyles[entityType] || profile?.entityStyles[entityType.replace('-', '_')];
	
	if (!baseStyle && entityType.startsWith('heading-')) {
		baseStyle = profile?.entityStyles['heading'] || {};
	}
	
	if (!baseStyle) {
		baseStyle = {};
	}

	// Get override for this specific element
	const override = overrides[elementId] || {};

	// Merge base style with override (override takes precedence)
	const mergedStyle = { ...baseStyle, ...override };
	
	// Apply global line height if enabled
	if (mergedStyle.lineHeightUseGlobal === true && profile?.pageSettings?.globalLineHeight !== undefined) {
		mergedStyle.lineHeight = profile.pageSettings.globalLineHeight;
	}
	
	return mergedStyle;
}

/**
 * Convert EntityStyle object to CSS string
 * Pure function - no side effects
 */
export function styleToCSS(style: EntityStyle): string {
	const cssParts: string[] = [];

	if (style.fontFamily) {
		cssParts.push(`font-family: ${style.fontFamily}, serif`);
	}

	if (style.fontSize !== undefined) {
		cssParts.push(`font-size: ${style.fontSize}pt`);
	}

	if (style.fontWeight) {
		cssParts.push(`font-weight: ${style.fontWeight}`);
	}

	if (style.fontStyle) {
		cssParts.push(`font-style: ${style.fontStyle}`);
	}

	if (style.textAlign) {
		cssParts.push(`text-align: ${style.textAlign}`);
	}

	if (style.textIndent !== undefined) {
		cssParts.push(`text-indent: ${style.textIndent}cm`);
	}

	if (style.lineHeight !== undefined) {
		cssParts.push(`line-height: ${style.lineHeight}`);
	}

	if (style.color) {
		cssParts.push(`color: ${style.color}`);
	}

	if (style.backgroundColor) {
		cssParts.push(`background-color: ${style.backgroundColor}`);
	}

	if (style.marginTop !== undefined) {
		cssParts.push(`margin-top: ${style.marginTop}pt`);
	}

	if (style.marginBottom !== undefined) {
		cssParts.push(`margin-bottom: ${style.marginBottom}pt`);
	}

	if (style.marginLeft !== undefined) {
		cssParts.push(`margin-left: ${style.marginLeft}pt`);
	}

	if (style.marginRight !== undefined) {
		cssParts.push(`margin-right: ${style.marginRight}pt`);
	}

	if (style.paddingLeft !== undefined) {
		cssParts.push(`padding-left: ${style.paddingLeft}pt`);
	}

	if (style.borderWidth !== undefined) {
		cssParts.push(`border-width: ${style.borderWidth}px`);
	}

	if (style.borderColor) {
		cssParts.push(`border-color: ${style.borderColor}`);
	}

	if (style.borderStyle) {
		cssParts.push(`border-style: ${style.borderStyle}`);
	}

	if (style.maxWidth !== undefined) {
		cssParts.push(`max-width: ${style.maxWidth}%`);
	}

	return cssParts.join('; ');
}

/**
 * Apply styles to a DOM element
 * Pure function - modifies element but has no other side effects
 */
export function applyStyles(
	element: Element,
	entityType: EntityType,
	elementId: string,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	const style = getFinalStyle(entityType, elementId, profile, overrides);

	element.id = elementId;
	element.setAttribute('data-type', entityType);
	element.setAttribute('style', styleToCSS(style));

	if (selectable) {
		element.classList.add('element-selectable');
	}
}
