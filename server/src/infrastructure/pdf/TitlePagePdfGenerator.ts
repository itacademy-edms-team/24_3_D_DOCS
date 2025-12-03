import type { TitlePage, TitlePageElement } from '../../../../shared/src/types';
import { launchBrowser } from './common/browserLauncher';
import { renderLineElement } from './titlePage/renderers/lineElementRenderer';
import { renderTextElement, renderVariableElement } from './titlePage/renderers/textElementRenderer';
import { buildHtmlTemplate } from './titlePage/utils/htmlBuilder';
import { TEXT_ALIGNMENT_SCRIPT } from './titlePage/utils/textAlignment';

/**
 * PDF Generator for Title Pages
 * Converts Canvas elements to PDF with precise positioning in mm
 */
export class TitlePagePdfGenerator {
  // A4 dimensions in mm
  private readonly pageWidth = 210;
  private readonly pageHeight = 297;

  /**
   * Generate PDF from TitlePage
   * @param titlePage - The title page to generate PDF from
   * @param documentVariables - Optional variables from document frontmatter (takes priority over titlePage.variables)
   */
  async generate(titlePage: TitlePage, documentVariables?: Record<string, string>): Promise<Buffer> {
    // Merge variables: documentVariables have priority over titlePage.variables
    const mergedVariables = {
      ...titlePage.variables,
      ...(documentVariables || {}),
    };
    
    // Build HTML with absolute positioning
    const elementsHtml = titlePage.elements.map(element => {
      return this.renderElement(element, mergedVariables);
    }).join('\n');

    const fullHtml = buildHtmlTemplate(elementsHtml, this.pageWidth, this.pageHeight);

    // Generate PDF with Puppeteer
    const browser = await launchBrowser();

    try {
      const page = await browser.newPage();
      await page.setContent(fullHtml, { waitUntil: 'networkidle0' });

      // Fix text alignment by measuring each line separately
      await page.evaluate(TEXT_ALIGNMENT_SCRIPT);

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
    if (element.type === 'line') {
      return renderLineElement(element);
    } else if (element.type === 'text') {
      return renderTextElement(element, variables);
    } else if (element.type === 'variable') {
      return renderVariableElement(element, variables);
    }
    return '';
  }
}
