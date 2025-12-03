/**
 * Build HTML template for title page PDF
 */
export function buildHtmlTemplate(elementsHtml: string, pageWidth: number, pageHeight: number): string {
  return `
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
      display: inline-block;
      width: fit-content;
      max-width: 100%;
    }
    .text-line {
      position: absolute;
      white-space: nowrap;
      display: inline-block;
      width: fit-content;
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
}

