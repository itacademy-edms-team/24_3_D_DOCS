import type { TitlePageElement } from '@/shared/types/titlePage';
import {
	mmToPx,
	PAGE_WIDTH_MM,
	PAGE_HEIGHT_MM,
	getElementBounds,
} from '@/shared/utils/canvasUtils';
import { getCanvasFontFamily } from './fontUtils';

export interface TextBounds {
	left: number;
	top: number;
	width: number;
	height: number;
}

/**
 * Calculate text bounds for an element
 */
export function calculateTextBounds(
	element: TitlePageElement,
	ctx: CanvasRenderingContext2D
): TextBounds {
	const x = mmToPx(element.x);
	const y = mmToPx(element.y);

	const content =
		element.type === 'variable'
			? `{${element.variableKey || ''}}`
			: element.content || '';

	const lines = content.split('\n');
	const fontSize = element.fontSize || 14;
	const fontFamily = element.fontFamily || 'Times New Roman';
	const fontWeight = element.fontWeight || 'normal';
	const fontStyle = element.fontStyle || 'normal';
	const fullFontFamily = getCanvasFontFamily(fontFamily);
	ctx.font = `${fontStyle} ${fontWeight} ${fontSize}pt ${fullFontFamily}`;
	
	const lineHeight = fontSize * (element.lineHeight || 1.2);
	const totalHeight = lines.length * lineHeight;

	let maxWidth = 0;
	lines.forEach((line) => {
		const metrics = ctx.measureText(line);
		if (metrics.width > maxWidth) maxWidth = metrics.width;
	});

	const textAlign = element.textAlign || 'left';
	let textLeft = x;
	if (textAlign === 'center') {
		textLeft = x - maxWidth / 2;
	} else if (textAlign === 'right') {
		textLeft = x - maxWidth;
	}

	return {
		left: textLeft,
		top: y,
		width: maxWidth,
		height: totalHeight,
	};
}

/**
 * Draw grid on canvas
 */
export function drawGrid(
	ctx: CanvasRenderingContext2D,
	gridSize: number
): void {
	const width = mmToPx(PAGE_WIDTH_MM);
	const height = mmToPx(PAGE_HEIGHT_MM);

	ctx.strokeStyle = '#e0e0e0';
	ctx.lineWidth = 0.5;
	const gridSizePx = mmToPx(gridSize);

	// Vertical lines
	for (let x = 0; x <= width; x += gridSizePx) {
		ctx.beginPath();
		ctx.moveTo(x, 0);
		ctx.lineTo(x, height);
		ctx.stroke();
	}

	// Horizontal lines
	for (let y = 0; y <= height; y += gridSizePx) {
		ctx.beginPath();
		ctx.moveTo(0, y);
		ctx.lineTo(width, y);
		ctx.stroke();
	}
}

/**
 * Draw a single element
 */
export function drawElement(
	element: TitlePageElement,
	ctx: CanvasRenderingContext2D
): void {
	const x = mmToPx(element.x);
	const y = mmToPx(element.y);

	if (element.type === 'line') {
		const length = mmToPx(element.length || 100);
		const thickness = mmToPx(element.thickness || 1);
		ctx.fillStyle = '#000000';
		ctx.fillRect(x, y, length, thickness);
	} else if (element.type === 'text' || element.type === 'variable') {
		const fontWeight = element.fontWeight || 'normal';
		const fontStyle = element.fontStyle || 'normal';
		const fontSize = element.fontSize || 14;
		const fontFamily = element.fontFamily || 'Times New Roman';
		const fullFontFamily = getCanvasFontFamily(fontFamily);
		
		ctx.font = `${fontStyle} ${fontWeight} ${fontSize}pt ${fullFontFamily}`;
		ctx.fillStyle = '#000000';
		ctx.textBaseline = 'top';

		const content =
			element.type === 'variable'
				? `{${element.variableKey || ''}}`
				: element.content || '';

		const lines = content.split('\n');
		const textAlign = element.textAlign || 'left';
		const lineHeight = fontSize * (element.lineHeight || 1.2);

		lines.forEach((line, i) => {
			// Calculate x position based on alignment
			let textX = x;
			if (textAlign === 'center') {
				const metrics = ctx.measureText(line);
				textX = x - metrics.width / 2;
				ctx.textAlign = 'left';
				ctx.fillText(line, textX, y + i * lineHeight);
			} else if (textAlign === 'right') {
				const metrics = ctx.measureText(line);
				textX = x - metrics.width;
				ctx.textAlign = 'left';
				ctx.fillText(line, textX, y + i * lineHeight);
			} else {
				ctx.textAlign = 'left';
				ctx.fillText(line, x, y + i * lineHeight);
			}
		});
	}
}

/**
 * Draw hover border for an element
 */
export function drawHover(
	element: TitlePageElement,
	ctx: CanvasRenderingContext2D
): void {
	const x = mmToPx(element.x);
	const y = mmToPx(element.y);

	ctx.strokeStyle = '#0066ff';
	ctx.lineWidth = 1;
	ctx.setLineDash([3, 3]);
	ctx.globalAlpha = 0.5;

	if (element.type === 'line') {
		const length = mmToPx(element.length || 100);
		const thickness = mmToPx(element.thickness || 1);
		ctx.strokeRect(x - 2, y - 2, length + 4, thickness + 4);
	} else {
		const bounds = calculateTextBounds(element, ctx);
		ctx.strokeRect(bounds.left - 2, bounds.top - 2, bounds.width + 4, bounds.height + 4);
	}

	ctx.setLineDash([]);
	ctx.globalAlpha = 1;
}

/**
 * Draw selection border for an element
 */
export function drawSelection(
	element: TitlePageElement,
	ctx: CanvasRenderingContext2D
): void {
	const x = mmToPx(element.x);
	const y = mmToPx(element.y);

	ctx.strokeStyle = '#0066ff';
	ctx.lineWidth = 2;
	ctx.setLineDash([5, 5]);

	if (element.type === 'line') {
		const length = mmToPx(element.length || 100);
		const thickness = mmToPx(element.thickness || 1);
		ctx.strokeRect(x - 2, y - 2, length + 4, thickness + 4);
	} else {
		const bounds = calculateTextBounds(element, ctx);
		ctx.strokeRect(bounds.left - 2, bounds.top - 2, bounds.width + 4, bounds.height + 4);
	}

	ctx.setLineDash([]);
}

/**
 * Draw page background and border
 */
export function drawPageBackground(ctx: CanvasRenderingContext2D): void {
	const width = mmToPx(PAGE_WIDTH_MM);
	const height = mmToPx(PAGE_HEIGHT_MM);

	// Draw page background
	ctx.fillStyle = '#ffffff';
	ctx.fillRect(0, 0, width, height);

	// Draw border
	ctx.strokeStyle = '#cccccc';
	ctx.lineWidth = 1;
	ctx.strokeRect(0, 0, width, height);
}

/**
 * Draw alignment guides
 */
export function drawAlignmentGuides(
	guides: Array<{ type: 'vertical' | 'horizontal'; position: number }>,
	ctx: CanvasRenderingContext2D
): void {
	if (guides.length === 0) return;

	const width = mmToPx(PAGE_WIDTH_MM);
	const height = mmToPx(PAGE_HEIGHT_MM);

	ctx.strokeStyle = '#0066ff';
	ctx.lineWidth = 1;
	ctx.setLineDash([4, 4]);
	ctx.globalAlpha = 0.7;

	for (const guide of guides) {
		const pos = mmToPx(guide.position);

		if (guide.type === 'vertical') {
			ctx.beginPath();
			ctx.moveTo(pos, 0);
			ctx.lineTo(pos, height);
			ctx.stroke();
		} else {
			ctx.beginPath();
			ctx.moveTo(0, pos);
			ctx.lineTo(width, pos);
			ctx.stroke();
		}
	}

	ctx.setLineDash([]);
	ctx.globalAlpha = 1;
}

/**
 * Distance information for an element
 */
export interface ElementDistances {
	top: number; // distance from top edge to page top (mm)
	bottom: number; // distance from bottom edge to page bottom (mm)
	left: number; // distance from left edge to page left (mm)
	right: number; // distance from right edge to page right (mm)
}

/**
 * Draw distance lines from element edges to page edges
 */
export function drawDistanceLines(
	element: TitlePageElement,
	distances: ElementDistances,
	ctx: CanvasRenderingContext2D
): void {
	const width = mmToPx(PAGE_WIDTH_MM);
	const height = mmToPx(PAGE_HEIGHT_MM);

	ctx.strokeStyle = '#666666';
	ctx.lineWidth = 1;
	ctx.setLineDash([2, 2]);
	ctx.globalAlpha = 0.6;
	ctx.font = '10px Arial';
	ctx.fillStyle = '#333333';
	ctx.textAlign = 'center';
	ctx.textBaseline = 'middle';

	const bounds = getElementBounds(element, ctx);
	const elementLeft = mmToPx(bounds.left);
	const elementRight = mmToPx(bounds.right);
	const elementTop = mmToPx(bounds.top);
	const elementBottom = mmToPx(bounds.bottom);

	// Top line
	ctx.beginPath();
	ctx.moveTo(elementLeft, elementTop);
	ctx.lineTo(elementLeft, 0);
	ctx.stroke();
	ctx.fillText(`${distances.top.toFixed(1)}mm`, elementLeft, elementTop / 2);

	// Bottom line
	ctx.beginPath();
	ctx.moveTo(elementLeft, elementBottom);
	ctx.lineTo(elementLeft, height);
	ctx.stroke();
	ctx.fillText(
		`${distances.bottom.toFixed(1)}mm`,
		elementLeft,
		elementBottom + (height - elementBottom) / 2
	);

	// Left line
	ctx.beginPath();
	ctx.moveTo(elementLeft, elementTop);
	ctx.lineTo(0, elementTop);
	ctx.stroke();
	ctx.textAlign = 'right';
	ctx.fillText(`${distances.left.toFixed(1)}mm`, elementLeft / 2, elementTop);

	// Right line
	ctx.beginPath();
	ctx.moveTo(elementRight, elementTop);
	ctx.lineTo(width, elementTop);
	ctx.stroke();
	ctx.textAlign = 'left';
	ctx.fillText(
		`${distances.right.toFixed(1)}mm`,
		elementRight + (width - elementRight) / 2,
		elementTop
	);

	ctx.setLineDash([]);
	ctx.globalAlpha = 1;
	ctx.textAlign = 'left';
	ctx.textBaseline = 'alphabetic';
}
