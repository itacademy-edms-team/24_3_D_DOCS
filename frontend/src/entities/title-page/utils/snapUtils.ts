import { Canvas, Line, type FabricObject } from 'fabric';
import { A4_WIDTH_PX, A4_HEIGHT_PX, SNAP_THRESHOLD_MM, MM_TO_PX, type AlignmentGuide, type ActiveSnapGuide } from '../constants';
import { getElementBounds, type ElementBounds } from './elementUtils';

/**
 * Result of applying snap
 */
interface SnapResult {
	x: number;
	y: number;
	activeGuides: ActiveSnapGuide[];
}

/**
 * Find alignment guides for a moving object
 */
export function findAlignmentGuides(
	canvas: Canvas,
	movingObj: FabricObject,
	isAltPressed: boolean,
	excludeId?: string
): AlignmentGuide[] {
	if (!canvas || isAltPressed) return [];
	
	const guides: AlignmentGuide[] = [];
	const movingBounds = getElementBounds(movingObj);
	const allObjects = canvas.getObjects();
	
	// Check page boundaries
	const pageCenterX = A4_WIDTH_PX / 2;
	const pageCenterY = A4_HEIGHT_PX / 2;
	const threshold = SNAP_THRESHOLD_MM * MM_TO_PX;
	
	// Vertical guides (left, center, right of page)
	if (Math.abs(movingBounds.left - 0) < threshold) {
		guides.push({ type: 'vertical', position: 0 });
	}
	if (Math.abs(movingBounds.centerX - pageCenterX) < threshold) {
		guides.push({ type: 'vertical', position: pageCenterX });
	}
	if (Math.abs(movingBounds.right - A4_WIDTH_PX) < threshold) {
		guides.push({ type: 'vertical', position: A4_WIDTH_PX });
	}
	
	// Horizontal guides (top, center, bottom of page)
	if (Math.abs(movingBounds.top - 0) < threshold) {
		guides.push({ type: 'horizontal', position: 0 });
	}
	if (Math.abs(movingBounds.centerY - pageCenterY) < threshold) {
		guides.push({ type: 'horizontal', position: pageCenterY });
	}
	if (Math.abs(movingBounds.bottom - A4_HEIGHT_PX) < threshold) {
		guides.push({ type: 'horizontal', position: A4_HEIGHT_PX });
	}
	
	// Check alignment with other objects
	for (const obj of allObjects) {
		if (obj === movingObj || (obj as any).id === excludeId) continue;
		
		const otherBounds = getElementBounds(obj);
		
		// Vertical alignments
		if (Math.abs(movingBounds.left - otherBounds.left) < threshold) {
			guides.push({ type: 'vertical', position: otherBounds.left });
		}
		if (Math.abs(movingBounds.centerX - otherBounds.centerX) < threshold) {
			guides.push({ type: 'vertical', position: otherBounds.centerX });
		}
		if (Math.abs(movingBounds.right - otherBounds.right) < threshold) {
			guides.push({ type: 'vertical', position: otherBounds.right });
		}
		
		// Horizontal alignments
		if (Math.abs(movingBounds.top - otherBounds.top) < threshold) {
			guides.push({ type: 'horizontal', position: otherBounds.top });
		}
		if (Math.abs(movingBounds.centerY - otherBounds.centerY) < threshold) {
			guides.push({ type: 'horizontal', position: otherBounds.centerY });
		}
		if (Math.abs(movingBounds.bottom - otherBounds.bottom) < threshold) {
			guides.push({ type: 'horizontal', position: otherBounds.bottom });
		}
	}
	
	return guides;
}

/**
 * Apply snap to an object based on guides
 */
export function applySnap(
	obj: FabricObject,
	guides: AlignmentGuide[]
): SnapResult {
	const threshold = SNAP_THRESHOLD_MM * MM_TO_PX;
	
	if (guides.length === 0) {
		return { x: obj.left || 0, y: obj.top || 0, activeGuides: [] };
	}
	
	const bounds = getElementBounds(obj);
	let newX = obj.left!;
	let newY = obj.top!;
	const activeGuides: ActiveSnapGuide[] = [];
	
	// Lines need special handling
	if (obj.type === 'line') {
		const lineObj = obj as Line;
		const x1 = lineObj.x1!;
		const y1 = lineObj.y1!;
		const x2 = lineObj.x2!;
		const y2 = lineObj.y2!;
		
		// Find closest vertical guide
		const verticalGuides = guides.filter(g => g.type === 'vertical');
		if (verticalGuides.length > 0) {
			let minDiff = Infinity;
			let bestGuide: { position: number; alignmentType: string } | null = null;
			
			for (const guide of verticalGuides) {
				const diffLeft = Math.abs(bounds.left - guide.position);
				const diffCenter = Math.abs(bounds.centerX - guide.position);
				const diffRight = Math.abs(bounds.right - guide.position);
				const min = Math.min(diffLeft, diffCenter, diffRight);
				
				if (min < minDiff) {
					minDiff = min;
					if (min === diffLeft) {
						bestGuide = { position: guide.position, alignmentType: 'left' };
					} else if (min === diffCenter) {
						bestGuide = { position: guide.position, alignmentType: 'center' };
					} else {
						bestGuide = { position: guide.position, alignmentType: 'right' };
					}
				}
			}
			
			if (bestGuide !== null && minDiff < threshold) {
				let offsetX = 0;
				if (bestGuide.alignmentType === 'left') {
					offsetX = bestGuide.position - bounds.left;
				} else if (bestGuide.alignmentType === 'center') {
					offsetX = bestGuide.position - bounds.centerX;
				} else {
					offsetX = bestGuide.position - bounds.right;
				}
				
				lineObj.set({
					x1: x1 + offsetX,
					x2: x2 + offsetX,
				});
				activeGuides.push({ type: 'vertical', position: bestGuide.position });
			}
		}
		
		// Find closest horizontal guide
		const horizontalGuides = guides.filter(g => g.type === 'horizontal');
		if (horizontalGuides.length > 0) {
			let minDiff = Infinity;
			let bestGuide: { position: number; alignmentType: string } | null = null;
			
			for (const guide of horizontalGuides) {
				const diffTop = Math.abs(bounds.top - guide.position);
				const diffCenter = Math.abs(bounds.centerY - guide.position);
				const diffBottom = Math.abs(bounds.bottom - guide.position);
				const min = Math.min(diffTop, diffCenter, diffBottom);
				
				if (min < minDiff) {
					minDiff = min;
					if (min === diffTop) {
						bestGuide = { position: guide.position, alignmentType: 'top' };
					} else if (min === diffCenter) {
						bestGuide = { position: guide.position, alignmentType: 'center' };
					} else {
						bestGuide = { position: guide.position, alignmentType: 'bottom' };
					}
				}
			}
			
			if (bestGuide !== null && minDiff < threshold) {
				let offsetY = 0;
				if (bestGuide.alignmentType === 'top') {
					offsetY = bestGuide.position - bounds.top;
				} else if (bestGuide.alignmentType === 'center') {
					offsetY = bestGuide.position - bounds.centerY;
				} else {
					offsetY = bestGuide.position - bounds.bottom;
				}
				
				lineObj.set({
					y1: y1 + offsetY,
					y2: y2 + offsetY,
				});
				activeGuides.push({ type: 'horizontal', position: bestGuide.position });
			}
		}
		
		// Update left/top for lines
		const newBounds = getElementBounds(lineObj);
		newX = newBounds.left;
		newY = newBounds.top;
	} else {
		// For other objects use standard logic
		// Find closest vertical guide
		const verticalGuides = guides.filter(g => g.type === 'vertical');
		if (verticalGuides.length > 0) {
			let minDiff = Infinity;
			let bestGuide: { position: number; alignmentType: string } | null = null;
			
			for (const guide of verticalGuides) {
				const diffLeft = Math.abs(bounds.left - guide.position);
				const diffCenter = Math.abs(bounds.centerX - guide.position);
				const diffRight = Math.abs(bounds.right - guide.position);
				const min = Math.min(diffLeft, diffCenter, diffRight);
				
				if (min < minDiff) {
					minDiff = min;
					if (min === diffLeft) {
						bestGuide = { position: guide.position, alignmentType: 'left' };
					} else if (min === diffCenter) {
						bestGuide = { position: guide.position, alignmentType: 'center' };
					} else {
						bestGuide = { position: guide.position, alignmentType: 'right' };
					}
				}
			}
			
			if (bestGuide !== null && minDiff < threshold) {
				if (bestGuide.alignmentType === 'left') {
					newX = bestGuide.position;
				} else if (bestGuide.alignmentType === 'center') {
					newX = bestGuide.position - (bounds.centerX - bounds.left);
				} else {
					newX = bestGuide.position - (bounds.right - bounds.left);
				}
				activeGuides.push({ type: 'vertical', position: bestGuide.position });
			}
		}
		
		// Find closest horizontal guide
		const horizontalGuides = guides.filter(g => g.type === 'horizontal');
		if (horizontalGuides.length > 0) {
			let minDiff = Infinity;
			let bestGuide: { position: number; alignmentType: string } | null = null;
			
			for (const guide of horizontalGuides) {
				const diffTop = Math.abs(bounds.top - guide.position);
				const diffCenter = Math.abs(bounds.centerY - guide.position);
				const diffBottom = Math.abs(bounds.bottom - guide.position);
				const min = Math.min(diffTop, diffCenter, diffBottom);
				
				if (min < minDiff) {
					minDiff = min;
					if (min === diffTop) {
						bestGuide = { position: guide.position, alignmentType: 'top' };
					} else if (min === diffCenter) {
						bestGuide = { position: guide.position, alignmentType: 'center' };
					} else {
						bestGuide = { position: guide.position, alignmentType: 'bottom' };
					}
				}
			}
			
			if (bestGuide !== null && minDiff < threshold) {
				if (bestGuide.alignmentType === 'top') {
					newY = bestGuide.position;
				} else if (bestGuide.alignmentType === 'center') {
					newY = bestGuide.position - (bounds.centerY - bounds.top);
				} else {
					newY = bestGuide.position - (bounds.bottom - bounds.top);
				}
				activeGuides.push({ type: 'horizontal', position: bestGuide.position });
			}
		}
	}
	
	return { x: newX, y: newY, activeGuides };
}
