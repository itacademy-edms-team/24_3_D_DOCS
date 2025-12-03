/**
 * Conversion constants
 */
export const MM_TO_PX = 3.7795275591; // 1mm ≈ 3.78 pixels at 96dpi
export const PT_TO_MM = 0.352778; // 1pt ≈ 0.352778mm
export const PX_TO_MM = 1 / MM_TO_PX; // ≈ 0.264583 mm

/**
 * Page sizes in mm
 */
export const PAGE_SIZES: Record<string, { width: number; height: number }> = {
  A4: { width: 210, height: 297 },
  A5: { width: 148, height: 210 },
  Letter: { width: 216, height: 279 },
};

/**
 * A4 dimensions in mm (for title pages)
 */
export const PAGE_WIDTH_MM = 210;
export const PAGE_HEIGHT_MM = 297;

