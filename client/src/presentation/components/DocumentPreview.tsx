import { useMemo } from 'react';
import type { Profile } from '../../../../shared/src/types';
import { DEFAULT_PAGE_SETTINGS } from '../../../../shared/src/types';

interface DocumentPreviewProps {
  html: string;
  profile: Profile | null;
  onClick?: (e: React.MouseEvent) => void;
}

/** Page sizes in mm */
const PAGE_SIZES: Record<string, { width: number; height: number }> = {
  A4: { width: 210, height: 297 },
  A5: { width: 148, height: 210 },
  Letter: { width: 216, height: 279 },
};

/** Conversion factor: 1mm ≈ 3.78 pixels at 96dpi */
const MM_TO_PX = 3.7795275591;

interface PageDimensions {
  pageWidth: number;
  pageHeight: number;
  marginTop: number;
  marginRight: number;
  marginBottom: number;
  marginLeft: number;
}

/**
 * Calculate page dimensions based on profile settings
 */
function calculateDimensions(profile: Profile | null): PageDimensions {
  const settings = profile?.page || DEFAULT_PAGE_SETTINGS;
  const size = PAGE_SIZES[settings.size] || PAGE_SIZES.A4;
  const isLandscape = settings.orientation === 'landscape';

  const pageWidth = (isLandscape ? size.height : size.width) * MM_TO_PX;
  const pageHeight = (isLandscape ? size.width : size.height) * MM_TO_PX;

  return {
    pageWidth,
    pageHeight,
    marginTop: settings.margins.top * MM_TO_PX,
    marginRight: settings.margins.right * MM_TO_PX,
    marginBottom: settings.margins.bottom * MM_TO_PX,
    marginLeft: settings.margins.left * MM_TO_PX,
  };
}

/**
 * Document Preview Component
 * 
 * Renders document with exact page width (A4/A5/Letter).
 * Content flows naturally without artificial page breaks.
 */
export function DocumentPreview({ html, profile, onClick }: DocumentPreviewProps) {
  const dimensions = useMemo(() => calculateDimensions(profile), [profile]);

  return (
    <div
      className="document-preview-container"
      style={{
        background: '#525659',
        padding: '20px',
        minHeight: '100%',
        overflow: 'auto',
      }}
    >
      <div
        className="document-page"
        style={{
          width: dimensions.pageWidth,
          minHeight: dimensions.pageHeight,
          margin: '0 auto',
          background: 'white',
          boxShadow: '0 2px 8px rgba(0,0,0,0.3)',
        }}
        onClick={onClick}
      >
        <div
          className="document-preview"
          style={{
            padding: `${dimensions.marginTop}px ${dimensions.marginRight}px ${dimensions.marginBottom}px ${dimensions.marginLeft}px`,
            fontFamily: "'Times New Roman', Times, serif",
            fontSize: '14pt',
            lineHeight: 1.5,
            color: '#1a1a1a',
          }}
        >
          <div dangerouslySetInnerHTML={{ __html: html }} />
        </div>
      </div>
    </div>
  );
}
