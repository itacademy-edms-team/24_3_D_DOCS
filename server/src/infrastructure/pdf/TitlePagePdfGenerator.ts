import puppeteer from 'puppeteer';
import type { TitlePage, TitlePageElement } from '../../../../shared/src/types';

/**
 * PDF Generator for Title Pages
 * Converts Canvas elements to PDF with precise positioning in mm
 */
export class TitlePagePdfGenerator {
  /**
   * Generate PDF from TitlePage
   */
  async generate(titlePage: TitlePage): Promise<Buffer> {
    // A4 dimensions in mm
    const pageWidth = 210;
    const pageHeight = 297;

    // Build HTML with absolute positioning
    const elementsHtml = titlePage.elements.map(element => {
      return this.renderElement(element, titlePage.variables);
    }).join('\n');

    const fullHtml = `
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <style>
    @page {
      size: A4;
      margin: 0;
    }
    * {
      box-sizing: border-box;
      margin: 0;
      padding: 0;
    }
    body {
      width: ${pageWidth}mm;
      height: ${pageHeight}mm;
      margin: 0;
      padding: 0;
      position: relative;
      font-family: 'Times New Roman', Times, serif;
    }
    .element {
      position: absolute;
    }
    .text-element, .variable-element {
      white-space: pre-wrap;
    }
    .line-element {
      background-color: #000;
    }
  </style>
</head>
<body>
  ${elementsHtml}
</body>
</html>`;

    // Generate PDF with Puppeteer
    const browser = await puppeteer.launch({
      headless: true,
      args: ['--no-sandbox', '--disable-setuid-sandbox'],
    });

    try {
      const page = await browser.newPage();
      await page.setContent(fullHtml, { waitUntil: 'networkidle0' });

      const pdfBuffer = await page.pdf({
        format: 'A4',
        landscape: false,
        margin: {
          top: '0mm',
          right: '0mm',
          bottom: '0mm',
          left: '0mm',
        },
        printBackground: true,
      });

      return Buffer.from(pdfBuffer);
    } finally {
      await browser.close();
    }
  }

  /**
   * Render single element to HTML
   */
  private renderElement(element: TitlePageElement, variables: Record<string, string>): string {
    const style = `left: ${element.x}mm; top: ${element.y}mm;`;

    if (element.type === 'line') {
      const length = element.length || 100;
      const thickness = element.thickness || 1;
      return `<div class="element line-element" style="${style} width: ${length}mm; height: ${thickness}mm;"></div>`;
    }

    if (element.type === 'variable') {
      const content = variables[element.variableKey || ''] || '';
      const textStyle = this.getTextStyle(element);
      return `<div class="element variable-element" style="${style}${textStyle}">${this.escapeHtml(content)}</div>`;
    }

    // text type
    const content = element.content || '';
    const textStyle = this.getTextStyle(element);
    return `<div class="element text-element" style="${style}${textStyle}">${this.escapeHtml(content)}</div>`;
  }

  /**
   * Get CSS style for text elements
   */
  private getTextStyle(element: TitlePageElement): string {
    const styles: string[] = [];

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
    if (element.lineHeight) {
      styles.push(`line-height: ${element.lineHeight}`);
    }
    if (element.textAlign) {
      styles.push(`text-align: ${element.textAlign}`);
    }

    return styles.length > 0 ? ` ${styles.join('; ')};` : '';
  }

  /**
   * Escape HTML special characters
   */
  private escapeHtml(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }
}

