import katex from 'katex';
import type { Profile } from '@/entities/profile/types';
import type { EntityType } from '@/entities/profile/constants';

export interface EntityStyle {
	// Typography
	fontSize?: number;
	fontWeight?: 'normal' | 'bold';
	fontStyle?: 'normal' | 'italic';
	fontFamily?: string;

	// Text formatting
	textAlign?: 'left' | 'center' | 'right' | 'justify';
	textIndent?: number;
	lineHeight?: number;

	// Margins
	marginTop?: number;
	marginBottom?: number;
	marginLeft?: number;
	marginRight?: number;
	paddingLeft?: number;

	// Border
	borderWidth?: number;
	borderColor?: string;
	borderStyle?: 'none' | 'solid' | 'dashed';

	// Size
	maxWidth?: number;

	// Colors
	color?: string;
	backgroundColor?: string;
}

const DEFAULT_ENTITY_STYLES: Record<EntityType, EntityStyle> = {
	paragraph: {
		fontSize: 14,
		fontWeight: 'normal',
		fontStyle: 'normal',
		fontFamily: 'Times New Roman',
		textAlign: 'justify',
		textIndent: 1.25,
		lineHeight: 1.5,
		marginTop: 0,
		marginBottom: 10,
	},
	heading: {
		fontSize: 14,
		fontWeight: 'bold',
		fontStyle: 'normal',
		fontFamily: 'Times New Roman',
		textAlign: 'left',
		textIndent: 0,
		lineHeight: 1.5,
		marginTop: 0,
		marginBottom: 0,
	},
	image: {
		textAlign: 'center',
		marginTop: 10,
		marginBottom: 5,
		maxWidth: 100,
	},
	'image-caption': {
		fontSize: 12,
		fontStyle: 'italic',
		fontFamily: 'Times New Roman',
		textAlign: 'center',
		marginTop: 5,
		marginBottom: 15,
	},
	'ordered-list': {
		fontSize: 14,
		fontFamily: 'Times New Roman',
		lineHeight: 1.5,
		marginTop: 10,
		marginBottom: 10,
		marginLeft: 20,
	},
	'unordered-list': {
		fontSize: 14,
		fontFamily: 'Times New Roman',
		lineHeight: 1.5,
		marginTop: 10,
		marginBottom: 10,
		marginLeft: 20,
	},
	table: {
		fontSize: 14,
		fontFamily: 'Times New Roman',
		textAlign: 'left',
		marginTop: 10,
		marginBottom: 10,
		borderWidth: 1,
		borderColor: '#333333',
		borderStyle: 'solid',
	},
	'table-caption': {
		fontSize: 12,
		fontStyle: 'italic',
		fontFamily: 'Times New Roman',
		textAlign: 'center',
		marginTop: 5,
		marginBottom: 15,
	},
	formula: {
		fontFamily: 'Times New Roman',
		textAlign: 'center',
		marginTop: 15,
		marginBottom: 15,
	},
	'formula-caption': {
		fontSize: 12,
		fontStyle: 'italic',
		fontFamily: 'Times New Roman',
		textAlign: 'center',
		marginTop: 5,
		marginBottom: 15,
	},
};

/**
 * Get base style for entity type (DEFAULT + Profile overrides)
 */
export function getBaseStyle(entityType: EntityType, profile: Profile | null): EntityStyle {
	const defaultStyle = DEFAULT_ENTITY_STYLES[entityType];
	const profileStyle = profile?.entities?.[entityType] || {};
	return { ...defaultStyle, ...profileStyle };
}

/**
 * Get final style for element (BASE + Element overrides)
 */
export function getFinalStyle(
	entityType: EntityType,
	elementId: string,
	profile: Profile | null,
	overrides: Record<string, EntityStyle>
): EntityStyle {
	const baseStyle = getBaseStyle(entityType, profile);
	const override = overrides[elementId] || {};
	return { ...baseStyle, ...override };
}

/**
 * Get fallback fonts for a given font family
 */
function getFontFallbacks(fontFamily: string): string {
	// Map of fonts to their fallback families
	const fontFallbacks: Record<string, string> = {
		'Times New Roman': 'Times, serif',
		'Georgia': 'serif',
		'Arial': 'Helvetica, sans-serif',
		'Calibri': 'sans-serif',
		'Verdana': 'sans-serif',
		'Courier New': 'Courier, monospace',
	};

	// Check if we have a specific fallback
	if (fontFallbacks[fontFamily]) {
		return fontFallbacks[fontFamily];
	}

	// Default fallback based on common font types
	if (fontFamily.toLowerCase().includes('serif') || fontFamily.toLowerCase().includes('times') || fontFamily.toLowerCase().includes('georgia')) {
		return 'serif';
	}
	if (fontFamily.toLowerCase().includes('mono') || fontFamily.toLowerCase().includes('courier')) {
		return 'monospace';
	}
	
	// Default to sans-serif for unknown fonts
	return 'sans-serif';
}

/**
 * Format font family name for CSS (add quotes if needed)
 */
function formatFontFamily(fontFamily: string): string {
	// If font name contains spaces, wrap it in quotes
	if (fontFamily.includes(' ')) {
		return `'${fontFamily}'`;
	}
	return fontFamily;
}

/**
 * Convert EntityStyle to CSS string
 */
export function styleToCSS(style: EntityStyle): string {
	const rules: string[] = [];

	// Typography
	if (style.fontSize !== undefined) rules.push(`font-size: ${style.fontSize}pt`);
	if (style.fontWeight) rules.push(`font-weight: ${style.fontWeight}`);
	if (style.fontStyle) rules.push(`font-style: ${style.fontStyle}`);
	if (style.fontFamily) {
		const formattedFont = formatFontFamily(style.fontFamily);
		const fallbacks = getFontFallbacks(style.fontFamily);
		rules.push(`font-family: ${formattedFont}, ${fallbacks}`);
	}

	// Text formatting
	if (style.textAlign) rules.push(`text-align: ${style.textAlign}`);
	if (style.textIndent !== undefined) rules.push(`text-indent: ${style.textIndent}cm`);
	if (style.lineHeight !== undefined) rules.push(`line-height: ${style.lineHeight}`);

	// Margins
	if (style.marginTop !== undefined) rules.push(`margin-top: ${style.marginTop}pt`);
	if (style.marginBottom !== undefined) rules.push(`margin-bottom: ${style.marginBottom}pt`);
	if (style.marginLeft !== undefined) rules.push(`margin-left: ${style.marginLeft}pt`);
	if (style.marginRight !== undefined) rules.push(`margin-right: ${style.marginRight}pt`);
	if (style.paddingLeft !== undefined) rules.push(`padding-left: ${style.paddingLeft}pt`);

	// Border
	if (style.borderWidth && style.borderStyle && style.borderStyle !== 'none') {
		rules.push(`border: ${style.borderWidth}px ${style.borderStyle} ${style.borderColor || '#333'}`);
	}

	// Size
	if (style.maxWidth !== undefined) rules.push(`max-width: ${style.maxWidth}%`);

	// Colors
	if (style.color) rules.push(`color: ${style.color}`);
	if (style.backgroundColor) rules.push(`background-color: ${style.backgroundColor}`);

	return rules.join('; ');
}

/**
 * Generate stable element ID based on content hash
 */
export function generateElementId(type: string, content: string, usedIds: Set<string>): string {
	let hash = 0;
	const str = content.slice(0, 100).trim();

	for (let i = 0; i < str.length; i++) {
		hash = ((hash << 5) - hash) + str.charCodeAt(i);
		hash = hash & hash;
	}

	const hashStr = Math.abs(hash).toString(36);
	let id = `${type}-${hashStr}`;
	let counter = 0;

	while (usedIds.has(id)) {
		counter++;
		id = `${type}-${hashStr}-${counter}`;
	}

	usedIds.add(id);
	return id;
}

/**
 * Render LaTeX formulas
 */
export function renderLatex(text: string): string {
	// Block formulas: $$...$$
	text = text.replace(/\$\$([^$]+)\$\$/g, (_, formula) => {
		try {
			const html = katex.renderToString(formula.trim(), {
				displayMode: true,
				throwOnError: false,
			});
			return `<div class="formula-block">${html}</div>`;
		} catch {
			return `<div class="formula-block formula-error">${formula}</div>`;
		}
	});

	// Inline formulas: $...$
	text = text.replace(/\$([^$]+)\$/g, (_, formula) => {
		try {
			return katex.renderToString(formula.trim(), {
				displayMode: false,
				throwOnError: false,
			});
		} catch {
			return `<span class="formula-error">${formula}</span>`;
		}
	});

	return text;
}

/**
 * Apply styles to element
 */
export function applyStyles(
	element: Element,
	entityType: EntityType,
	elementId: string,
	profile: Profile | null,
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
