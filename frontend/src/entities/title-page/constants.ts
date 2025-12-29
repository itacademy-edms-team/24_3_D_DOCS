import { MM_TO_PX, PAGE_SIZES } from '@/utils/pageConstants';

/**
 * A4 page dimensions in millimeters
 */
export const A4_WIDTH_MM = PAGE_SIZES.A4.width;
export const A4_HEIGHT_MM = PAGE_SIZES.A4.height;

/**
 * A4 page dimensions in pixels
 */
export const A4_WIDTH_PX = A4_WIDTH_MM * MM_TO_PX;
export const A4_HEIGHT_PX = A4_HEIGHT_MM * MM_TO_PX;

/**
 * Grid size in millimeters
 */
export const GRID_SIZE_MM = 5;

/**
 * Snap threshold in millimeters
 */
export const SNAP_THRESHOLD_MM = 5;

/**
 * Maximum history size for undo/redo
 */
export const MAX_HISTORY_SIZE = 50;

/**
 * Re-export MM_TO_PX for convenience
 */
export { MM_TO_PX };

/**
 * Point to pixel conversion factor (96 DPI)
 * 1 point = 1/72 inch, 96 pixels per inch
 * PT_TO_PX = 96 / 72 ≈ 1.333
 */
export const PT_TO_PX = 96 / 72;

/**
 * Pixel to point conversion factor (96 DPI)
 * PX_TO_PT = 72 / 96 = 0.75
 */
export const PX_TO_PT = 72 / 96;

/**
 * Font families available in the editor
 */
export const FONT_FAMILIES = [
	{ value: 'Arial', label: 'Arial' },
	{ value: 'Times New Roman', label: 'Times New Roman' },
	{ value: 'Courier New', label: 'Courier New' },
	{ value: 'Georgia', label: 'Georgia' },
	{ value: 'Verdana', label: 'Verdana' },
	{ value: 'Helvetica', label: 'Helvetica' },
] as const;

/**
 * Available tools in the editor
 */
export const TOOLS = [
	{ value: 'select', label: 'Выделение', icon: '↖' },
	{ value: 'text', label: 'Текст', icon: 'T' },
	{ value: 'variable', label: 'Переменная', icon: '{ }' },
	{ value: 'line', label: 'Линия', icon: '—' },
] as const;

/**
 * Zoom options
 */
export const ZOOM_OPTIONS = [
	{ value: 50, label: '50%' },
	{ value: 75, label: '75%' },
	{ value: 100, label: '100%' },
	{ value: 150, label: '150%' },
	{ value: 200, label: '200%' },
] as const;

/**
 * Tool type
 */
export type ToolType = typeof TOOLS[number]['value'];

/**
 * Guide type (vertical or horizontal)
 */
export type GuideType = 'vertical' | 'horizontal';

/**
 * Alignment guide
 */
export interface AlignmentGuide {
	type: GuideType;
	position: number;
	alignmentType?: string;
}

/**
 * Active snap guide (simplified version without alignmentType)
 */
export interface ActiveSnapGuide {
	type: GuideType;
	position: number;
}

/**
 * Element distances from page edges
 */
export interface ElementDistances {
	top: number;
	bottom: number;
	left: number;
	right: number;
}

/**
 * Ruler mark
 */
export interface RulerMark {
	value: number;
	position: number;
	isMajor: boolean;
}

/**
 * Mouse position in millimeters
 */
export interface MousePosition {
	x: number;
	y: number;
}

/**
 * Font family option
 */
export type FontFamily = typeof FONT_FAMILIES[number];

/**
 * Zoom option
 */
export type ZoomOption = typeof ZOOM_OPTIONS[number];
