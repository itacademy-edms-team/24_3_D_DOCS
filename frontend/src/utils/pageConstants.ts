import type { ProfileData } from '@/entities/profile/types';

/**
 * Millimeters to pixels conversion factor (96 DPI)
 * 1 inch = 25.4mm, 96 pixels per inch
 * MM_TO_PX = 96 / 25.4 ≈ 3.7795275591
 */
export const MM_TO_PX = 3.7795275591;

/**
 * Page sizes in millimeters (width x height)
 */
export const PAGE_SIZES = {
	A4: { width: 210, height: 297 },
	A5: { width: 148, height: 210 },
} as const;

type PageSizeName = keyof typeof PAGE_SIZES;

/**
 * Page dimensions in pixels
 */
export interface PageDimensions {
	pageWidth: number;
	pageHeight: number;
	marginTop: number;
	marginRight: number;
	marginBottom: number;
	marginLeft: number;
}

/**
 * Calculate page dimensions based on profile settings
 * Pure function - no side effects
 */
export function calculatePageDimensions(
	profile: ProfileData | null
): PageDimensions {
	const defaultSettings = {
		size: 'A4' as PageSizeName,
		orientation: 'portrait' as const,
		margins: {
			top: 20,
			right: 20,
			bottom: 20,
			left: 20,
		},
	};

	const settings = profile?.pageSettings || defaultSettings;
	const pageSize = PAGE_SIZES[settings.size as PageSizeName] || PAGE_SIZES.A4;
	const isLandscape = settings.orientation === 'landscape';

	const pageWidth = (isLandscape ? pageSize.height : pageSize.width) * MM_TO_PX;
	const pageHeight = (isLandscape ? pageSize.width : pageSize.height) * MM_TO_PX;

	return {
		pageWidth,
		pageHeight,
		marginTop: (settings.margins?.top || defaultSettings.margins.top) * MM_TO_PX,
		marginRight:
			(settings.margins?.right || defaultSettings.margins.right) * MM_TO_PX,
		marginBottom:
			(settings.margins?.bottom || defaultSettings.margins.bottom) * MM_TO_PX,
		marginLeft:
			(settings.margins?.left || defaultSettings.margins.left) * MM_TO_PX,
	};
}
