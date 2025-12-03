/**
 * JavaScript code to fix text alignment in Puppeteer
 * This matches Canvas behavior where each line is measured and positioned independently
 */
export const TEXT_ALIGNMENT_SCRIPT = `
  (function() {
    const textLines = document.querySelectorAll('.text-line[data-text-align]');
    const MM_TO_PX = 3.7795275591;
    
    textLines.forEach((element) => {
      const htmlElement = element;
      const textAlign = htmlElement.getAttribute('data-text-align');
      const originalX = parseFloat(htmlElement.getAttribute('data-original-x') || '0');
      
      if (textAlign !== 'center' && textAlign !== 'right') {
        return;
      }

      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');
      if (!ctx) return;

      const computedStyle = window.getComputedStyle(htmlElement);
      const fontSize = computedStyle.fontSize;
      const fontFamily = computedStyle.fontFamily;
      const fontWeight = computedStyle.fontWeight;
      const fontStyle = computedStyle.fontStyle;
      
      ctx.font = \`\${fontStyle} \${fontWeight} \${fontSize} \${fontFamily}\`;

      const textContent = htmlElement.textContent || '';
      const metrics = ctx.measureText(textContent);
      const lineWidth = metrics.width;
      const lineWidthMm = lineWidth / MM_TO_PX;

      let newLeft;
      if (textAlign === 'center') {
        newLeft = originalX - lineWidthMm / 2;
        htmlElement.style.textAlign = 'center';
      } else {
        newLeft = originalX - lineWidthMm;
        htmlElement.style.textAlign = 'right';
      }

      htmlElement.style.left = \`\${newLeft}mm\`;
    });
  })();
`;

