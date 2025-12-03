import katex from 'katex';
import type { EntityStyle, EntityType, Profile } from '../../../../shared/src/types';
import { getFinalStyle, styleToCSS } from '../../../../shared/src/utils';

/**
 * Render LaTeX formulas
 */
export function renderLatex(text: string): string {
  // Block formulas: $$...$$
  text = text.replace(/\$\$([^$]+)\$\$/g, (_, formula) => {
    try {
      const html = katex.renderToString(formula.trim(), {
        displayMode: true,
        throwOnError: false,
      });
      return `<div class="formula-block">${html}</div>`;
    } catch {
      return `<div class="formula-block formula-error">${formula}</div>`;
    }
  });

  // Inline formulas: $...$
  text = text.replace(/\$([^$]+)\$/g, (_, formula) => {
    try {
      return katex.renderToString(formula.trim(), {
        displayMode: false,
        throwOnError: false,
      });
    } catch {
      return `<span class="formula-error">${formula}</span>`;
    }
  });

  return text;
}

/**
 * Apply styles to element
 */
export function applyStyles(
  element: Element,
  entityType: EntityType,
  elementId: string,
  profile: Profile | null,
  overrides: Record<string, EntityStyle>,
  selectable: boolean
): void {
  const style = getFinalStyle(entityType, elementId, profile, overrides);

  element.id = elementId;
  element.setAttribute('data-type', entityType);
  element.setAttribute('style', styleToCSS(style));

  if (selectable) {
    element.classList.add('element-selectable');
  }
}

