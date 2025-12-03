import type { TitlePageElement } from '../../../../shared/src/types';
import { PAGE_WIDTH_MM, PAGE_HEIGHT_MM, MM_TO_PX, PT_TO_MM } from '../../../../shared/src/constants';

/**
 * Convert millimeters to pixels
 */
export function mmToPx(mm: number): number {
  return mm * MM_TO_PX;
}

/**
 * Convert pixels to millimeters
 */
export function pxToMm(px: number): number {
  return px / MM_TO_PX;
}

/**
 * Get canvas coordinates from mouse event, accounting for zoom
 */
export function getCanvasCoords(
  e: React.MouseEvent<HTMLCanvasElement>,
  canvas: HTMLCanvasElement | null,
  zoom: number
): { x: number; y: number } {
  if (!canvas) return { x: 0, y: 0 };
  const rect = canvas.getBoundingClientRect();
  return {
    x: (e.clientX - rect.left) / zoom,
    y: (e.clientY - rect.top) / zoom,
  };
}

/**
 * Calculate distance from point to element center
 */
export function getDistanceToElement(
  x: number,
  y: number,
  element: TitlePageElement,
  ctx: CanvasRenderingContext2D
): number | null {
  if (element.type === 'line') {
    const length = element.length || 100;
    const thickness = element.thickness || 1;
    const centerX = element.x + length / 2;
    const centerY = element.y + thickness / 2;
    return Math.sqrt((x - centerX) ** 2 + (y - centerY) ** 2);
  } else {
    const fontSize = element.fontSize || 14;
    const fontFamily = element.fontFamily || 'Times New Roman';
    const fontWeight = element.fontWeight || 'normal';
    const fontStyle = element.fontStyle || 'normal';
    ctx.font = `${fontStyle} ${fontWeight} ${fontSize}pt ${fontFamily}`;
    
    const content = element.type === 'variable' 
      ? `{${element.variableKey || ''}}`
      : (element.content || '');
    
    const lines = content.split('\n');
    const textAlign = element.textAlign || 'left';
    const lineHeight = fontSize * (element.lineHeight || 1.2);
    
    let maxWidth = 0;
    lines.forEach(line => {
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
    
    // Convert totalHeight from pt to mm
    const totalHeightMm = lines.length * lineHeight * PT_TO_MM;
    
    const centerX = textLeft + maxWidthMm / 2;
    const centerY = element.y + totalHeightMm / 2;
    return Math.sqrt((x - centerX) ** 2 + (y - centerY) ** 2);
  }
}

export { PAGE_WIDTH_MM, PAGE_HEIGHT_MM };

/**
 * Get bounding box for an element in mm
 */
export interface ElementBounds {
  left: number;
  right: number;
  top: number;
  bottom: number;
  centerX: number;
  centerY: number;
}

export function getElementBounds(
  element: TitlePageElement,
  ctx: CanvasRenderingContext2D
): ElementBounds {
  if (element.type === 'line') {
    const length = element.length || 100;
    const thickness = element.thickness || 1;
    return {
      left: element.x,
      right: element.x + length,
      top: element.y,
      bottom: element.y + thickness,
      centerX: element.x + length / 2,
      centerY: element.y + thickness / 2,
    };
  } else {
    const fontSize = element.fontSize || 14;
    const fontFamily = element.fontFamily || 'Times New Roman';
    const fontWeight = element.fontWeight || 'normal';
    const fontStyle = element.fontStyle || 'normal';
    ctx.font = `${fontStyle} ${fontWeight} ${fontSize}pt ${fontFamily}`;
    
    const content = element.type === 'variable' 
      ? `{${element.variableKey || ''}}`
      : (element.content || '');
    
    const lines = content.split('\n');
    const textAlign = element.textAlign || 'left';
    const lineHeight = fontSize * (element.lineHeight || 1.2);
    const totalHeightMm = lines.length * lineHeight * PT_TO_MM;
    
    let maxWidth = 0;
    lines.forEach(line => {
      const metrics = ctx.measureText(line);
      if (metrics.width > maxWidth) maxWidth = metrics.width;
    });
    
    const maxWidthMm = pxToMm(maxWidth);
    
    let textLeft = element.x;
    if (textAlign === 'center') {
      textLeft = element.x - maxWidthMm / 2;
    } else if (textAlign === 'right') {
      textLeft = element.x - maxWidthMm;
    }
    
    return {
      left: textLeft,
      right: textLeft + maxWidthMm,
      top: element.y,
      bottom: element.y + totalHeightMm,
      centerX: textLeft + maxWidthMm / 2,
      centerY: element.y + totalHeightMm / 2,
    };
  }
}

/**
 * Alignment guide information
 */
export interface AlignmentGuide {
  type: 'vertical' | 'horizontal';
  position: number; // in mm
  elementId: string; // element that this guide aligns with
  alignmentType: 'left' | 'right' | 'center' | 'top' | 'bottom';
}

const SNAP_THRESHOLD_MM = 5; // 5mm threshold for snapping

/**
 * Find alignment guides for a moving element
 */
export function findAlignmentGuides(
  movingElement: TitlePageElement,
  otherElements: TitlePageElement[],
  ctx: CanvasRenderingContext2D,
  snapEnabled: boolean = true,
  excludeElementIds: string[] = []
): AlignmentGuide[] {
  if (!snapEnabled) return [];
  
  const guides: AlignmentGuide[] = [];
  const movingBounds = getElementBounds(movingElement, ctx);
  
  for (const otherElement of otherElements) {
    // Skip the moving element itself and any excluded elements (e.g., elements in the same group)
    if (otherElement.id === movingElement.id || excludeElementIds.includes(otherElement.id)) continue;
    
    const otherBounds = getElementBounds(otherElement, ctx);
    
    // Check vertical alignments (left, center, right)
    const leftDiff = Math.abs(movingBounds.left - otherBounds.left);
    const centerDiff = Math.abs(movingBounds.centerX - otherBounds.centerX);
    const rightDiff = Math.abs(movingBounds.right - otherBounds.right);
    
    if (leftDiff < SNAP_THRESHOLD_MM) {
      guides.push({
        type: 'vertical',
        position: otherBounds.left,
        elementId: otherElement.id,
        alignmentType: 'left',
      });
    }
    
    if (centerDiff < SNAP_THRESHOLD_MM) {
      guides.push({
        type: 'vertical',
        position: otherBounds.centerX,
        elementId: otherElement.id,
        alignmentType: 'center',
      });
    }
    
    if (rightDiff < SNAP_THRESHOLD_MM) {
      guides.push({
        type: 'vertical',
        position: otherBounds.right,
        elementId: otherElement.id,
        alignmentType: 'right',
      });
    }
    
    // Check horizontal alignments (top, center, bottom)
    const topDiff = Math.abs(movingBounds.top - otherBounds.top);
    const centerYDiff = Math.abs(movingBounds.centerY - otherBounds.centerY);
    const bottomDiff = Math.abs(movingBounds.bottom - otherBounds.bottom);
    
    if (topDiff < SNAP_THRESHOLD_MM) {
      guides.push({
        type: 'horizontal',
        position: otherBounds.top,
        elementId: otherElement.id,
        alignmentType: 'top',
      });
    }
    
    if (centerYDiff < SNAP_THRESHOLD_MM) {
      guides.push({
        type: 'horizontal',
        position: otherBounds.centerY,
        elementId: otherElement.id,
        alignmentType: 'center',
      });
    }
    
    if (bottomDiff < SNAP_THRESHOLD_MM) {
      guides.push({
        type: 'horizontal',
        position: otherBounds.bottom,
        elementId: otherElement.id,
        alignmentType: 'bottom',
      });
    }
  }
  
  return guides;
}

/**
 * Apply snap to position based on alignment guides
 */
export function applySnap(
  element: TitlePageElement,
  newX: number,
  newY: number,
  guides: AlignmentGuide[],
  ctx: CanvasRenderingContext2D
): { x: number; y: number } {
  if (guides.length === 0) return { x: newX, y: newY };
  
  // Create a temporary element with new position to calculate bounds
  const tempElement = { ...element, x: newX, y: newY };
  const bounds = getElementBounds(tempElement, ctx);
  
  let snappedX = newX;
  let snappedY = newY;
  
  // Find closest vertical guide
  const verticalGuides = guides.filter(g => g.type === 'vertical');
  if (verticalGuides.length > 0) {
    let minDiff = Infinity;
    let bestGuide: AlignmentGuide | null = null;
    
    for (const guide of verticalGuides) {
      let diff = 0;
      if (guide.alignmentType === 'left') {
        diff = Math.abs(bounds.left - guide.position);
      } else if (guide.alignmentType === 'center') {
        diff = Math.abs(bounds.centerX - guide.position);
      } else if (guide.alignmentType === 'right') {
        diff = Math.abs(bounds.right - guide.position);
      }
      
      if (diff < minDiff) {
        minDiff = diff;
        bestGuide = guide;
      }
    }
    
    if (bestGuide && minDiff < SNAP_THRESHOLD_MM) {
      if (bestGuide.alignmentType === 'left') {
        snappedX = newX + (bestGuide.position - bounds.left);
      } else if (bestGuide.alignmentType === 'center') {
        snappedX = newX + (bestGuide.position - bounds.centerX);
      } else if (bestGuide.alignmentType === 'right') {
        snappedX = newX + (bestGuide.position - bounds.right);
      }
    }
  }
  
  // Find closest horizontal guide
  const horizontalGuides = guides.filter(g => g.type === 'horizontal');
  if (horizontalGuides.length > 0) {
    let minDiff = Infinity;
    let bestGuide: AlignmentGuide | null = null;
    
    for (const guide of horizontalGuides) {
      let diff = 0;
      if (guide.alignmentType === 'top') {
        diff = Math.abs(bounds.top - guide.position);
      } else if (guide.alignmentType === 'center') {
        diff = Math.abs(bounds.centerY - guide.position);
      } else if (guide.alignmentType === 'bottom') {
        diff = Math.abs(bounds.bottom - guide.position);
      }
      
      if (diff < minDiff) {
        minDiff = diff;
        bestGuide = guide;
      }
    }
    
    if (bestGuide && minDiff < SNAP_THRESHOLD_MM) {
      if (bestGuide.alignmentType === 'top') {
        snappedY = newY + (bestGuide.position - bounds.top);
      } else if (bestGuide.alignmentType === 'center') {
        snappedY = newY + (bestGuide.position - bounds.centerY);
      } else if (bestGuide.alignmentType === 'bottom') {
        snappedY = newY + (bestGuide.position - bounds.bottom);
      }
    }
  }
  
  return { x: snappedX, y: snappedY };
}

/**
 * Calculate distances from element edges to page edges
 */
export function calculateElementDistances(
  element: TitlePageElement,
  ctx: CanvasRenderingContext2D
): { top: number; bottom: number; left: number; right: number } {
  const bounds = getElementBounds(element, ctx);
  
  return {
    top: bounds.top,
    bottom: PAGE_HEIGHT_MM - bounds.bottom,
    left: bounds.left,
    right: PAGE_WIDTH_MM - bounds.right,
  };
}

