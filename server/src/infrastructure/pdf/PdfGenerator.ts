import puppeteer from 'puppeteer';
import { marked } from 'marked';
import katex from 'katex';
import path from 'path';
import fs from 'fs';
import type { Profile, EntityStyle, EntityType } from '../../../../shared/src/types';
import { DEFAULT_ENTITY_STYLES, DEFAULT_PAGE_SETTINGS } from '../../../../shared/src/types';
import { styleToCSS, generateElementId, getPageDimensions } from '../../../../shared/src/utils';

/**
 * PDF Generator Service
 * Converts Markdown to PDF with profile-based styling
 */
export class PdfGenerator {
  /**
   * Get base style (DEFAULT + Profile)
   */
  private getBaseStyle(entityType: EntityType, profile: Profile | null): EntityStyle {
    const defaultStyle = DEFAULT_ENTITY_STYLES[entityType];
    const profileStyle = profile?.entities?.[entityType] || {};
    return { ...defaultStyle, ...profileStyle };
  }

  /**
   * Get final style (BASE + Override)
   */
  private getFinalStyle(
    entityType: EntityType,
    elementId: string,
    profile: Profile | null,
    overrides: Record<string, EntityStyle>
  ): EntityStyle {
    const baseStyle = this.getBaseStyle(entityType, profile);
    const override = overrides[elementId] || {};
    return { ...baseStyle, ...override };
  }

  /**
   * Render LaTeX formulas
   */
  private renderLatex(text: string): string {
    // Block formulas: $$...$$
    text = text.replace(/\$\$([^$]+)\$\$/g, (_, formula) => {
      try {
        return `<div class="formula-block">${katex.renderToString(formula.trim(), { displayMode: true, throwOnError: false })}</div>`;
      } catch {
        return `<div class="formula-block">${formula}</div>`;
      }
    });

    // Inline formulas: $...$
    text = text.replace(/\$([^$]+)\$/g, (_, formula) => {
      try {
        return katex.renderToString(formula.trim(), { displayMode: false, throwOnError: false });
      } catch {
        return formula;
      }
    });

    return text;
  }

  /**
   * Convert local images to base64
   */
  private processImages(html: string, docDir: string): string {
    return html.replace(/src="([^"]+)"/g, (match, src) => {
      if (src.startsWith('http') || src.startsWith('data:')) {
        return match;
      }

      const filename = path.basename(src);
      const localPath = path.join(docDir, 'images', filename);

      if (fs.existsSync(localPath)) {
        const imgData = fs.readFileSync(localPath);
        const ext = path.extname(localPath).slice(1) || 'jpg';
        return `src="data:image/${ext};base64,${imgData.toString('base64')}"`;
      }

      return match;
    });
  }

  /**
   * Generate PDF from Markdown
   */
  async generate(
    markdown: string,
    profile: Profile | null,
    overrides: Record<string, EntityStyle>,
    docDir: string
  ): Promise<Buffer> {
    const pageSettings = profile?.page || DEFAULT_PAGE_SETTINGS;
    const pageSize = getPageDimensions(pageSettings.size, pageSettings.orientation);
    const pageNumbers = pageSettings.pageNumbers;

    // 1. Process LaTeX formulas
    const contentWithFormulas = this.renderLatex(markdown);

    // 2. Markdown -> HTML
    let html = await marked.parse(contentWithFormulas);

    // 3. Convert local images to base64
    html = this.processImages(html, docDir);

    // 4. Extract caption markers
    const captionMarkers = new Map<string, string>();
    let markerCounter = 0;

    html = html.replace(/<p>\[(TABLE-CAPTION|FORMULA-CAPTION):\s*(.+?)\]<\/p>/gi, (match, type, text) => {
      const marker = `__CAPTION_MARKER_${markerCounter++}__`;
      captionMarkers.set(marker, `${type.toUpperCase()}:${text.trim()}`);
      return marker;
    });

    // 5. Apply styles to elements
    const usedIds = new Set<string>();

    // Paragraphs
    html = html.replace(/<p>([^<]*(?:<(?!\/p>)[^<]*)*)<\/p>/g, (_, content) => {
      const text = content.replace(/<[^>]+>/g, '');
      const id = generateElementId('p', text, usedIds);
      const style = this.getFinalStyle('paragraph', id, profile, overrides);
      return `<p id="${id}" style="${styleToCSS(style)}">${content}</p>`;
    });

    // Headings
    for (let level = 1; level <= 6; level++) {
      const regex = new RegExp(`<h${level}>([^<]*(?:<(?!\\/h${level}>)[^<]*)*)<\\/h${level}>`, 'g');
      html = html.replace(regex, (_, content) => {
        const text = content.replace(/<[^>]+>/g, '');
        const id = generateElementId(`h${level}`, text, usedIds);
        const style = this.getFinalStyle('heading', id, profile, overrides);
        return `<h${level} id="${id}" style="${styleToCSS(style)}">${content}</h${level}>`;
      });
    }

    // Images
    html = html.replace(/<img([^>]*)>/g, (_, attrs) => {
      const srcMatch = attrs.match(/src="([^"]+)"/);
      const altMatch = attrs.match(/alt="([^"]*)"/);
      const src = srcMatch?.[1] || '';
      const alt = altMatch?.[1] || '';

      const id = generateElementId('img', src + alt, usedIds);
      const imageStyle = this.getFinalStyle('image', id, profile, overrides);

      const imgStyle = `max-width: ${imageStyle.maxWidth || 100}%; height: auto; display: block;`;
      let imgAlignStyle = '';
      if (imageStyle.textAlign === 'center') {
        imgAlignStyle = ' margin: 0 auto;';
      } else if (imageStyle.textAlign === 'right') {
        imgAlignStyle = ' margin-left: auto;';
      } else if (imageStyle.textAlign === 'left') {
        imgAlignStyle = ' margin-left: 0;';
      }

      let result = `<div id="${id}" data-type="image" style="${styleToCSS(imageStyle)}"><img${attrs} style="${imgStyle}${imgAlignStyle}"></div>`;

      // Image caption
      if (alt) {
        const captionId = generateElementId('caption', alt, usedIds);
        const captionStyle = this.getFinalStyle('image-caption', captionId, profile, overrides);
        result += `<div id="${captionId}" data-type="image-caption" style="${styleToCSS(captionStyle)}">${alt}</div>`;
      }

      return result;
    });

    // Lists - Unordered
    html = html.replace(/<ul>([^]*?)<\/ul>/g, (_, content) => {
      const text = content.replace(/<[^>]+>/g, '').slice(0, 50);
      const id = generateElementId('ul', text, usedIds);
      const style = this.getFinalStyle('unordered-list', id, profile, overrides);

      const paddingLeft = (style.paddingLeft !== undefined && style.paddingLeft > 0)
        ? `${style.paddingLeft}pt`
        : '20pt';

      let processedContent = content;
      const textIndent = style.textIndent;
      if (textIndent !== undefined && textIndent > 0) {
        const textIndentPt = textIndent * 28.35;
        let isFirst = true;
        processedContent = processedContent.replace(/<li>/g, () => {
          if (isFirst) {
            isFirst = false;
            return `<li style="margin-left: ${textIndentPt}pt;">`;
          } else {
            return `<li style="margin-left: 0;">`;
          }
        });
      }

      const listStyle: EntityStyle = { ...style, textIndent: undefined };
      return `<ul id="${id}" style="${styleToCSS(listStyle)}; list-style-type: disc; list-style-position: outside; padding-left: ${paddingLeft};">${processedContent}</ul>`;
    });

    // Lists - Ordered
    html = html.replace(/<ol>([^]*?)<\/ol>/g, (_, content) => {
      const text = content.replace(/<[^>]+>/g, '').slice(0, 50);
      const id = generateElementId('ol', text, usedIds);
      const style = this.getFinalStyle('ordered-list', id, profile, overrides);

      const paddingLeft = (style.paddingLeft !== undefined && style.paddingLeft > 0)
        ? `${style.paddingLeft}pt`
        : '20pt';

      let processedContent = content;
      const textIndent = style.textIndent;
      if (textIndent !== undefined && textIndent > 0) {
        const textIndentPt = textIndent * 28.35;
        let isFirst = true;
        processedContent = processedContent.replace(/<li>/g, () => {
          if (isFirst) {
            isFirst = false;
            return `<li style="margin-left: ${textIndentPt}pt;">`;
          } else {
            return `<li style="margin-left: 0;">`;
          }
        });
      }

      const listStyle: EntityStyle = { ...style, textIndent: undefined };
      return `<ol id="${id}" style="${styleToCSS(listStyle)}; list-style-type: decimal; list-style-position: outside; padding-left: ${paddingLeft};">${processedContent}</ol>`;
    });

    // Tables
    html = html.replace(/<table>([^]*?)<\/table>(\s*)(?:__CAPTION_MARKER_(\d+)__)?/g, (match, content, whitespace, markerNum) => {
      const marker = markerNum ? `__CAPTION_MARKER_${markerNum}__` : null;
      const text = content.replace(/<[^>]+>/g, '').slice(0, 50);
      const tableId = generateElementId('table', text, usedIds);
      const tableStyle = this.getFinalStyle('table', tableId, profile, overrides);

      const borderStyle = tableStyle.borderStyle || 'solid';
      const borderWidth = tableStyle.borderWidth || 1;
      const borderColor = tableStyle.borderColor || '#333';
      const cellStyle = `border: ${borderWidth}px ${borderStyle} ${borderColor}; padding: 8px;`;

      const processed = content
        .replace(/<th>/g, `<th style="${cellStyle}">`)
        .replace(/<td>/g, `<td style="${cellStyle}">`);

      let captionText: string | null = null;
      if (marker && captionMarkers.has(marker)) {
        const captionData = captionMarkers.get(marker)!;
        if (captionData.startsWith('TABLE-CAPTION:')) {
          captionText = captionData.substring('TABLE-CAPTION:'.length);
          captionMarkers.delete(marker);
        }
      }

      const tableFinalStyle: EntityStyle = {
        ...tableStyle,
        marginTop: undefined,
        marginBottom: undefined,
        marginLeft: undefined,
        marginRight: undefined,
      };

      let result = `<div id="${tableId}" data-type="table" style="${styleToCSS(tableStyle)}"><table style="${styleToCSS(tableFinalStyle)}; border-collapse: collapse; width: 100%;">${processed}</table></div>`;

      if (captionText) {
        const captionId = generateElementId('table-caption', captionText, usedIds);
        const captionStyle = this.getFinalStyle('table-caption', captionId, profile, overrides);
        result += `<div id="${captionId}" data-type="table-caption" style="${styleToCSS(captionStyle)}">${captionText}</div>`;
      }

      return result;
    });

    // Formulas
    html = html.replace(/<div class="formula-block">([^]*?)<\/div>(\s*)(?:__CAPTION_MARKER_(\d+)__)?/g, (match, content, whitespace, markerNum) => {
      const marker = markerNum ? `__CAPTION_MARKER_${markerNum}__` : null;
      const text = content.replace(/<[^>]+>/g, '').slice(0, 50);
      const formulaId = generateElementId('formula', text, usedIds);
      const formulaStyle = this.getFinalStyle('formula', formulaId, profile, overrides);

      let captionText: string | null = null;
      if (marker && captionMarkers.has(marker)) {
        const captionData = captionMarkers.get(marker)!;
        if (captionData.startsWith('FORMULA-CAPTION:')) {
          captionText = captionData.substring('FORMULA-CAPTION:'.length);
          captionMarkers.delete(marker);
        }
      }

      const formulaFinalStyle: EntityStyle = {
        ...formulaStyle,
        marginTop: undefined,
        marginBottom: undefined,
        marginLeft: undefined,
        marginRight: undefined,
      };

      let result = `<div id="${formulaId}" data-type="formula" style="${styleToCSS(formulaStyle)}"><div class="formula-block" style="${styleToCSS(formulaFinalStyle)}">${content}</div></div>`;

      if (captionText) {
        const captionId = generateElementId('formula-caption', captionText, usedIds);
        const captionStyle = this.getFinalStyle('formula-caption', captionId, profile, overrides);
        result += `<div id="${captionId}" data-type="formula-caption" style="${styleToCSS(captionStyle)}">${captionText}</div>`;
      }

      return result;
    });

    // Process remaining caption markers
    captionMarkers.forEach((value, marker) => {
      const [type, text] = value.split(':', 2);

      if (type === 'TABLE-CAPTION') {
        html = html.replace(new RegExp(`(<div[^>]*data-type="table"[^>]*>.*?</div>)([^]{0,500}?)${marker.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}`, 's'), (match, tableDiv, between) => {
          if (!between.includes('data-type="table-caption"')) {
            const captionId = generateElementId('table-caption', text, usedIds);
            const captionStyle = this.getFinalStyle('table-caption', captionId, profile, overrides);
            return tableDiv + `<div id="${captionId}" data-type="table-caption" style="${styleToCSS(captionStyle)}">${text}</div>` + between;
          }
          return match.replace(marker, '');
        });
      } else if (type === 'FORMULA-CAPTION') {
        html = html.replace(new RegExp(`(<div[^>]*data-type="formula"[^>]*>.*?</div>)([^]{0,500}?)${marker.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}`, 's'), (match, formulaDiv, between) => {
          if (!between.includes('data-type="formula-caption"')) {
            const captionId = generateElementId('formula-caption', text, usedIds);
            const captionStyle = this.getFinalStyle('formula-caption', captionId, profile, overrides);
            return formulaDiv + `<div id="${captionId}" data-type="formula-caption" style="${styleToCSS(captionStyle)}">${text}</div>` + between;
          }
          return match.replace(marker, '');
        });
      }

      html = html.replace(marker, '');
    });

    // 6. Build full HTML
    const fullHtml = `
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.css">
  <style>
    @page {
      size: ${pageSize.width} ${pageSize.height};
      margin: ${pageSettings.margins.top}mm ${pageSettings.margins.right}mm ${pageSettings.margins.bottom}mm ${pageSettings.margins.left}mm;
    }
    * { box-sizing: border-box; }
    body {
      font-family: 'Times New Roman', Times, serif;
      font-size: 14pt;
      line-height: 1.5;
      margin: 0;
      padding: 0;
    }
    p, h1, h2, h3, h4, h5, h6 { margin: 0; padding: 0; }
    ul, ol { margin: 0; padding-left: 20pt; }
    ul { list-style-type: disc !important; list-style-position: outside; }
    ol { list-style-type: decimal !important; list-style-position: outside; }
    li { margin: 0; padding: 0; }
    div[data-type] { display: block; }
    img { max-width: 100%; height: auto; margin: 0; padding: 0; }
    table { page-break-inside: avoid; margin: 0; padding: 0; }
    .formula-block { display: block; text-align: center; margin: 0 !important; padding: 0 !important; }
    .formula-block .katex-display { margin: 0 !important; }
    .formula-block .katex { margin: 0 !important; }
    .katex-display { margin: 0 !important; }
    .katex { margin: 0 !important; }
  </style>
</head>
<body>
  ${html}
</body>
</html>`;

    // 7. Generate PDF with Puppeteer
    const browser = await puppeteer.launch({
      headless: true,
      args: ['--no-sandbox', '--disable-setuid-sandbox'],
    });

    try {
      const page = await browser.newPage();
      await page.setContent(fullHtml, { waitUntil: 'networkidle0' });

      const pdfOptions: Parameters<typeof page.pdf>[0] = {
        format: pageSettings.size as 'A4' | 'A5' | 'Letter',
        landscape: pageSettings.orientation === 'landscape',
        margin: {
          top: `${pageSettings.margins.top}mm`,
          right: `${pageSettings.margins.right}mm`,
          bottom: `${pageSettings.margins.bottom}mm`,
          left: `${pageSettings.margins.left}mm`,
        },
        printBackground: true,
      };

      // Page numbers
      if (pageNumbers?.enabled) {
        pdfOptions.displayHeaderFooter = true;

        const format = pageNumbers.format
          .replace('{n}', '<span class="pageNumber"></span>')
          .replace('{total}', '<span class="totalPages"></span>');

        const fontFamily = pageNumbers.fontFamily ? `font-family: ${pageNumbers.fontFamily};` : '';
        const template = `<div style="${fontFamily} font-size: ${pageNumbers.fontSize || 12}pt; font-style: ${pageNumbers.fontStyle || 'normal'}; width: 100%; text-align: ${pageNumbers.align}; padding: 10px;">${format}</div>`;

        if (pageNumbers.position === 'top') {
          pdfOptions.headerTemplate = template;
          pdfOptions.footerTemplate = '<div></div>';
        } else {
          pdfOptions.headerTemplate = '<div></div>';
          pdfOptions.footerTemplate = template;
        }
      }

      const pdfBuffer = await page.pdf(pdfOptions);
      return Buffer.from(pdfBuffer);
    } finally {
      await browser.close();
    }
  }
}

