import type { TitlePageElement, TitlePage } from '../../../../shared/src/types';

/**
 * Escape HTML special characters
 */
function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

/**
 * Get CSS style for text elements
 */
function getTextStyle(element: TitlePageElement, includeLineHeight: boolean = true): string {
  const styles: string[] = [];

  // Always set color to black
  styles.push('color: #000');

  if (element.fontSize) {
    styles.push(`font-size: ${element.fontSize}pt`);
  }
  if (element.fontFamily) {
    styles.push(`font-family: ${element.fontFamily}`);
  }
  if (element.fontWeight) {
    styles.push(`font-weight: ${element.fontWeight}`);
  }
  if (element.fontStyle) {
    styles.push(`font-style: ${element.fontStyle}`);
  }
  if (includeLineHeight && element.lineHeight) {
    styles.push(`line-height: ${element.lineHeight}`);
  }

  return styles.length > 0 ? ` ${styles.join('; ')};` : '';
}

/**
 * Measure text width using canvas (similar to server-side approach)
 */
function measureTextWidth(
  text: string,
  fontSize: number,
  fontFamily: string,
  fontWeight: string,
  fontStyle: string
): number {
  const canvas = document.createElement('canvas');
  const ctx = canvas.getContext('2d');
  if (!ctx) return 0;
  
  ctx.font = `${fontStyle} ${fontWeight} ${fontSize}pt ${fontFamily}`;
  const metrics = ctx.measureText(text);
  return metrics.width;
}

/**
 * Render text or variable element to HTML
 */
function renderTextOrVariable(
  element: TitlePageElement,
  content: string,
  variables: Record<string, string>
): string {
  const lines = content.split('\n');
  const fontSize = element.fontSize || 14;
  const lineHeightMultiplier = element.lineHeight || 1.2;
  const lineHeightPt = fontSize * lineHeightMultiplier;
  const textAlign = element.textAlign || 'left';
  const fontFamily = element.fontFamily || 'Times New Roman';
  const fontWeight = element.fontWeight || 'normal';
  const fontStyle = element.fontStyle || 'normal';
  
  const MM_TO_PX = 3.7795275591;
  const PX_TO_MM = 1 / MM_TO_PX;
  
  const linesHtml = lines.map((line, i) => {
    const lineTop = element.y + (i * lineHeightPt * PX_TO_MM);
    const textStyle = getTextStyle(element, false);
    
    // Calculate left position based on alignment
    let leftPos = element.x;
    if (textAlign === 'center' || textAlign === 'right') {
      const lineWidth = measureTextWidth(line, fontSize, fontFamily, fontWeight, fontStyle);
      const lineWidthMm = lineWidth / MM_TO_PX;
      
      if (textAlign === 'center') {
        leftPos = element.x - lineWidthMm / 2;
      } else { // right
        leftPos = element.x - lineWidthMm;
      }
    }
    
    const lineStyle = `position: absolute; left: ${leftPos}mm; top: ${lineTop}mm; text-align: ${textAlign};`;
    
    return `<div style="${lineStyle}${textStyle} white-space: nowrap;">${escapeHtml(line)}</div>`;
  }).join('\n');
  
  return linesHtml;
}

/**
 * Render text element to HTML
 */
function renderTextElement(
  element: TitlePageElement,
  variables: Record<string, string>
): string {
  const content = element.content || '';
  return renderTextOrVariable(element, content, variables);
}

/**
 * Render variable element to HTML
 */
function renderVariableElement(
  element: TitlePageElement,
  variables: Record<string, string>
): string {
  const content = variables[element.variableKey || ''] || '';
  return renderTextOrVariable(element, content, variables);
}

/**
 * Render line element to HTML
 */
function renderLineElement(element: TitlePageElement): string {
  const positionStyle = `position: absolute; left: ${element.x}mm; top: ${element.y}mm;`;
  const length = element.length || 100;
  const thickness = element.thickness || 1;
  return `<div style="${positionStyle} width: ${length}mm; height: ${thickness}mm; background-color: #000;"></div>`;
}

/**
 * Render single element to HTML
 */
function renderElement(element: TitlePageElement, variables: Record<string, string>): string {
  if (element.type === 'line') {
    return renderLineElement(element);
  } else if (element.type === 'text') {
    return renderTextElement(element, variables);
  } else if (element.type === 'variable') {
    return renderVariableElement(element, variables);
  }
  return '';
}

/**
 * Render title page to HTML
 * Returns HTML string that can be inserted into a page container
 * @param titlePage - The title page to render
 * @param documentVariables - Optional variables from document frontmatter (takes priority over titlePage.variables)
 */
export function renderTitlePageToHtml(
  titlePage: TitlePage,
  documentVariables?: Record<string, string>
): string {
  // Merge variables: documentVariables have priority over titlePage.variables
  const mergedVariables = {
    ...titlePage.variables,
    ...(documentVariables || {}),
  };
  
  const elementsHtml = titlePage.elements.map(element => {
    return renderElement(element, mergedVariables);
  }).join('\n');
  
  return elementsHtml;
}

