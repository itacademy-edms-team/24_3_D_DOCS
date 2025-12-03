import type { TitlePageElement } from '../../../../../../shared/src/types';

/**
 * Get CSS style for text elements
 */
export function getTextStyle(element: TitlePageElement, includeLineHeight: boolean = true): string {
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
 * Escape HTML special characters
 */
export function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

