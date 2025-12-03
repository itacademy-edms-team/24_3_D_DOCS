import type { TitlePageElement } from '../../../../../../shared/src/types';
import { getTextStyle, escapeHtml } from '../utils/styleBuilder';

export function renderTextElement(
  element: TitlePageElement,
  variables: Record<string, string>
): string {
  const content = element.content || '';
  return renderTextOrVariable(element, content, variables);
}

export function renderVariableElement(
  element: TitlePageElement,
  variables: Record<string, string>
): string {
  const content = variables[element.variableKey || ''] || '';
  return renderTextOrVariable(element, content, variables);
}

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
  
  const MM_TO_PX = 3.7795275591;
  const PX_TO_MM = 1 / MM_TO_PX;
  
  const linesHtml = lines.map((line, i) => {
    const lineTop = element.y + (i * lineHeightPt * PX_TO_MM);
    const lineStyle = `left: ${element.x}mm; top: ${lineTop}mm;`;
    const textStyle = getTextStyle(element, false);
    const dataAttrs = textAlign !== 'left' 
      ? `data-text-align="${textAlign}" data-original-x="${element.x}"`
      : '';
    
    return `<div class="element text-line" style="${lineStyle}${textStyle}" ${dataAttrs}>${escapeHtml(line)}</div>`;
  }).join('\n');
  
  return linesHtml;
}

