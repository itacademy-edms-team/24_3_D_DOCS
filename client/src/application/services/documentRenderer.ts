import { marked } from 'marked';
import katex from 'katex';
import type { EntityStyle, EntityType, Profile } from '../../../../shared/src/types';
import { getFinalStyle, styleToCSS, generateElementId } from './styleEngine';

interface RenderOptions {
  markdown: string;
  profile: Profile | null;
  overrides: Record<string, EntityStyle>;
  selectable?: boolean;
}

/**
 * Render LaTeX formulas
 */
function renderLatex(text: string): string {
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
function applyStyles(
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

  // 4. Process paragraphs
  doc.querySelectorAll('p').forEach((el) => {
    if (el.closest('figure')) return;

    const text = el.textContent || '';
    if (text.match(/^\[(TABLE|FORMULA)-CAPTION:/)) return;

    const content = text;
    const elId = generateElementId('p', content, usedIds);
    applyStyles(el, 'paragraph', elId, profile, overrides, selectable);
  });

  // 5. Process headings
  doc.querySelectorAll('h1, h2, h3, h4, h5, h6').forEach((el) => {
    const content = el.textContent || '';
    const level = parseInt(el.tagName[1]);
    const elId = generateElementId(`h${level}`, content, usedIds);
    applyStyles(el, 'heading', elId, profile, overrides, selectable);
  });

  // 6. Process images
  doc.querySelectorAll('img').forEach((img) => {
    const src = img.getAttribute('src') || '';
    const alt = img.getAttribute('alt') || '';
    const elId = generateElementId('img', src + alt, usedIds);

    const imageStyle = getFinalStyle('image', elId, profile, overrides);

    const imgWrapper = doc.createElement('div');
    imgWrapper.id = elId;
    imgWrapper.setAttribute('data-type', 'image');
    imgWrapper.setAttribute('style', styleToCSS(imageStyle));
    if (selectable) imgWrapper.classList.add('element-selectable');

    const imgStyle = `max-width: ${imageStyle.maxWidth || 100}%; height: auto; display: block;`;
    let alignStyle = '';
    if (imageStyle.textAlign === 'center') {
      alignStyle = ' margin: 0 auto;';
    } else if (imageStyle.textAlign === 'right') {
      alignStyle = ' margin-left: auto;';
    } else if (imageStyle.textAlign === 'left') {
      alignStyle = ' margin-left: 0;';
    }
    img.setAttribute('style', imgStyle + alignStyle);

    const parent = img.parentNode;
    if (parent) {
      parent.insertBefore(imgWrapper, img);
      imgWrapper.appendChild(img);

      if (alt) {
        const captionId = generateElementId('caption', alt, usedIds);
        const captionStyle = getFinalStyle('image-caption', captionId, profile, overrides);

        const caption = doc.createElement('div');
        caption.id = captionId;
        caption.setAttribute('data-type', 'image-caption');
        caption.setAttribute('style', styleToCSS(captionStyle));
        caption.textContent = alt;
        if (selectable) caption.classList.add('element-selectable');

        parent.insertBefore(caption, imgWrapper.nextSibling);
      }
    }
  });

  // 7. Process unordered lists
  doc.querySelectorAll('ul').forEach((el) => {
    const content = el.textContent || '';
    const elId = generateElementId('ul', content.slice(0, 50), usedIds);
    const style = getFinalStyle('unordered-list', elId, profile, overrides);

    const listStyle: EntityStyle = { ...style, textIndent: undefined };

    el.id = elId;
    el.setAttribute('data-type', 'unordered-list');
    el.setAttribute('style', styleToCSS(listStyle));
    if (selectable) el.classList.add('element-selectable');

    if (style.textIndent !== undefined && style.textIndent > 0) {
      const textIndentPt = style.textIndent * 28.35;
      const listItems = el.querySelectorAll('li');
      listItems.forEach((li, index) => {
        if (index === 0) {
          const currentStyle = (li as HTMLElement).getAttribute('style') || '';
          (li as HTMLElement).setAttribute('style', `${currentStyle}; margin-left: ${textIndentPt}pt;`.trim());
        } else {
          const currentStyle = (li as HTMLElement).getAttribute('style') || '';
          (li as HTMLElement).setAttribute('style', `${currentStyle}; margin-left: 0;`.trim());
        }
      });
    }
  });

  // 8. Process ordered lists
  doc.querySelectorAll('ol').forEach((el) => {
    const content = el.textContent || '';
    const elId = generateElementId('ol', content.slice(0, 50), usedIds);
    const style = getFinalStyle('ordered-list', elId, profile, overrides);

    const listStyle: EntityStyle = { ...style, textIndent: undefined };

    el.id = elId;
    el.setAttribute('data-type', 'ordered-list');
    el.setAttribute('style', styleToCSS(listStyle));
    if (selectable) el.classList.add('element-selectable');

    if (style.textIndent !== undefined && style.textIndent > 0) {
      const textIndentPt = style.textIndent * 28.35;
      const listItems = el.querySelectorAll('li');
      listItems.forEach((li, index) => {
        if (index === 0) {
          const currentStyle = (li as HTMLElement).getAttribute('style') || '';
          (li as HTMLElement).setAttribute('style', `${currentStyle}; margin-left: ${textIndentPt}pt;`.trim());
        } else {
          const currentStyle = (li as HTMLElement).getAttribute('style') || '';
          (li as HTMLElement).setAttribute('style', `${currentStyle}; margin-left: 0;`.trim());
        }
      });
    }
  });

  // 9. Process tables
  doc.querySelectorAll('table').forEach((table) => {
    const content = table.textContent || '';
    const elId = generateElementId('table', content.slice(0, 50), usedIds);
    const style = getFinalStyle('table', elId, profile, overrides);

    const borderStyle = style.borderStyle || 'solid';
    const borderWidth = style.borderWidth || 1;
    const borderColor = style.borderColor || '#333';
    const cellStyle = `border: ${borderWidth}px ${borderStyle} ${borderColor}; padding: 8px;`;

    table.querySelectorAll('th, td').forEach((cell) => {
      cell.setAttribute('style', cellStyle);
    });

    let captionText: string | null = null;
    const nextSibling = table.nextElementSibling;
    if (nextSibling && nextSibling.tagName === 'P') {
      const pText = nextSibling.textContent || '';
      const match = pText.match(/^\[TABLE-CAPTION:\s*(.+)\]$/);
      if (match) {
        captionText = match[1].trim();
        nextSibling.remove();
      }
    }

    const tableWrapper = doc.createElement('div');
    tableWrapper.id = elId;
    tableWrapper.setAttribute('data-type', 'table');
    tableWrapper.setAttribute('style', styleToCSS(style));
    if (selectable) tableWrapper.classList.add('element-selectable');

    const parent = table.parentNode;
    if (parent) {
      parent.insertBefore(tableWrapper, table);
      tableWrapper.appendChild(table);

      const tableFinalStyle: EntityStyle = {
        ...style,
        marginTop: undefined,
        marginBottom: undefined,
        marginLeft: undefined,
        marginRight: undefined,
      };
      table.setAttribute('style', styleToCSS(tableFinalStyle) + '; border-collapse: collapse; width: 100%;');

      if (captionText) {
        const captionId = generateElementId('table-caption', captionText, usedIds);
        const captionStyle = getFinalStyle('table-caption', captionId, profile, overrides);

        const caption = doc.createElement('div');
        caption.id = captionId;
        caption.setAttribute('data-type', 'table-caption');
        caption.setAttribute('style', styleToCSS(captionStyle));
        caption.textContent = captionText;
        if (selectable) caption.classList.add('element-selectable');

        parent.insertBefore(caption, tableWrapper.nextSibling);
      }
    }
  });

  // 10. Process formulas
  doc.querySelectorAll('.formula-block').forEach((formulaBlock) => {
    const content = formulaBlock.textContent || '';
    const elId = generateElementId('formula', content.slice(0, 50), usedIds);
    const style = getFinalStyle('formula', elId, profile, overrides);

    let captionText: string | null = null;
    const nextSibling = formulaBlock.nextElementSibling;
    if (nextSibling && nextSibling.tagName === 'P') {
      const pText = nextSibling.textContent || '';
      const match = pText.match(/^\[FORMULA-CAPTION:\s*(.+)\]$/);
      if (match) {
        captionText = match[1].trim();
        nextSibling.remove();
      }
    }

    const formulaWrapper = doc.createElement('div');
    formulaWrapper.id = elId;
    formulaWrapper.setAttribute('data-type', 'formula');
    formulaWrapper.setAttribute('style', styleToCSS(style));
    if (selectable) formulaWrapper.classList.add('element-selectable');

    const parent = formulaBlock.parentNode;
    if (parent) {
      parent.insertBefore(formulaWrapper, formulaBlock);
      formulaWrapper.appendChild(formulaBlock);

      const formulaFinalStyle: EntityStyle = {
        ...style,
        marginTop: undefined,
        marginBottom: undefined,
        marginLeft: undefined,
        marginRight: undefined,
      };
      formulaBlock.setAttribute('style', styleToCSS(formulaFinalStyle));
      if (selectable) (formulaBlock as HTMLElement).classList.add('element-selectable');

      if (captionText) {
        const captionId = generateElementId('formula-caption', captionText, usedIds);
        const captionStyle = getFinalStyle('formula-caption', captionId, profile, overrides);

        const caption = doc.createElement('div');
        caption.id = captionId;
        caption.setAttribute('data-type', 'formula-caption');
        caption.setAttribute('style', styleToCSS(captionStyle));
        caption.textContent = captionText;
        if (selectable) caption.classList.add('element-selectable');

        parent.insertBefore(caption, formulaWrapper.nextSibling);
      }
    }
  });

  return doc.body.innerHTML;
}

