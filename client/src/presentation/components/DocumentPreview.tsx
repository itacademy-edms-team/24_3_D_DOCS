import { useMemo, useRef, useEffect, useState } from 'react';
import type { Profile, TitlePage } from '../../../../shared/src/types';
import { DEFAULT_PAGE_SETTINGS } from '../../../../shared/src/types';
import { PAGE_SIZES, MM_TO_PX } from '../../../../shared/src/constants';
import { titlePageApi } from '../../infrastructure/api/titlePageApi';
import { getDefaultTitlePageId } from '../../utils/storageUtils';
import { renderTitlePageToHtml } from '../../application/services/titlePageRenderer';

interface DocumentPreviewProps {
  html: string;
  profile: Profile | null;
  onClick?: (e: React.MouseEvent) => void;
  documentVariables?: Record<string, string>;
}

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
 * Render page number based on settings
 */
function renderPageNumber(
  pageNumber: number,
  totalPages: number,
  pageNumbers: Profile['page']['pageNumbers'],
  position: 'top' | 'bottom'
): string {
  if (!pageNumbers?.enabled) {
    return '';
  }

  const format = pageNumbers.format
    .replace('{n}', String(pageNumber))
    .replace('{total}', String(totalPages));

  const fontSize = pageNumbers.fontSize || 12;
  const fontFamily = pageNumbers.fontFamily ? `font-family: ${pageNumbers.fontFamily};` : '';
  const fontStyle = pageNumbers.fontStyle ? `font-style: ${pageNumbers.fontStyle};` : '';
  const textAlign = `text-align: ${pageNumbers.align};`;
  
  // Add padding-bottom for footer to raise page number slightly
  const paddingBottom = position === 'bottom' ? 'padding-bottom: 6px;' : '';

  return `
    <div style="${fontFamily} ${fontStyle} font-size: ${fontSize}pt; ${textAlign} width: 100%; color: #000; ${paddingBottom}">
      ${format}
    </div>
  `;
}

/**
 * Split HTML content into pages based on content height
 * Elements are never split - they are moved to next page if they don't fit
 */
function splitIntoPages(
  html: string,
  pageContentHeight: number,
  contentWidth: number
): string[] {
  if (!html.trim()) {
    return [html];
  }

  // Create a temporary container WITHOUT padding to measure elements accurately
  const tempContainer = document.createElement('div');
  tempContainer.style.position = 'absolute';
  tempContainer.style.visibility = 'hidden';
  tempContainer.style.width = `${contentWidth}px`;
  // NO padding here - we measure elements without padding
  tempContainer.style.fontFamily = "'Times New Roman', Times, serif";
  tempContainer.style.fontSize = '14pt';
  tempContainer.style.lineHeight = '1.5';
  tempContainer.style.color = '#1a1a1a';
  document.body.appendChild(tempContainer);

  // First, measure total content height WITHOUT padding to check if splitting is needed
  // pageContentHeight is already calculated without padding, so we measure without padding too
  const totalContainer = document.createElement('div');
  totalContainer.style.position = 'absolute';
  totalContainer.style.visibility = 'hidden';
  totalContainer.style.width = `${contentWidth}px`;
  // NO padding here - we measure content height without padding to match pageContentHeight
  totalContainer.style.fontFamily = "'Times New Roman', Times, serif";
  totalContainer.style.fontSize = '14pt';
  totalContainer.style.lineHeight = '1.5';
  totalContainer.style.color = '#1a1a1a';
  totalContainer.innerHTML = html;
  document.body.appendChild(totalContainer);

  const totalHeight = totalContainer.scrollHeight;
  document.body.removeChild(totalContainer);
  
  // If content fits in one page, return as is
  if (totalHeight <= pageContentHeight) {
    document.body.removeChild(tempContainer);
    return [html];
  }

  // Parse HTML to get individual elements
  const parser = new DOMParser();
  const doc = parser.parseFromString(html, 'text/html');
  const body = doc.body;
  
  const pages: string[] = [];
  const elements = Array.from(body.children);
  let currentPageElements: Element[] = [];

  // Elements that should not be split (block-level elements)
  const blockElements = new Set(['P', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'UL', 'OL', 'LI', 'TABLE', 'TR', 'THEAD', 'TBODY', 'DIV']);

  for (const element of elements) {
    // Measure this element WITHOUT padding
    const elementClone = element.cloneNode(true) as HTMLElement;
    tempContainer.innerHTML = '';
    tempContainer.appendChild(elementClone);
    const elementHeight = tempContainer.scrollHeight;

    // Check if element should not be split
    const isBlockElement = blockElements.has(element.tagName);
    
    // If element alone exceeds page height and it's a block element, put it on its own page
    if (elementHeight > pageContentHeight && isBlockElement) {
      // Save current page if it has content
      if (currentPageElements.length > 0) {
        const pageDiv = document.createElement('div');
        currentPageElements.forEach(el => pageDiv.appendChild(el.cloneNode(true)));
        pages.push(pageDiv.innerHTML);
        currentPageElements = [];
      }

      // Put large element on its own page
      const pageDiv = document.createElement('div');
      pageDiv.appendChild(element.cloneNode(true));
      pages.push(pageDiv.innerHTML);
      continue;
    }

    // Measure combined height of current page + new element
    // This gives us more accurate measurement than just summing heights
    if (currentPageElements.length > 0) {
      const testContainer = document.createElement('div');
      testContainer.style.position = 'absolute';
      testContainer.style.visibility = 'hidden';
      testContainer.style.width = `${contentWidth}px`;
      testContainer.style.fontFamily = "'Times New Roman', Times, serif";
      testContainer.style.fontSize = '14pt';
      testContainer.style.lineHeight = '1.5';
      testContainer.style.color = '#1a1a1a';
      
      const testPageDiv = document.createElement('div');
      currentPageElements.forEach(el => testPageDiv.appendChild(el.cloneNode(true)));
      testPageDiv.appendChild(element.cloneNode(true));
      testContainer.appendChild(testPageDiv);
      document.body.appendChild(testContainer);
      
      const combinedHeight = testContainer.scrollHeight;
      document.body.removeChild(testContainer);
      
      // Check if combined height exceeds page height
      if (combinedHeight > pageContentHeight) {
        // Start new page
        const pageDiv = document.createElement('div');
        currentPageElements.forEach(el => pageDiv.appendChild(el.cloneNode(true)));
        pages.push(pageDiv.innerHTML);
        currentPageElements = [element];
      } else {
        // Add to current page
        currentPageElements.push(element);
      }
    } else {
      // First element on page
      currentPageElements.push(element);
    }
  }

  // Add remaining elements as last page
  if (currentPageElements.length > 0) {
    const pageDiv = document.createElement('div');
    currentPageElements.forEach(el => pageDiv.appendChild(el.cloneNode(true)));
    pages.push(pageDiv.innerHTML);
  }

  document.body.removeChild(tempContainer);
  return pages.length > 0 ? pages : [html];
}

/**
 * Document Preview Component
 * 
 * Renders document with separate pages, page breaks, and gaps between pages.
 */
export function DocumentPreview({ html, profile, onClick, documentVariables }: DocumentPreviewProps) {
  const dimensions = useMemo(() => calculateDimensions(profile), [profile]);
  const contentRef = useRef<HTMLDivElement>(null);
  const [pages, setPages] = useState<string[]>([html]);
  const [titlePage, setTitlePage] = useState<TitlePage | null>(null);
  const [zoom, setZoom] = useState(100);
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 });
  const [scrollPosition, setScrollPosition] = useState({ left: 0, top: 0 });
  const [hasMoved, setHasMoved] = useState(false);
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  const settings = profile?.page || DEFAULT_PAGE_SETTINGS;
  const pageNumbers = settings.pageNumbers;
  const hasPageNumbers = pageNumbers?.enabled || false;
  
  // Measure actual header/footer content heights (without margins)
  const headerContentHeight = useMemo(() => {
    if (!pageNumbers?.enabled || pageNumbers.position !== 'top') {
      return 0;
    }
    const tempDiv = document.createElement('div');
    tempDiv.style.position = 'absolute';
    tempDiv.style.visibility = 'hidden';
    tempDiv.style.width = `${dimensions.pageWidth}px`;
    tempDiv.innerHTML = renderPageNumber(1, 1, pageNumbers, 'top');
    document.body.appendChild(tempDiv);
    const height = tempDiv.scrollHeight;
    document.body.removeChild(tempDiv);
    return height;
  }, [pageNumbers, dimensions]);
  
  // Calculate available content height
  // Content area = page height - margins - header content height
  // Footer is absolutely positioned and doesn't take space in the flow
  const contentHeight = dimensions.pageHeight 
    - dimensions.marginTop 
    - dimensions.marginBottom 
    - headerContentHeight;
  const contentWidth = dimensions.pageWidth - dimensions.marginLeft - dimensions.marginRight;

  // Load title page
  useEffect(() => {
    const titlePageId = getDefaultTitlePageId();
    if (titlePageId) {
      titlePageApi.getById(titlePageId)
        .then(setTitlePage)
        .catch((error) => {
          console.error('Failed to load title page:', error);
          setTitlePage(null);
        });
    } else {
      setTitlePage(null);
    }
  }, []);

  useEffect(() => {
    if (!html) {
      setPages(['']);
      return;
    }

    // Split into pages
    const splitPages = splitIntoPages(html, contentHeight, contentWidth);
    setPages(splitPages);
  }, [html, contentHeight, contentWidth]);

  const totalPages = pages.length;
  
  // Merge documentVariables with titlePage.variables (documentVariables have priority)
  const mergedVariables = useMemo(() => {
    if (!titlePage) return documentVariables || {};
    return {
      ...titlePage.variables,
      ...(documentVariables || {}),
    };
  }, [titlePage, documentVariables]);
  
  const titlePageHtml = titlePage ? renderTitlePageToHtml(titlePage, mergedVariables) : null;

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        background: '#525659',
      }}
    >
      {/* Zoom controls */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '0.5rem',
          padding: '0.5rem 1rem',
          background: '#3d4043',
          borderBottom: '1px solid #525659',
        }}
      >
        <button
          onClick={() => setZoom(Math.max(50, zoom - 10))}
          style={{
            padding: '0.25rem 0.5rem',
            background: '#525659',
            color: 'white',
            border: '1px solid #6d7073',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '0.875rem',
          }}
          title="Уменьшить масштаб"
        >
          −
        </button>
        <input
          type="range"
          min="50"
          max="200"
          value={zoom}
          onChange={(e) => setZoom(parseInt(e.target.value))}
          style={{
            flex: 1,
            maxWidth: '200px',
          }}
        />
        <span style={{ color: 'white', fontSize: '0.875rem', minWidth: '50px', textAlign: 'center' }}>
          {zoom}%
        </span>
        <button
          onClick={() => setZoom(Math.min(200, zoom + 10))}
          style={{
            padding: '0.25rem 0.5rem',
            background: '#525659',
            color: 'white',
            border: '1px solid #6d7073',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '0.875rem',
          }}
          title="Увеличить масштаб"
        >
          +
        </button>
        <button
          onClick={() => setZoom(100)}
          style={{
            padding: '0.25rem 0.5rem',
            background: '#525659',
            color: 'white',
            border: '1px solid #6d7073',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '0.875rem',
            marginLeft: '0.5rem',
          }}
          title="Сбросить масштаб"
        >
          100%
        </button>
      </div>

      {/* Preview content with zoom and drag to pan */}
      <div
        ref={scrollContainerRef}
        className="document-preview-container"
        style={{
          background: '#525659',
          padding: '20px',
          flex: 1,
          overflow: 'auto',
          position: 'relative',
          cursor: isDragging && hasMoved ? 'grabbing' : 'default',
        }}
        onMouseDown={(e) => {
          if (e.button === 0) { // Left mouse button
            setIsDragging(true);
            setHasMoved(false);
            setDragStart({ x: e.clientX, y: e.clientY });
            if (scrollContainerRef.current) {
              setScrollPosition({
                left: scrollContainerRef.current.scrollLeft,
                top: scrollContainerRef.current.scrollTop,
              });
            }
          }
        }}
        onMouseMove={(e) => {
          if (isDragging) {
            const deltaX = Math.abs(dragStart.x - e.clientX);
            const deltaY = Math.abs(dragStart.y - e.clientY);
            
            // Only start dragging if mouse moved more than 5px
            if (deltaX > 5 || deltaY > 5) {
              setHasMoved(true);
              if (scrollContainerRef.current) {
                e.preventDefault();
                const scrollDeltaX = dragStart.x - e.clientX;
                const scrollDeltaY = dragStart.y - e.clientY;
                scrollContainerRef.current.scrollLeft = scrollPosition.left + scrollDeltaX;
                scrollContainerRef.current.scrollTop = scrollPosition.top + scrollDeltaY;
              }
            }
          }
        }}
        onMouseUp={() => {
          if (!hasMoved && scrollContainerRef.current) {
            // If didn't move, allow normal click behavior
          }
          setIsDragging(false);
          setHasMoved(false);
        }}
        onMouseLeave={() => {
          setIsDragging(false);
          setHasMoved(false);
        }}
      >
        <div
          style={{
            transform: `scale(${zoom / 100})`,
            transformOrigin: 'top left',
            width: `${100 / (zoom / 100)}%`,
            minHeight: '100%',
          }}
        >
      <div
        ref={contentRef}
        style={{ display: 'none' }}
        dangerouslySetInnerHTML={{ __html: html }}
      />
      
      {/* Title Page */}
      {titlePageHtml && (
        <div
          className="document-page"
          style={{
            width: `${210 * MM_TO_PX}px`, // A4 width in pixels
            height: `${297 * MM_TO_PX}px`, // A4 height in pixels
            margin: '0 auto',
            background: 'white',
            boxShadow: '0 2px 8px rgba(0,0,0,0.3)',
            pageBreakAfter: 'always',
            pageBreakInside: 'avoid',
            position: 'relative',
            overflow: 'hidden',
          }}
          onClick={onClick}
        >
          <div
            style={{
              width: '210mm',
              height: '297mm',
              position: 'relative',
              fontFamily: "'Times New Roman', Times, serif",
              margin: 0,
              padding: 0,
            }}
            dangerouslySetInnerHTML={{ __html: titlePageHtml }}
          />
        </div>
      )}

      {/* Document Pages */}
      {pages.map((pageHtml, index) => {
        const pageNumber = titlePageHtml ? index + 2 : index + 1; // Adjust page numbers if title page exists
        const adjustedTotalPages = titlePageHtml ? totalPages + 1 : totalPages;
        const headerContent = hasPageNumbers && pageNumbers?.position === 'top';
        const footerContent = hasPageNumbers && pageNumbers?.position === 'bottom';
        
        return (
          <div
            key={index}
            className="document-page"
            style={{
              width: dimensions.pageWidth,
              height: dimensions.pageHeight,
              margin: (index > 0 || titlePageHtml) ? '20px auto 0' : '0 auto',
              background: 'white',
              boxShadow: '0 2px 8px rgba(0,0,0,0.3)',
              pageBreakAfter: 'always',
              pageBreakInside: 'avoid',
              position: 'relative',
            }}
            onClick={onClick}
          >
            {/* Header with page number - absolutely positioned at top */}
            {headerContent && (
              <div
                style={{
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  right: 0,
                  padding: `${dimensions.marginTop}px 0 0 0`,
                  width: '100%',
                  zIndex: 1,
                }}
                dangerouslySetInnerHTML={{ __html: renderPageNumber(pageNumber, adjustedTotalPages, pageNumbers, 'top') }}
              />
            )}

            {/* Content area */}
            <div
              className="document-preview"
              style={{
                width: '100%',
                height: '100%',
                padding: headerContent && footerContent
                  ? `${dimensions.marginTop + headerContentHeight}px ${dimensions.marginRight}px ${dimensions.marginBottom}px ${dimensions.marginLeft}px`
                  : headerContent
                  ? `${dimensions.marginTop + headerContentHeight}px ${dimensions.marginRight}px ${dimensions.marginBottom}px ${dimensions.marginLeft}px`
                  : footerContent
                  ? `${dimensions.marginTop}px ${dimensions.marginRight}px ${dimensions.marginBottom}px ${dimensions.marginLeft}px`
                  : `${dimensions.marginTop}px ${dimensions.marginRight}px ${dimensions.marginBottom}px ${dimensions.marginLeft}px`,
                fontFamily: "'Times New Roman', Times, serif",
                fontSize: '14pt',
                lineHeight: 1.5,
                color: '#1a1a1a',
                overflow: 'auto',
                boxSizing: 'border-box',
              }}
            >
              <div 
                dangerouslySetInnerHTML={{ __html: pageHtml }}
                style={{
                  pageBreakInside: 'avoid',
                }}
              />
            </div>

            {/* Footer with page number - absolutely positioned at bottom with strict marginBottom */}
            {footerContent && (
              <div
                style={{
                  position: 'absolute',
                  bottom: 0,
                  left: 0,
                  right: 0,
                  height: `${dimensions.marginBottom}px`,
                  width: '100%',
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'flex-end',
                  zIndex: 1,
                }}
                dangerouslySetInnerHTML={{ __html: renderPageNumber(pageNumber, adjustedTotalPages, pageNumbers, 'bottom') }}
              />
            )}
          </div>
        );
      })}
        </div>
      </div>
    </div>
  );
}
