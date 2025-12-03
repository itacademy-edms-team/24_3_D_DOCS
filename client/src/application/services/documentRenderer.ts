import { marked } from 'marked';
import type { EntityStyle, Profile } from '../../../../shared/src/types';
import { renderLatex } from './renderUtils';
import { renderParagraphs } from './renderers/paragraphRenderer';
import { renderHeadings } from './renderers/headingRenderer';
import { renderImages } from './renderers/imageRenderer';
import { renderUnorderedLists, renderOrderedLists } from './renderers/listRenderer';
import { renderTables } from './renderers/tableRenderer';
import { renderFormulas } from './renderers/formulaRenderer';

interface RenderOptions {
  markdown: string;
  profile: Profile | null;
  overrides: Record<string, EntityStyle>;
  selectable?: boolean;
}

/**
 * Render markdown document to HTML with applied styles
 */
export function renderDocument(options: RenderOptions): string {
  const { markdown, profile, overrides, selectable = false } = options;

  if (!markdown.trim()) {
    return '';
  }

  // 1. Process LaTeX formulas
  const contentWithFormulas = renderLatex(markdown);

  // 2. Markdown -> HTML
  const rawHtml = marked.parse(contentWithFormulas, { async: false }) as string;

  // 3. Parse HTML for style application
  const parser = new DOMParser();
  const doc = parser.parseFromString(rawHtml, 'text/html');
  const usedIds = new Set<string>();

  // 4-10. Process all element types using renderers
  renderParagraphs(doc, usedIds, profile, overrides, selectable);
  renderHeadings(doc, usedIds, profile, overrides, selectable);
  renderImages(doc, usedIds, profile, overrides, selectable);
  renderUnorderedLists(doc, usedIds, profile, overrides, selectable);
  renderOrderedLists(doc, usedIds, profile, overrides, selectable);
  renderTables(doc, usedIds, profile, overrides, selectable);
  renderFormulas(doc, usedIds, profile, overrides, selectable);

  return doc.body.innerHTML;
}
