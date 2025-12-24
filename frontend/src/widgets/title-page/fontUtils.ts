/**
 * Get fallback fonts for a given font family (same logic as in renderUtils.ts)
 */
export function getFontFallbacks(fontFamily: string): string {
	const fontFallbacks: Record<string, string> = {
		'Times New Roman': 'Times, serif',
		'Georgia': 'serif',
		'Arial': 'Helvetica, sans-serif',
		'Calibri': 'sans-serif',
		'Verdana': 'sans-serif',
		'Courier New': 'Courier, monospace',
	};

	if (fontFallbacks[fontFamily]) {
		return fontFallbacks[fontFamily];
	}

	if (
		fontFamily.toLowerCase().includes('serif') ||
		fontFamily.toLowerCase().includes('times') ||
		fontFamily.toLowerCase().includes('georgia')
	) {
		return 'serif';
	}
	if (
		fontFamily.toLowerCase().includes('mono') ||
		fontFamily.toLowerCase().includes('courier')
	) {
		return 'monospace';
	}

	return 'sans-serif';
}

/**
 * Format font family name for Canvas (add quotes if needed)
 */
export function formatFontFamily(fontFamily: string): string {
	if (fontFamily.includes(' ')) {
		return `'${fontFamily}'`;
	}
	return fontFamily;
}

/**
 * Get full font family string with fallbacks for Canvas
 */
export function getCanvasFontFamily(fontFamily: string): string {
	const formattedFont = formatFontFamily(fontFamily);
	const fallbacks = getFontFallbacks(fontFamily);
	return `${formattedFont}, ${fallbacks}`;
}
