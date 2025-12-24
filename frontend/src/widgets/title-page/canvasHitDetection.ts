import type { TitlePageElement } from '@/shared/types/titlePage';
import { pxToMm } from '@/shared/utils/canvasUtils';
import { PT_TO_MM } from '@/shared/constants/canvas';
import { getCanvasFontFamily } from './fontUtils';

const HITBOX_PADDING = 5; // mm padding for easier selection

export interface Hitbox {
	minX: number;
	maxX: number;
	minY: number;
	maxY: number;
}

/**
 * Calculate hitbox for a line element
 */
export function calculateLineHitbox(
	element: TitlePageElement,
	padding: number = HITBOX_PADDING
): Hitbox {
	const length = element.length || 100;
	const thickness = element.thickness || 1;

	return {
		minX: element.x - padding,
		maxX: element.x + length + padding,
		minY: element.y - padding,
		maxY: element.y + thickness + padding,
	};
}

/**
 * Calculate hitbox for a text/variable element
 */
export function calculateTextHitbox(
	element: TitlePageElement,
	ctx: CanvasRenderingContext2D,
	padding: number = HITBOX_PADDING
): Hitbox {
	const fontSize = element.fontSize || 14;
	const fontFamily = element.fontFamily || 'Times New Roman';
	const fontWeight = element.fontWeight || 'normal';
	const fontStyle = element.fontStyle || 'normal';
	const fullFontFamily = getCanvasFontFamily(fontFamily);
	ctx.font = `${fontStyle} ${fontWeight} ${fontSize}pt ${fullFontFamily}`;

	const content =
		element.type === 'variable'
			? `{${element.variableKey || ''}}`
			: element.content || '';

	const lines = content.split('\n');
	const textAlign = element.textAlign || 'left';
	const lineHeight = fontSize * (element.lineHeight || 1.2);
	const totalHeightMm = lines.length * lineHeight * PT_TO_MM;

	let maxWidth = 0;
	lines.forEach((line) => {
		const metrics = ctx.measureText(line);
		if (metrics.width > maxWidth) maxWidth = metrics.width;
	});

	// Convert maxWidth from pixels to mm
	const maxWidthMm = pxToMm(maxWidth);

	let textLeft = element.x;
	if (textAlign === 'center') {
		textLeft = element.x - maxWidthMm / 2;
	} else if (textAlign === 'right') {
		textLeft = element.x - maxWidthMm;
	}

	return {
		minX: textLeft - padding,
		maxX: textLeft + maxWidthMm + padding,
		minY: element.y - padding,
		maxY: element.y + totalHeightMm + padding,
	};
}

/**
 * Check if a point is inside an element's hitbox
 */
export function isPointInElement(
	x: number,
	y: number,
	element: TitlePageElement,
	ctx: CanvasRenderingContext2D
): boolean {
	let hitbox: Hitbox;

	if (element.type === 'line') {
		hitbox = calculateLineHitbox(element);
	} else {
		hitbox = calculateTextHitbox(element, ctx);
	}

	return (
		x >= hitbox.minX &&
		x <= hitbox.maxX &&
		y >= hitbox.minY &&
		y <= hitbox.maxY
	);
}

/**
 * Find element at point, returning the closest one if multiple elements overlap
 */
export function findElementAtPoint(
	x: number,
	y: number,
	elements: TitlePageElement[],
	ctx: CanvasRenderingContext2D,
	getDistanceToElement: (
		x: number,
		y: number,
		el: TitlePageElement,
		ctx: CanvasRenderingContext2D
	) => number | null
): TitlePageElement | null {
	const candidates: Array<{ element: TitlePageElement; distance: number }> =
		[];

	// Check elements from top to bottom (reverse order)
	for (let i = elements.length - 1; i >= 0; i--) {
		const el = elements[i];

		if (isPointInElement(x, y, el, ctx)) {
			const distance = getDistanceToElement(x, y, el, ctx);
			if (distance !== null) {
				candidates.push({ element: el, distance });
			}
		}
	}

	// Select the closest element to the point
	if (candidates.length > 0) {
		candidates.sort((a, b) => a.distance - b.distance);
		return candidates[0].element;
	}

	return null;
}
