import { Canvas, Text, Line, type FabricObject } from 'fabric';
import { A4_WIDTH_PX, A4_HEIGHT_PX, MM_TO_PX } from '../constants';

/**
 * Element bounds with center points
 */
export interface ElementBounds {
	left: number;
	right: number;
	top: number;
	bottom: number;
	centerX: number;
	centerY: number;
}

/**
 * Serialized element data
 */
export interface SerializedElement {
	type: string;
	data: any;
}

/**
 * Calculate element bounds (works for both lines and regular objects)
 */
export function getElementBounds(obj: FabricObject): ElementBounds {
	if (obj.type === 'line') {
		const lineObj = obj as Line;
		const x1 = lineObj.x1!;
		const y1 = lineObj.y1!;
		const x2 = lineObj.x2!;
		const y2 = lineObj.y2!;
		const left = Math.min(x1, x2);
		const right = Math.max(x1, x2);
		const top = Math.min(y1, y2);
		const bottom = Math.max(y1, y2);
		return {
			left,
			right,
			top,
			bottom,
			centerX: (left + right) / 2,
			centerY: (top + bottom) / 2,
		};
	} else {
		const width = obj.width! * (obj.scaleX || 1);
		const height = obj.height! * (obj.scaleY || 1);
		return {
			left: obj.left!,
			right: obj.left! + width,
			top: obj.top!,
			bottom: obj.top! + height,
			centerX: obj.left! + width / 2,
			centerY: obj.top! + height / 2,
		};
	}
}

/**
 * Constrain element to stay within A4 page bounds
 */
export function constrainToBounds(obj: FabricObject): void {
	const bounds = getElementBounds(obj);
	
	if (obj.type === 'line') {
		const lineObj = obj as Line;
		let x1 = lineObj.x1!;
		let y1 = lineObj.y1!;
		let x2 = lineObj.x2!;
		let y2 = lineObj.y2!;
		
		// Constrain line points within page bounds
		x1 = Math.max(0, Math.min(x1, A4_WIDTH_PX));
		y1 = Math.max(0, Math.min(y1, A4_HEIGHT_PX));
		x2 = Math.max(0, Math.min(x2, A4_WIDTH_PX));
		y2 = Math.max(0, Math.min(y2, A4_HEIGHT_PX));
		
		lineObj.set({ x1, y1, x2, y2 });
	} else {
		// For text and other objects, constrain by bounding box
		let left = obj.left!;
		let top = obj.top!;
		
		// Calculate object dimensions
		const objWidth = bounds.right - bounds.left;
		const objHeight = bounds.bottom - bounds.top;
		
		// Constrain position so object doesn't go outside bounds
		left = Math.max(0, Math.min(left, A4_WIDTH_PX - objWidth));
		top = Math.max(0, Math.min(top, A4_HEIGHT_PX - objHeight));
		
		obj.set({ left, top });
	}
}

/**
 * Serialize a Fabric object to a serializable format
 */
export function serializeElement(obj: FabricObject): SerializedElement | null {
	if (obj.type === 'text' || obj.type === 'i-text' || obj.type === 'textbox') {
		const textObj = obj as Text;
		const isVariable = (obj as any).isVariable;
		return {
			type: isVariable ? 'variable' : 'text',
			data: {
				text: textObj.text || '',
				fontSize: textObj.fontSize,
				fontFamily: textObj.fontFamily,
				fontWeight: textObj.fontWeight,
				fontStyle: textObj.fontStyle,
				textAlign: textObj.textAlign,
				lineHeight: textObj.lineHeight,
				fill: (textObj.fill as string) || '#000000',
				left: obj.left!,
				top: obj.top!,
				isVariable: isVariable,
			},
		};
	} else if (obj.type === 'line') {
		const lineObj = obj as Line;
		return {
			type: 'line',
			data: {
				x1: lineObj.x1!,
				y1: lineObj.y1!,
				x2: lineObj.x2!,
				y2: lineObj.y2!,
				stroke: (lineObj.stroke as string) || '#000000',
				strokeWidth: lineObj.strokeWidth,
				strokeDashArray: (lineObj as any).strokeDashArray,
			},
		};
	}
	return null;
}

/**
 * Validate and fix element bounds - move elements that are outside page boundaries
 * Returns the number of fixed elements
 */
export function validateAndFixElementBounds(canvas: Canvas): number {
	const objects = canvas.getObjects();
	let fixedCount = 0;
	
	// Default position for elements that are out of bounds (5mm down and right)
	const defaultOffsetX = 5 * MM_TO_PX;
	const defaultOffsetY = 5 * MM_TO_PX;
	
	for (const obj of objects) {
		const bounds = getElementBounds(obj);
		let needsFix = false;
		
		// Check if element is outside page boundaries
		if (bounds.left < 0 || bounds.top < 0 || 
			bounds.right > A4_WIDTH_PX || bounds.bottom > A4_HEIGHT_PX) {
			needsFix = true;
		}
		
		if (needsFix) {
			if (obj.type === 'line') {
				const lineObj = obj as Line;
				// For lines, move both points
				const x1 = lineObj.x1!;
				const y1 = lineObj.y1!;
				const x2 = lineObj.x2!;
				const y2 = lineObj.y2!;
				
				// Calculate offset to move line to default position
				const currentMinX = Math.min(x1, x2);
				const currentMinY = Math.min(y1, y2);
				const offsetX = defaultOffsetX - currentMinX;
				const offsetY = defaultOffsetY - currentMinY;
				
				lineObj.set({
					x1: x1 + offsetX,
					y1: y1 + offsetY,
					x2: x2 + offsetX,
					y2: y2 + offsetY,
				});
				lineObj.setCoords();
			} else {
				// For text and other objects, move by left/top
				const currentLeft = obj.left!;
				const currentTop = obj.top!;
				const offsetX = defaultOffsetX - currentLeft;
				const offsetY = defaultOffsetY - currentTop;
				
				obj.set({
					left: currentLeft + offsetX,
					top: currentTop + offsetY,
				});
				obj.setCoords();
			}
			fixedCount++;
		}
	}
	
	if (fixedCount > 0) {
		canvas.renderAll();
	}
	
	return fixedCount;
}

/**
 * Deserialize element data back to a Fabric object
 */
export function deserializeElement(
	item: SerializedElement,
	offsetX: number,
	offsetY: number
): FabricObject | null {
	if (item.type === 'text' || item.type === 'variable') {
		const text = new Text(item.data.text || '', {
			left: item.data.left + offsetX,
			top: item.data.top + offsetY,
			fontSize: item.data.fontSize || 12,
			fontFamily: item.data.fontFamily || 'Arial',
			fontWeight: item.data.fontWeight || 'normal',
			fontStyle: item.data.fontStyle || 'normal',
			textAlign: item.data.textAlign || 'left',
			lineHeight: item.data.lineHeight || 1.2,
			fill: item.data.fill || '#000000',
		});
		(text as any).id = crypto.randomUUID();
		if (item.type === 'variable' || item.data.isVariable) {
			(text as any).isVariable = true;
		}
		return text;
	} else if (item.type === 'line') {
		const line = new Line(
			[
				item.data.x1 + offsetX,
				item.data.y1 + offsetY,
				item.data.x2 + offsetX,
				item.data.y2 + offsetY,
			],
			{
				stroke: item.data.stroke || '#000000',
				strokeWidth: item.data.strokeWidth || 1 * MM_TO_PX,
				strokeDashArray: item.data.strokeDashArray,
				perPixelTargetFind: true,
				padding: 10,
			}
		);
		(line as any).id = crypto.randomUUID();
		return line;
	}
	return null;
}
