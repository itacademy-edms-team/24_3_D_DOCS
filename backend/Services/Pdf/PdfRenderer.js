/**
 * PDF Renderer - Standalone JavaScript bundle for Puppeteer
 * This file includes all rendering logic from the frontend to ensure 100% visual consistency
 */

(function(global) {
    'use strict';

    // Constants
    const MM_TO_PX = 3.7795275591;
    const A4_WIDTH_MM = 210;
    const A4_HEIGHT_MM = 297;
    const PAGE_SIZES = {
        A4: { width: 210, height: 297 },
        A5: { width: 148, height: 210 }
    };

    // Utility functions
    function generateElementId(type, content, usedIds) {
        const baseId = `${type}-${content.slice(0, 50).replace(/[^a-z0-9]/gi, '-')}`;
        let id = baseId;
        let counter = 0;

        while (usedIds.has(id)) {
            counter++;
            id = `${baseId}-${counter}`;
        }

        usedIds.add(id);
        return id;
    }

    function getFinalStyle(entityType, elementId, profile, overrides) {
        let baseStyle = profile?.entityStyles?.[entityType] || profile?.entityStyles?.[entityType.replace('-', '_')] || {};
        
        if (!baseStyle && entityType.startsWith('heading-')) {
            baseStyle = profile?.entityStyles?.heading || {};
        }
        
        if (!baseStyle) {
            baseStyle = {};
        }

        const override = overrides[elementId] || {};
        const mergedStyle = { ...baseStyle, ...override };
        
        if (mergedStyle.lineHeightUseGlobal === true && profile?.pageSettings?.globalLineHeight !== undefined) {
            mergedStyle.lineHeight = profile.pageSettings.globalLineHeight;
        }
        
        return mergedStyle;
    }

    function styleToCSS(style) {
        const cssParts = [];

        if (style.fontFamily) cssParts.push(`font-family: ${style.fontFamily}, serif`);
        if (style.fontSize !== undefined) cssParts.push(`font-size: ${style.fontSize}pt`);
        if (style.fontWeight) cssParts.push(`font-weight: ${style.fontWeight}`);
        if (style.fontStyle) cssParts.push(`font-style: ${style.fontStyle}`);
        if (style.textAlign) cssParts.push(`text-align: ${style.textAlign}`);
        if (style.textIndent !== undefined) cssParts.push(`text-indent: ${style.textIndent}cm`);
        if (style.lineHeight !== undefined) cssParts.push(`line-height: ${style.lineHeight}`);
        if (style.color) cssParts.push(`color: ${style.color}`);
        if (style.backgroundColor) cssParts.push(`background-color: ${style.backgroundColor}`);
        if (style.marginTop !== undefined) cssParts.push(`margin-top: ${style.marginTop}pt`);
        if (style.marginBottom !== undefined) cssParts.push(`margin-bottom: ${style.marginBottom}pt`);
        if (style.marginLeft !== undefined) cssParts.push(`margin-left: ${style.marginLeft}pt`);
        if (style.marginRight !== undefined) cssParts.push(`margin-right: ${style.marginRight}pt`);
        if (style.paddingLeft !== undefined) cssParts.push(`padding-left: ${style.paddingLeft}pt`);
        if (style.borderWidth !== undefined) cssParts.push(`border-width: ${style.borderWidth}px`);
        if (style.borderColor) cssParts.push(`border-color: ${style.borderColor}`);
        if (style.borderStyle) cssParts.push(`border-style: ${style.borderStyle}`);
        if (style.maxWidth !== undefined) cssParts.push(`max-width: ${style.maxWidth}%`);

        return cssParts.join('; ');
    }

    function applyStyles(element, entityType, elementId, profile, overrides, selectable) {
        const style = getFinalStyle(entityType, elementId, profile, overrides);
        element.id = elementId;
        element.setAttribute('data-type', entityType);
        // Add CSS class to match CSS selectors from profile
        element.classList.add(entityType);
        element.setAttribute('style', styleToCSS(style));
        if (selectable) element.classList.add('element-selectable');
    }

    function calculatePageDimensions(profile) {
        const defaultSettings = {
            size: 'A4',
            orientation: 'portrait',
            margins: { top: 20, right: 20, bottom: 20, left: 20 }
        };

        const settings = profile?.pageSettings || defaultSettings;
        const pageSize = PAGE_SIZES[settings.size] || PAGE_SIZES.A4;
        const isLandscape = settings.orientation === 'landscape';

        const pageWidth = (isLandscape ? pageSize.height : pageSize.width) * MM_TO_PX;
        const pageHeight = (isLandscape ? pageSize.width : pageSize.height) * MM_TO_PX;

        return {
            pageWidth,
            pageHeight,
            marginTop: (settings.margins?.top || defaultSettings.margins.top) * MM_TO_PX,
            marginRight: (settings.margins?.right || defaultSettings.margins.right) * MM_TO_PX,
            marginBottom: (settings.margins?.bottom || defaultSettings.margins.bottom) * MM_TO_PX,
            marginLeft: (settings.margins?.left || defaultSettings.margins.left) * MM_TO_PX
        };
    }

    // Helper function to calculate heading numbers
    function calculateHeadingNumbers(headings, settings) {
        const numbers = new Map();
        const counters = [0, 0, 0, 0, 0, 0]; // Counters for levels 1-6

        headings.forEach((heading) => {
            const level = parseInt(heading.tagName[1], 10); // Level 1-6
            const levelIndex = level - 1; // Convert to 0-based index
            const template = settings?.templates?.[level];

            if (template?.enabled) {
                // Reset counters for deeper levels
                for (let i = levelIndex + 1; i < 6; i++) {
                    counters[i] = 0;
                }

                // Increment counter for current level
                counters[levelIndex]++;

                // Build hierarchical number (e.g., "1.2.3")
                const numberParts = [];
                for (let i = 0; i <= levelIndex; i++) {
                    numberParts.push(counters[i].toString());
                }
                const number = numberParts.join('.');

                numbers.set(heading, number);
            }
        });

        return numbers;
    }

    // Helper function to calculate list item nesting level
    function calculateListItemLevel(li) {
        let level = 0;
        let parent = li.parentElement;

        while (parent && parent !== li.ownerDocument?.body) {
            if (parent.tagName === 'UL' || parent.tagName === 'OL') {
                level++;
            }
            parent = parent.parentElement;
        }

        return Math.max(0, level - 1);
    }

    // Helper function to calculate list indent
    function calculateListIndent(style, profile, nestingLevel) {
        if (style.listUseParagraphTextIndent === true) {
            const paragraphStyle = profile?.entityStyles?.['paragraph'];
            if (paragraphStyle?.textIndent !== undefined && paragraphStyle.textIndent > 0) {
                return (paragraphStyle.textIndent * 10) * nestingLevel;
            }
            return null;
        }

        if (style.listAdditionalIndent !== undefined) {
            return style.listAdditionalIndent * nestingLevel;
        }

        return null;
    }

    // Render formulas with profile styles and captions
    function renderFormulas(doc, usedIds, profile, overrides, selectable) {
        const formulas = Array.from(doc.querySelectorAll('.formula-block'));

        // First pass: calculate formula numbers for formulas with captions
        const formulaNumbers = new Map();
        let formulaCounter = 0;

        formulas.forEach((formulaBlock) => {
            const nextSibling = formulaBlock.nextElementSibling;
            if (nextSibling && nextSibling.tagName === 'P') {
                const pText = nextSibling.textContent || '';
                const match = pText.match(/^\[FORMULA-CAPTION:\s*(.+)\]$/);
                if (match) {
                    formulaCounter++;
                    formulaNumbers.set(formulaBlock, formulaCounter);
                }
            }
        });

        // Second pass: render formulas
        formulas.forEach((formulaBlock) => {
            const content = formulaBlock.textContent || '';
            const elId = generateElementId('formula', content.slice(0, 50), usedIds);
            const style = getFinalStyle('formula', elId, profile, overrides);

            let captionText = null;
            const formulaNumber = formulaNumbers.get(formulaBlock);
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
            formulaWrapper.classList.add('formula');
            formulaWrapper.setAttribute('style', styleToCSS(style));
            if (selectable) formulaWrapper.classList.add('element-selectable');

            const parent = formulaBlock.parentNode;
            if (parent) {
                parent.insertBefore(formulaWrapper, formulaBlock);
                formulaWrapper.appendChild(formulaBlock);

                const formulaFinalStyle = Object.assign({}, style);
                delete formulaFinalStyle.marginTop;
                delete formulaFinalStyle.marginBottom;
                delete formulaFinalStyle.marginLeft;
                delete formulaFinalStyle.marginRight;
                formulaBlock.setAttribute('style', styleToCSS(formulaFinalStyle));
                if (selectable) formulaBlock.classList.add('element-selectable');

                if (captionText && formulaNumber) {
                    const captionId = generateElementId('formula-caption', captionText, usedIds);
                    const captionStyle = getFinalStyle('formula-caption', captionId, profile, overrides);

                    const captionFormat = captionStyle.captionFormat ||
                        profile?.entityStyles?.['formula-caption']?.captionFormat ||
                        'Формула {n} - {content}';

                    const formattedCaption = captionFormat
                        .replace(/{n}/g, formulaNumber.toString())
                        .replace(/{content}/g, captionText);

                    const caption = doc.createElement('div');
                    caption.id = captionId;
                    caption.setAttribute('data-type', 'formula-caption');
                    caption.classList.add('formula-caption');
                    caption.setAttribute('style', styleToCSS(captionStyle));
                    caption.textContent = formattedCaption;
                    if (selectable) caption.classList.add('element-selectable');

                    parent.insertBefore(caption, formulaWrapper.nextSibling);
                }
            }
        });
    }

    // Render paragraphs with profile styles
    function renderParagraphs(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('p').forEach((el) => {
            if (el.closest('figure')) return;

            const text = el.textContent || '';
            if (text.match(/^\[(IMAGE|TABLE|FORMULA)-CAPTION:/)) return;

            const content = text;
            const elId = generateElementId('p', content, usedIds);
            applyStyles(el, 'paragraph', elId, profile, overrides, selectable);
        });
    }

    // Render headings with profile styles and numbering
    function renderHeadings(doc, usedIds, profile, overrides, selectable) {
        const headings = Array.from(doc.querySelectorAll('h1, h2, h3, h4, h5, h6'));

        const numberingSettings = profile?.headingNumbering;
        const headingNumbers = calculateHeadingNumbers(headings, numberingSettings);

        headings.forEach((el) => {
            const originalContent = el.textContent || '';
            const level = parseInt(el.tagName[1], 10);
            const elId = generateElementId(`h${level}`, originalContent, usedIds);

            const template = numberingSettings?.templates?.[level];

            if (template?.enabled && template.format) {
                const number = headingNumbers.get(el) || '';
                const formattedText = template.format
                    .replace(/{n}/g, number)
                    .replace(/{content}/g, originalContent);
                el.textContent = formattedText;
            }

            const headingType = `heading-${level}`;
            applyStyles(el, headingType, elId, profile, overrides, selectable);
        });
    }

    // Render images with profile styles and captions
    function renderImages(doc, usedIds, profile, overrides, selectable) {
        const images = Array.from(doc.querySelectorAll('img'));

        // First pass: find all caption paragraphs and map them to previous images
        const imageCaptionMap = new Map();
        let imageCounter = 0;

        const allParagraphs = Array.from(doc.querySelectorAll('p'));
        allParagraphs.forEach((captionP) => {
            const pText = (captionP.textContent || '').trim();
            const match = pText.match(/^\[IMAGE-CAPTION:\s*(.+)\]$/);
            if (match) {
                let prevSibling = captionP.previousElementSibling;

                while (prevSibling) {
                    if (prevSibling.tagName === 'P') {
                        const imgInParagraph = prevSibling.querySelector('img');
                        if (imgInParagraph && images.includes(imgInParagraph)) {
                            imageCounter++;
                            imageCaptionMap.set(imgInParagraph, {
                                paragraph: captionP,
                                text: match[1].trim(),
                                imageNumber: imageCounter,
                                imageParagraph: prevSibling
                            });
                            break;
                        }
                    }
                    prevSibling = prevSibling.previousElementSibling;
                }
            }
        });

        // Second pass: render images with their captions
        images.forEach((img) => {
            const src = img.getAttribute('src') || '';
            const alt = img.getAttribute('alt') || '';
            const elId = generateElementId('img', src + alt, usedIds);
            const imageStyle = getFinalStyle('image', elId, profile, overrides);

            const imgWrapper = doc.createElement('div');
            imgWrapper.id = elId;
            imgWrapper.setAttribute('data-type', 'image');
            imgWrapper.classList.add('image');
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

                const captionInfo = imageCaptionMap.get(img);
                if (captionInfo) {
                    const captionText = captionInfo.text;
                    const captionParagraph = captionInfo.paragraph;
                    const imageNumber = captionInfo.imageNumber;
                    const imageParagraph = captionInfo.imageParagraph;

                    const captionId = generateElementId('image-caption', captionText, usedIds);
                    const captionStyle = getFinalStyle('image-caption', captionId, profile, overrides);

                    const captionFormat = captionStyle.captionFormat ||
                        profile?.entityStyles?.['image-caption']?.captionFormat ||
                        'Рисунок {n} - {content}';

                    const formattedCaption = captionFormat
                        .replace(/{n}/g, imageNumber.toString())
                        .replace(/{content}/g, captionText);

                    const caption = doc.createElement('div');
                    caption.id = captionId;
                    caption.setAttribute('data-type', 'image-caption');
                    caption.classList.add('image-caption');
                    caption.setAttribute('style', styleToCSS(captionStyle));
                    caption.textContent = formattedCaption;
                    if (selectable) caption.classList.add('element-selectable');

                    if (captionParagraph.parentElement) {
                        captionParagraph.remove();
                    }

                    if (imageParagraph && imageParagraph.parentNode && doc.body.contains(imageParagraph)) {
                        if (imageParagraph.nextSibling) {
                            imageParagraph.parentNode.insertBefore(caption, imageParagraph.nextSibling);
                        } else {
                            imageParagraph.parentNode.appendChild(caption);
                        }
                    } else {
                        let wrapperParent = imgWrapper.parentElement;
                        while (wrapperParent && wrapperParent.tagName !== 'P' && wrapperParent !== doc.body) {
                            wrapperParent = wrapperParent.parentElement;
                        }

                        if (wrapperParent && wrapperParent.parentElement) {
                            if (wrapperParent.nextSibling) {
                                wrapperParent.parentElement.insertBefore(caption, wrapperParent.nextSibling);
                            } else {
                                wrapperParent.parentElement.appendChild(caption);
                            }
                        } else {
                            if (imgWrapper.nextSibling) {
                                parent.insertBefore(caption, imgWrapper.nextSibling);
                            } else {
                                parent.appendChild(caption);
                            }
                        }
                    }
                }
            }
        });
    }

    // Render unordered lists with profile styles
    function renderUnorderedLists(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('ul').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('ul', content.slice(0, 50), usedIds);
            const style = getFinalStyle('unordered-list', elId, profile, overrides);

            const listStyle = Object.assign({}, style);
            delete listStyle.textIndent;

            el.id = elId;
            el.setAttribute('data-type', 'unordered-list');
            el.classList.add('unordered-list');
            el.setAttribute('style', styleToCSS(listStyle));
            if (selectable) el.classList.add('element-selectable');

            const listItems = el.querySelectorAll('li');
            listItems.forEach((li) => {
                const nestingLevel = calculateListItemLevel(li);
                const indentValue = calculateListIndent(style, profile, nestingLevel);

                if (indentValue !== null && indentValue !== 0) {
                    const indentPt = indentValue * 2.83465; // Convert mm to pt
                    const currentStyle = li.getAttribute('style') || '';
                    li.setAttribute('style', `${currentStyle}; margin-left: ${indentPt}pt;`.trim());
                }
            });
        });
    }

    // Render ordered lists with profile styles
    function renderOrderedLists(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('ol').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('ol', content.slice(0, 50), usedIds);
            const style = getFinalStyle('ordered-list', elId, profile, overrides);

            const listStyle = Object.assign({}, style);
            delete listStyle.textIndent;

            el.id = elId;
            el.setAttribute('data-type', 'ordered-list');
            el.classList.add('ordered-list');
            el.setAttribute('style', styleToCSS(listStyle));
            if (selectable) el.classList.add('element-selectable');

            const listItems = el.querySelectorAll('li');
            listItems.forEach((li) => {
                const nestingLevel = calculateListItemLevel(li);
                const indentValue = calculateListIndent(style, profile, nestingLevel);

                if (indentValue !== null && indentValue !== 0) {
                    const indentPt = indentValue * 2.83465; // Convert mm to pt
                    const currentStyle = li.getAttribute('style') || '';
                    li.setAttribute('style', `${currentStyle}; margin-left: ${indentPt}pt;`.trim());
                }
            });
        });
    }

    // Render task lists with profile styles
    function renderTaskLists(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('ul').forEach((ul) => {
            const hasCheckboxes = ul.querySelector('li input[type="checkbox"]');
            if (hasCheckboxes) {
                renderUnorderedLists(doc, usedIds, profile, overrides, selectable);
            }
        });
    }

    // Render tables with profile styles and captions
    function renderTables(doc, usedIds, profile, overrides, selectable) {
        const tables = Array.from(doc.querySelectorAll('table'));

        // First pass: calculate table numbers for tables with captions
        const tableNumbers = new Map();
        let tableCounter = 0;

        tables.forEach((table) => {
            const nextSibling = table.nextElementSibling;
            if (nextSibling && nextSibling.tagName === 'P') {
                const pText = nextSibling.textContent || '';
                const match = pText.match(/^\[TABLE-CAPTION:\s*(.+)\]$/);
                if (match) {
                    tableCounter++;
                    tableNumbers.set(table, tableCounter);
                }
            }
        });

        // Second pass: render tables
        tables.forEach((table) => {
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

            let captionText = null;
            const tableNumber = tableNumbers.get(table);
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
            // Do NOT add 'table' class to wrapper - it should only be on the table element itself
            // to avoid inheriting table border styles
            // Add width constraints to prevent table from shrinking entire document
            // styleToCSS includes margin-bottom if defined in style, ensuring it's in inline style
            const wrapperStyle = styleToCSS(style);
            tableWrapper.setAttribute('style', wrapperStyle + '; max-width: 100%; overflow-x: auto; box-sizing: border-box;');
            if (selectable) tableWrapper.classList.add('element-selectable');
            
            // Add class to the table element itself for CSS matching
            table.classList.add('table');

            const parent = table.parentNode;
            if (parent) {
                parent.insertBefore(tableWrapper, table);
                tableWrapper.appendChild(table);

                const tableFinalStyle = Object.assign({}, style);
                delete tableFinalStyle.marginTop;
                delete tableFinalStyle.marginBottom;
                delete tableFinalStyle.marginLeft;
                delete tableFinalStyle.marginRight;
                // Ensure table respects container width and doesn't force document to shrink
                table.setAttribute('style', styleToCSS(tableFinalStyle) + '; border-collapse: collapse; width: 100%; max-width: 100%; table-layout: auto; box-sizing: border-box;');

                if (captionText && tableNumber) {
                    const captionId = generateElementId('table-caption', captionText, usedIds);
                    const captionStyle = getFinalStyle('table-caption', captionId, profile, overrides);

                    const captionFormat = captionStyle.captionFormat ||
                        profile?.entityStyles?.['table-caption']?.captionFormat ||
                        'Таблица {n} - {content}';

                    const formattedCaption = captionFormat
                        .replace(/{n}/g, tableNumber.toString())
                        .replace(/{content}/g, captionText);

                    const caption = doc.createElement('div');
                    caption.id = captionId;
                    caption.setAttribute('data-type', 'table-caption');
                    caption.classList.add('table-caption');
                    caption.setAttribute('style', styleToCSS(captionStyle));
                    caption.textContent = formattedCaption;
                    if (selectable) caption.classList.add('element-selectable');

                    parent.insertBefore(caption, tableWrapper.nextSibling);
                }
            }
        });
    }

    // Render code blocks with profile styles
    function renderCodeBlocks(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('pre > code, pre code').forEach((codeElement) => {
            const preElement = codeElement.parentElement;
            if (!preElement || preElement.tagName !== 'PRE') return;

            const content = codeElement.textContent || '';
            const elId = generateElementId('code', content.slice(0, 50), usedIds);

            applyStyles(preElement, 'code', elId, profile, overrides, selectable);
        });
    }

    // Render inline code with profile styles
    function renderInlineCode(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('code').forEach((codeElement) => {
            if (codeElement.parentElement?.tagName === 'PRE') return;

            const content = codeElement.textContent || '';
            const elId = generateElementId('code-inline', content.slice(0, 50), usedIds);

            codeElement.id = elId;
            codeElement.setAttribute('data-type', 'code-inline');
            codeElement.classList.add('code-inline');
            if (selectable) codeElement.classList.add('element-selectable');

            const style = profile?.entityStyles?.['code-inline'] || {};
            if (style.backgroundColor) {
                codeElement.setAttribute('style', `background-color: ${style.backgroundColor}; padding: 2px 4px; border-radius: 3px;`);
            }
        });
    }

    // Render blockquotes with profile styles
    function renderBlockquotes(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('blockquote').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('blockquote', content.slice(0, 50), usedIds);
            applyStyles(el, 'blockquote', elId, profile, overrides, selectable);

            const currentStyle = el.getAttribute('style') || '';
            if (!currentStyle.includes('border-left')) {
                el.setAttribute('style', `${currentStyle}; border-left: 3px solid #ccc; padding-left: 1em; margin: 1em 0;`.trim());
            }
        });
    }

    // Render links with profile styles
    function renderLinks(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('a').forEach((el) => {
            const href = el.getAttribute('href') || '';
            const text = el.textContent || '';
            const elId = generateElementId('link', href + text, usedIds);
            applyStyles(el, 'link', elId, profile, overrides, selectable);

            const currentStyle = el.getAttribute('style') || '';
            if (!currentStyle.includes('color')) {
                el.setAttribute('style', `${currentStyle}; color: #0066cc; text-decoration: underline;`.trim());
            }
        });
    }

    // Render horizontal rules with profile styles
    function renderHorizontalRules(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('hr').forEach((el) => {
            const elId = generateElementId('hr', `hr-${usedIds.size}`, usedIds);
            applyStyles(el, 'horizontal-rule', elId, profile, overrides, selectable);

            const currentStyle = el.getAttribute('style') || '';
            if (!currentStyle.includes('border')) {
                el.setAttribute('style', `${currentStyle}; border: none; border-top: 1px solid #ccc; margin: 1em 0;`.trim());
            }
        });
    }

    // Render highlighted text with profile styles
    function renderHighlight(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('mark').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('mark', content.slice(0, 50), usedIds);
            applyStyles(el, 'highlight', elId, profile, overrides, selectable);

            const style = getFinalStyle('highlight', elId, profile, overrides);
            const currentStyle = el.getAttribute('style') || '';
            const styleParts = [];

            const textColor = style.highlightColor || style.color;
            const bgColor = style.highlightBackgroundColor || style.backgroundColor;

            if (textColor) {
                styleParts.push(`color: ${textColor}`);
            }

            if (bgColor) {
                styleParts.push(`background-color: ${bgColor}`);
            }

            if (!bgColor && !currentStyle.includes('background-color')) {
                styleParts.push('background-color: #ffeb3b');
            }

            // Add print-color-adjust to force background-color rendering in PDF
            if (styleParts.length > 0 || currentStyle.includes('background-color')) {
                styleParts.push('-webkit-print-color-adjust: exact');
                styleParts.push('print-color-adjust: exact');
            }

            if (styleParts.length > 0) {
                styleParts.push('padding: 2px 4px; border-radius: 2px');
                el.setAttribute('style', currentStyle ? `${currentStyle}; ${styleParts.join('; ')}` : styleParts.join('; '));
            } else if (!currentStyle.includes('background-color')) {
                el.setAttribute('style', 'background-color: #ffeb3b; padding: 2px 4px; border-radius: 2px; -webkit-print-color-adjust: exact; print-color-adjust: exact;');
            } else {
                // If background-color exists but print-color-adjust doesn't, add it
                if (!currentStyle.includes('print-color-adjust')) {
                    el.setAttribute('style', currentStyle + '; -webkit-print-color-adjust: exact; print-color-adjust: exact;');
                }
            }
        });
    }

    // Render superscript with profile styles
    function renderSuperscript(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('sup').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('sup', content.slice(0, 50), usedIds);
            applyStyles(el, 'superscript', elId, profile, overrides, selectable);

            const currentStyle = el.getAttribute('style') || '';
            if (!currentStyle.includes('vertical-align')) {
                const defaultStyle = 'vertical-align: super; font-size: 0.8em;';
                el.setAttribute('style', currentStyle ? `${currentStyle}; ${defaultStyle}` : defaultStyle);
            }
        });
    }

    // Render subscript with profile styles
    function renderSubscript(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('sub').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('sub', content.slice(0, 50), usedIds);
            applyStyles(el, 'subscript', elId, profile, overrides, selectable);

            const currentStyle = el.getAttribute('style') || '';
            if (!currentStyle.includes('vertical-align')) {
                const defaultStyle = 'vertical-align: sub; font-size: 0.8em;';
                el.setAttribute('style', currentStyle ? `${currentStyle}; ${defaultStyle}` : defaultStyle);
            }
        });
    }

    // Process formulas inside table cells
    function processFormulasInTableCells(doc) {
        const katexLib = typeof window !== 'undefined' && typeof window.katex !== 'undefined' 
            ? window.katex 
            : (typeof katex !== 'undefined' ? katex : null);
            
        if (!katexLib) return;

        // Find all table cells
        const cells = doc.querySelectorAll('td, th');
        cells.forEach((cell) => {
            const cellText = cell.textContent || '';
            
            // Check if cell contains formula patterns
            const hasBlockFormula = /\\\[|\\\]|\$\$/.test(cellText);
            const hasInlineFormula = /\\\(|\\\)|\$/.test(cellText);
            
            if (hasBlockFormula || hasInlineFormula) {
                // Process block formulas
                let processed = cellText;
                processed = processed.replace(/\\\[([^\]]+)\\\]/g, (_, formula) => {
                    try {
                        return katexLib.renderToString(formula.trim(), { displayMode: true, throwOnError: false });
                    } catch {
                        return formula;
                    }
                });
                processed = processed.replace(/\$\$([^$]+)\$\$/g, (_, formula) => {
                    try {
                        return katexLib.renderToString(formula.trim(), { displayMode: true, throwOnError: false });
                    } catch {
                        return formula;
                    }
                });
                
                // Process inline formulas
                processed = processed.replace(/\\\(([^)]+)\\\)/g, (_, formula) => {
                    try {
                        return katexLib.renderToString(formula.trim(), { displayMode: false, throwOnError: false });
                    } catch {
                        return formula;
                    }
                });
                processed = processed.replace(/\$([^$\n]+)\$/g, (_, formula) => {
                    try {
                        return katexLib.renderToString(formula.trim(), { displayMode: false, throwOnError: false });
                    } catch {
                        return formula;
                    }
                });
                
                // Only update if something changed
                if (processed !== cellText) {
                    cell.innerHTML = processed;
                }
            }
        });
    }

    // Render strikethrough with profile styles
    function renderStrikethrough(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('del, s').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('del', content.slice(0, 50), usedIds);
            applyStyles(el, 'strikethrough', elId, profile, overrides, selectable);

            const currentStyle = el.getAttribute('style') || '';
            if (!currentStyle.includes('text-decoration')) {
                const defaultStyle = 'text-decoration: line-through;';
                el.setAttribute('style', currentStyle ? `${currentStyle}; ${defaultStyle}` : defaultStyle);
            }
        });
    }

    // Render LaTeX formulas using KaTeX (must be loaded in page)
    function renderLatexText(text) {
        var katexLib = typeof window !== 'undefined' && typeof window.katex !== 'undefined' 
            ? window.katex 
            : (typeof katex !== 'undefined' ? katex : null);
            
        if (!katexLib) {
            console.warn('KaTeX not loaded, formulas will not render');
            return text;
        }

        // Block formulas: $$...$$ or \[...\]
        text = text.replace(/\$\$([^$]+)\$\$/g, (_, formula) => {
            try {
                return `<div class="formula-block">${katexLib.renderToString(formula.trim(), { displayMode: true, throwOnError: false })}</div>`;
            } catch {
                return `<div class="formula-block formula-error">${formula}</div>`;
            }
        });

        text = text.replace(/\\\[([^\]]+)\\\]/g, (_, formula) => {
            try {
                return `<div class="formula-block">${katexLib.renderToString(formula.trim(), { displayMode: true, throwOnError: false })}</div>`;
            } catch {
                return `<div class="formula-block formula-error">${formula}</div>`;
            }
        });

        // Inline formulas: $...$ or \(...\)
        text = text.replace(/\$([^$\n]+)\$/g, (_, formula) => {
            try {
                return katexLib.renderToString(formula.trim(), { displayMode: false, throwOnError: false });
            } catch {
                return `<span class="formula-error">${formula}</span>`;
            }
        });

        text = text.replace(/\\\(([^)]+)\\\)/g, (_, formula) => {
            try {
                return katexLib.renderToString(formula.trim(), { displayMode: false, throwOnError: false });
            } catch {
                return `<span class="formula-error">${formula}</span>`;
            }
        });

        return text;
    }

    // Main rendering function
    function renderDocument(markdown, profile, overrides, selectable) {
        if (!markdown || !markdown.trim()) return '';

        // Preprocess markdown
        let preprocessed = markdown;
        preprocessed = preprocessed.replace(/\^\^([^^]+)\^\^/g, '<sup>$1</sup>');
        preprocessed = preprocessed.replace(/~~([^~]+)~~/g, '<del>$1</del>');
        preprocessed = preprocessed.replace(/\[(IMAGE|TABLE|FORMULA)-CAPTION:\s*([^\]]+)\]/g, (match, type, text) => {
            const escapedText = text.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
            return `<x-caption type="${type}" text="${escapedText}"></x-caption>`;
        });
        preprocessed = preprocessed.replace(/==([^=]+)==/g, '<mark>$1</mark>');
        preprocessed = preprocessed.replace(/(?<!~)~([^~\n]+)~(?!~)/g, '<sub>$1</sub>');

        // Process LaTeX
        const contentWithFormulas = renderLatexText(preprocessed);

        // Convert markdown to HTML using markdown-it (must be loaded)
        // markdown-it is loaded from CDN and available as window.markdownit
        if (typeof window !== 'undefined' && typeof window.markdownit !== 'undefined') {
            var MarkdownIt = window.markdownit;
        } else if (typeof markdownit !== 'undefined') {
            var MarkdownIt = markdownit;
        } else {
            console.error('markdown-it not loaded');
            return '';
        }

        const md = new MarkdownIt({
            html: true,
            linkify: true,
            breaks: false
        });

        md.enable(['table']);
        
        // Add plugins if available (they're loaded as global functions)
        if (typeof window !== 'undefined') {
            if (typeof window.markdownitSup !== 'undefined') md.use(window.markdownitSup);
            if (typeof window.markdownitSub !== 'undefined') md.use(window.markdownitSub);
            if (typeof window.markdownitMark !== 'undefined') md.use(window.markdownitMark);
        }

        const rawHtml = md.render(contentWithFormulas);

        // Parse HTML
        const parser = new DOMParser();
        const doc = parser.parseFromString(rawHtml, 'text/html');
        const usedIds = new Set();

        // Restore caption placeholders
        const captionPlaceholders = Array.from(doc.querySelectorAll('x-caption'));
        captionPlaceholders.forEach(placeholder => {
            const type = placeholder.getAttribute('type');
            const escapedText = placeholder.getAttribute('text') || '';
            const text = escapedText.replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/&amp;/g, '&');
            const captionText = `[${type}-CAPTION: ${text}]`;
            const textNode = doc.createTextNode(captionText);
            const parent = placeholder.parentNode;
            if (parent) {
                parent.replaceChild(textNode, placeholder);
            }
        });

        // Process all element types using renderers (order matters - same as frontend)
        // 1. Process formulas first (formula blocks created by renderLatex)
        renderFormulas(doc, usedIds, profile, overrides, selectable);

        // 2. Process paragraphs (must come after formula processing to skip captions)
        renderParagraphs(doc, usedIds, profile, overrides, selectable);

        // 3. Process headings
        renderHeadings(doc, usedIds, profile, overrides, selectable);

        // 4. Process images
        renderImages(doc, usedIds, profile, overrides, selectable);

        // 5. Process lists (unordered, ordered, task lists)
        renderUnorderedLists(doc, usedIds, profile, overrides, selectable);
        renderOrderedLists(doc, usedIds, profile, overrides, selectable);
        renderTaskLists(doc, usedIds, profile, overrides, selectable);

        // 6. Process tables
        renderTables(doc, usedIds, profile, overrides, selectable);
        
        // 6a. Process formulas inside table cells (after tables are rendered)
        processFormulasInTableCells(doc);

        // 7. Process code blocks (must come after other block elements)
        renderCodeBlocks(doc, usedIds, profile, overrides, selectable);
        renderInlineCode(doc, usedIds, profile, overrides, selectable);

        // 8. Process blockquotes
        renderBlockquotes(doc, usedIds, profile, overrides, selectable);

        // 9. Process links
        renderLinks(doc, usedIds, profile, overrides, selectable);

        // 10. Process horizontal rules
        renderHorizontalRules(doc, usedIds, profile, overrides, selectable);

        // 11. Process highlighted text (mark elements)
        renderHighlight(doc, usedIds, profile, overrides, selectable);

        // 12. Process superscript text (sup elements)
        renderSuperscript(doc, usedIds, profile, overrides, selectable);

        // 13. Process subscript text (sub elements)
        renderSubscript(doc, usedIds, profile, overrides, selectable);

        // 14. Process strikethrough text (del elements) - from ~~text~~ preprocessing
        renderStrikethrough(doc, usedIds, profile, overrides, selectable);

        return doc.body.innerHTML;
    }

    // Ensure images inside element are fully decoded / loaded before measuring.
    // Returns a Promise that resolves when all images are decoded or after timeout.
    function ensureImagesLoadedAsync(element, timeoutMs = 5000) {
        const images = Array.from(element.querySelectorAll('img'));
        if (images.length === 0) return Promise.resolve();

        const promises = images.map((img) => {
            try {
                if (img.complete && img.naturalWidth > 0 && img.naturalHeight > 0) {
                    return Promise.resolve();
                }
                if (typeof img.decode === 'function') {
                    return img.decode().catch(() => {
                        return new Promise((res) => {
                            if (img.complete) return res();
                            img.addEventListener('load', () => res(), { once: true });
                            img.addEventListener('error', () => res(), { once: true });
                        });
                    });
                }
                return new Promise((resolve) => {
                    if (img.complete) return resolve();
                    img.addEventListener('load', () => resolve(), { once: true });
                    img.addEventListener('error', () => resolve(), { once: true });
                });
            } catch (e) {
                return Promise.resolve();
            }
        });

        const all = Promise.all(promises);
        const timeout = new Promise((resolve) => setTimeout(resolve, timeoutMs));
        return Promise.race([all, timeout]).then(() => {
            void element.offsetHeight;
        });
    }

    // Split HTML into pages
    async function splitIntoPages(html, pageContentHeight, contentWidth) {
        if (!html || !html.trim()) return [''];

        const tempContainer = document.createElement('div');
        tempContainer.style.position = 'absolute';
        tempContainer.style.visibility = 'hidden';
        tempContainer.style.width = `${contentWidth}px`;
        tempContainer.style.fontFamily = "'Times New Roman', Times, serif";
        tempContainer.style.fontSize = '14pt';
        tempContainer.style.lineHeight = '1.5';
        tempContainer.style.color = '#1a1a1a';
        document.body.appendChild(tempContainer);

        const totalContainer = document.createElement('div');
        totalContainer.style.position = 'absolute';
        totalContainer.style.visibility = 'hidden';
        totalContainer.style.width = `${contentWidth}px`;
        totalContainer.style.fontFamily = "'Times New Roman', Times, serif";
        totalContainer.style.fontSize = '14pt';
        totalContainer.style.lineHeight = '1.5';
        totalContainer.style.color = '#1a1a1a';
        totalContainer.innerHTML = html;
        document.body.appendChild(totalContainer);
        // Wait for images in total container to be decoded so scrollHeight is accurate
        await ensureImagesLoadedAsync(totalContainer);
        const totalHeight = totalContainer.scrollHeight;
        document.body.removeChild(totalContainer);

        if (totalHeight <= pageContentHeight) {
            document.body.removeChild(tempContainer);
            return [html];
        }

        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        const body = doc.body;
        const pages = [];
        const elements = Array.from(body.children);
        let currentPageElements = [];

        const blockElements = new Set(['P', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'UL', 'OL', 'LI', 'TABLE', 'TR', 'THEAD', 'TBODY', 'DIV', 'BLOCKQUOTE', 'PRE', 'HR']);
        
        // Helper function to check if element is an image wrapper or contains images
        function isImageElement(element) {
            return element.getAttribute('data-type') === 'image' ||
                   element.classList.contains('image') ||
                   element.querySelector('img') !== null ||
                   (element.tagName === 'DIV' && element.querySelector('[data-type="image"]') !== null);
        }
        
        // Helper function to ensure images in element are loaded before measurement
        function ensureImagesLoaded(element) {
            const images = element.querySelectorAll('img');
            if (images.length > 0) {
                images.forEach(img => {
                    // Force layout calculation
                    if (img.naturalWidth > 0 && img.naturalHeight > 0) {
                        void img.offsetHeight;
                    } else if (img.complete) {
                        void img.offsetHeight;
                    }
                });
                // Force parent layout recalculation
                void element.offsetHeight;
            }
        }

        for (const element of elements) {
            const elementClone = element.cloneNode(true);
            tempContainer.innerHTML = '';
            tempContainer.appendChild(elementClone);
            // Ensure images inside this cloned element are loaded before measuring
            await ensureImagesLoadedAsync(elementClone);
            
            // Wait for images to load if they exist in the element
            // Images in data URIs should be loaded immediately, but we need to ensure layout is calculated
            const imagesInClone = elementClone.querySelectorAll('img');
            if (imagesInClone.length > 0) {
                // Force layout calculation by accessing offsetHeight and scrollHeight
                // This ensures images are measured with their actual dimensions
                void elementClone.offsetHeight;
                void elementClone.scrollHeight;
                
                // For each image, ensure it's loaded and has dimensions
                imagesInClone.forEach(img => {
                    // If image has natural dimensions, use them
                    if (img.naturalWidth > 0 && img.naturalHeight > 0) {
                        // Image is loaded, dimensions are available
                        // Force recalculation by accessing offsetHeight
                        void img.offsetHeight;
                    } else if (img.complete) {
                        // Image is marked as complete, but may not have natural dimensions yet
                        // Force layout recalculation
                        void img.offsetHeight;
                    }
                });
                
                // Force a layout recalculation after images are processed
                void tempContainer.offsetHeight;
            }
            
            // Measure element height after ensuring images are loaded
            // For image elements, ensure all images are loaded before measurement
            if (isImageElement(elementClone)) {
                await ensureImagesLoadedAsync(elementClone);
            }
            
            // Measure element height including margins
            // Use a wrapper div to measure the total height including margins
            // This is more reliable than trying to parse styles
            const measurementWrapper = document.createElement('div');
            measurementWrapper.style.position = 'absolute';
            measurementWrapper.style.visibility = 'hidden';
            measurementWrapper.style.width = `${contentWidth}px`;
            measurementWrapper.style.margin = '0';
            measurementWrapper.style.padding = '0';
            measurementWrapper.style.border = '0';
            measurementWrapper.appendChild(elementClone.cloneNode(true));
            document.body.appendChild(measurementWrapper);
            
            // Force layout calculation
            void measurementWrapper.offsetHeight;
            
            // Get the actual height including margins using getBoundingClientRect
            const rect = measurementWrapper.getBoundingClientRect();
            const elementHeight = rect.height;

            document.body.removeChild(measurementWrapper);

            // Debug logging for image elements
            if (isImageElement(element)) {
                console.log(`Individual image element: height=${elementHeight}, pageContentHeight=${pageContentHeight}, fits=${elementHeight <= pageContentHeight}`);
            }
            
            // Check if element is a block element or an image wrapper
            const isBlockElement = blockElements.has(element.tagName) ||
                                   element.getAttribute('data-type') === 'image' ||
                                   element.getAttribute('data-type') === 'table' ||
                                   element.classList.contains('image') ||
                                   element.classList.contains('table') ||
                                   isImageElement(element);

            // Special handling for images: if image alone takes more than available page content height, force page break
            const shouldForcePageBreak = isImageElement(element) && elementHeight > pageContentHeight;

            // Additional logic: if current page has content and we're adding an image, check if it fits
            let forceImagePageBreak = false;
            if (isImageElement(element) && currentPageElements.length > 0) {
                // Quick check: if current page content + image would exceed page height
                let currentPageHeight = 0;
                const quickCheckContainer = document.createElement('div');
                quickCheckContainer.style.position = 'absolute';
                quickCheckContainer.style.visibility = 'hidden';
                quickCheckContainer.style.width = `${contentWidth}px`;

                const quickCheckDiv = document.createElement('div');
                currentPageElements.forEach(el => quickCheckDiv.appendChild(el.cloneNode(true)));
                quickCheckContainer.appendChild(quickCheckDiv);
                document.body.appendChild(quickCheckContainer);
                // Wait for images in quick check container to stabilize
                await ensureImagesLoadedAsync(quickCheckContainer);
                currentPageHeight = quickCheckContainer.scrollHeight;
                document.body.removeChild(quickCheckContainer);

                // If current page content + image would exceed available space, put image on new page
                if (currentPageHeight + elementHeight > pageContentHeight) {
                    forceImagePageBreak = true;
                }
            }


            if ((elementHeight > pageContentHeight && isBlockElement) || shouldForcePageBreak || forceImagePageBreak) {
                if (currentPageElements.length > 0) {
                    const pageDiv = document.createElement('div');
                    currentPageElements.forEach(el => pageDiv.appendChild(el.cloneNode(true)));
                    // Add a zero-height spacer to preserve margin-bottom of last element
                    const spacer = document.createElement('div');
                    spacer.style.height = '0';
                    spacer.style.margin = '0';
                    spacer.style.padding = '0';
                    spacer.style.border = '0';
                    spacer.style.overflow = 'hidden';
                    pageDiv.appendChild(spacer);
                    pages.push(pageDiv.innerHTML);
                    currentPageElements = [];
                }
                const pageDiv = document.createElement('div');
                pageDiv.appendChild(element.cloneNode(true));
                // Add a zero-height spacer to preserve margin-bottom
                const spacer = document.createElement('div');
                spacer.style.height = '0';
                spacer.style.margin = '0';
                spacer.style.padding = '0';
                spacer.style.border = '0';
                spacer.style.overflow = 'hidden';
                pageDiv.appendChild(spacer);
                pages.push(pageDiv.innerHTML);
                continue;
            }

            if (currentPageElements.length > 0) {
                    const testContainer = document.createElement('div');
                    testContainer.style.position = 'absolute';
                    testContainer.style.visibility = 'hidden';
                    testContainer.style.width = `${contentWidth}px`;
                    testContainer.style.fontFamily = "'Times New Roman', Times, serif";
                    testContainer.style.fontSize = '14pt';
                    testContainer.style.lineHeight = '1.5';
                    testContainer.style.color = '#1a1a1a';

                    const testPageDiv = document.createElement('div');
                    currentPageElements.forEach(el => testPageDiv.appendChild(el.cloneNode(true)));
                    const elementCloneForTest = element.cloneNode(true);
                    testPageDiv.appendChild(elementCloneForTest);
                    testContainer.appendChild(testPageDiv);
                    document.body.appendChild(testContainer);

                    // Ensure images in the test container are loaded before measurement
                    await ensureImagesLoadedAsync(testContainer);

                // Measure combined height including margins
                // Use getBoundingClientRect to get the actual height including all margins
                const testRect = testContainer.getBoundingClientRect();
                const combinedHeight = testRect.height;
                document.body.removeChild(testContainer);

                // Debug logging for image elements
                if (isImageElement(element)) {
                    console.log(`Combined image measurement: elementHeight=${elementHeight}, combinedHeight=${combinedHeight}, pageContentHeight=${pageContentHeight}, currentPageElements=${currentPageElements.length}, forceImagePageBreak=${forceImagePageBreak}, fits=${combinedHeight <= pageContentHeight}`);
                }

                if (combinedHeight > pageContentHeight) {
                        // Element doesn't fit - save current page
                        // cloneNode(true) preserves all inline styles including margins
                        // To preserve margin-bottom of last element, we need to ensure it's visible
                        // by adding a zero-height spacer element that prevents margin collapsing
                        const pageDiv = document.createElement('div');
                        currentPageElements.forEach(el => pageDiv.appendChild(el.cloneNode(true)));
                        // Add a zero-height spacer to preserve margin-bottom of last element
                        const spacer = document.createElement('div');
                        spacer.style.height = '0';
                        spacer.style.margin = '0';
                        spacer.style.padding = '0';
                        spacer.style.border = '0';
                        spacer.style.overflow = 'hidden';
                        pageDiv.appendChild(spacer);
                        pages.push(pageDiv.innerHTML);
                        currentPageElements = [element];
                    } else {
                        currentPageElements.push(element);
                    }
            } else {
                currentPageElements.push(element);
            }
        }

        // Add remaining elements as last page
        // cloneNode(true) preserves all inline styles including margins
        // Add a zero-height spacer to preserve margin-bottom of last element
        if (currentPageElements.length > 0) {
            const pageDiv = document.createElement('div');
            currentPageElements.forEach(el => pageDiv.appendChild(el.cloneNode(true)));
            // Add a zero-height spacer to preserve margin-bottom of last element
            // This prevents margin collapsing and ensures margin-bottom creates visible space
            const spacer = document.createElement('div');
            spacer.style.height = '0';
            spacer.style.margin = '0';
            spacer.style.padding = '0';
            spacer.style.border = '0';
            spacer.style.overflow = 'hidden';
            pageDiv.appendChild(spacer);
            pages.push(pageDiv.innerHTML);
        }

        document.body.removeChild(tempContainer);
        return pages.length > 0 ? pages : [html];
    }

    // Simple HTML escape function
    function escapeHtml(text) {
        if (!text) return '';
        const div = typeof document !== 'undefined' ? document.createElement('div') : null;
        if (div) {
            div.textContent = text;
            return div.innerHTML;
        }
        // Fallback: manual escaping
        return String(text)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    // Render title page
    function renderTitlePageToHtml(titlePage, variables, pageDimensions) {
        // C# serializes TitlePageData with camelCase (elements) due to JsonNamingPolicy.CamelCase
        // Also handle PascalCase (Elements) and nested format (data.elements) for compatibility
        const elements = titlePage?.elements || titlePage?.Elements || titlePage?.data?.elements;
        if (!elements || !Array.isArray(elements) || elements.length === 0) {
            console.warn('renderTitlePageToHtml: No elements found in title page data');
            return '';
        }

        // Use calculated page dimensions or fallback to A4
        const pageWidthMm = pageDimensions ? (pageDimensions.pageWidth / MM_TO_PX) : A4_WIDTH_MM;
        const pageHeightMm = pageDimensions ? (pageDimensions.pageHeight / MM_TO_PX) : A4_HEIGHT_MM;

        const html = [];
        html.push(`<div class="title-page" style="position: relative; width: ${pageWidthMm}mm; height: ${pageHeightMm}mm; font-family: 'Times New Roman', Times, serif; margin: 0; padding: 0;">`);

        // Create case-insensitive variable lookup map
        // C# serializes with camelCase (Title -> title), but we need to handle all cases
        const variableMap = new Map();
        if (variables) {
            console.log('Received variables object:', JSON.stringify(variables, null, 2));
            for (const [key, value] of Object.entries(variables)) {
                // Store original key
                variableMap.set(key, value || '');
                // Store lowercase version for case-insensitive lookup
                variableMap.set(key.toLowerCase(), value || '');
                // Store capitalized version (Title, Author, etc.)
                if (key.length > 0) {
                    const capitalized = key.charAt(0).toUpperCase() + key.slice(1).toLowerCase();
                    variableMap.set(capitalized, value || '');
                }
            }
            console.log('Variable map created with keys:', Array.from(variableMap.keys()));
            console.log('Variable map entries:', Array.from(variableMap.entries()).map(([k, v]) => `${k}="${v}"`).join(', '));
        } else {
            console.warn('No variables provided to renderTitlePageToHtml');
        }

        // Helper function for case-insensitive variable lookup
        function getVariable(key) {
            if (!key) {
                console.warn('getVariable called with empty key');
                return '';
            }
            // Try multiple formats: exact, lowercase, capitalized
            const normalizedKey = key.trim();
            const lowerKey = normalizedKey.toLowerCase();
            const capitalizedKey = lowerKey.length > 0 ? lowerKey.charAt(0).toUpperCase() + lowerKey.slice(1) : '';
            
            let value = variableMap.get(normalizedKey) || 
                       variableMap.get(lowerKey) || 
                       variableMap.get(capitalizedKey) || 
                       variableMap.get(normalizedKey.toLowerCase()) || '';
            
            console.log(`Variable lookup: key="${key}" (normalized="${normalizedKey}", lower="${lowerKey}", cap="${capitalizedKey}"), found="${value}"`);
            return value;
        }

        for (const element of elements) {
            const positionStyle = `position: absolute; left: ${element.x}mm; top: ${element.y}mm;`;

            if (element.type === 'line') {
                const lineColor = element.stroke || element.color || '#000';
                const lineThickness = element.thickness ?? 1;
                
                // Build line styles - ensure proper isolation from layout flow
                const lineStyles = [];
                lineStyles.push(positionStyle); // position: absolute with coordinates
                lineStyles.push(`border-bottom: ${lineThickness}mm ${element.lineStyle ?? 'solid'} ${lineColor};`);
                
                // Set explicit height to prevent layout issues (at least equal to border thickness)
                lineStyles.push(`height: ${lineThickness}mm;`);
                
                // Calculate width properly - ensure it doesn't exceed page boundaries
                if (element.stretchToPageWidth) {
                    // Calculate width from element.x to right edge of page
                    const widthToRight = Math.max(0, pageWidthMm - element.x);
                    lineStyles.push(`width: ${widthToRight}mm;`);
                    console.log(`Line with stretchToPageWidth: x=${element.x}mm, pageWidth=${pageWidthMm}mm, calculated width=${widthToRight}mm`);
                } else if (element.length) {
                    // Ensure length doesn't exceed page width
                    const maxWidth = Math.max(0, pageWidthMm - element.x);
                    const lineWidth = Math.min(element.length, maxWidth);
                    lineStyles.push(`width: ${lineWidth}mm;`);
                    console.log(`Line with length: x=${element.x}mm, requested length=${element.length}mm, maxWidth=${maxWidth}mm, final width=${lineWidth}mm`);
                } else {
                    // Default width if neither is set - use a small default, but don't exceed page
                    const maxWidth = Math.max(0, pageWidthMm - element.x);
                    const defaultWidth = Math.min(lineThickness * 10, maxWidth);
                    lineStyles.push(`width: ${defaultWidth}mm;`);
                }
                
                // Add properties to isolate line from layout flow completely
                lineStyles.push('box-sizing: border-box;');
                lineStyles.push('overflow: hidden;');
                lineStyles.push('display: block;');
                lineStyles.push('margin: 0;');
                lineStyles.push('padding: 0;');
                
                // Add z-index and isolation to prevent interference with text elements
                lineStyles.push('z-index: 0;'); // Lines should be below text
                lineStyles.push('pointer-events: none;'); // Prevent interaction
                lineStyles.push('isolation: isolate;'); // Create new stacking context
                
                const lineStyleString = lineStyles.join(' ');
                console.log(`Rendering line element: x=${element.x}mm, y=${element.y}mm, style="${lineStyleString}"`);
                html.push(`<div style="${lineStyleString}"></div>`);
            } else {
                let content = '';
                let isEmptyVariable = false;
                
                if (element.type === 'variable') {
                    // Extract variable key: try variableType first, then text (removing {})
                    let variableKey = '';
                    if (element.variableType) {
                        variableKey = element.variableType.trim();
                        console.log(`Variable element: using variableType="${variableKey}"`);
                    } else if (element.text) {
                        variableKey = element.text.replace(/[{}]/g, '').trim();
                        console.log(`Variable element: extracted from text="${element.text}", key="${variableKey}"`);
                    }
                    
                    console.log(`Processing variable element: variableKey="${variableKey}", element:`, JSON.stringify({
                        type: element.type,
                        variableType: element.variableType,
                        text: element.text,
                        format: element.format
                    }));
                    
                    // Get variable value (case-insensitive)
                    const variableValue = variableKey ? getVariable(variableKey) : '';
                    
                    console.log(`Variable value retrieved: key="${variableKey}", value="${variableValue}", isEmpty=${!variableValue || variableValue.trim() === ''}`);
                    
                    // Show placeholder text ONLY if variable is truly empty
                    // Don't show placeholder if we have a value
                    if (!variableValue || variableValue.trim() === '') {
                        const displayKey = variableKey || 'Переменная';
                        content = `${displayKey} не задана`;
                        isEmptyVariable = true;
                        console.warn(`Variable "${variableKey}" is empty, showing placeholder: "${content}"`);
                    } else {
                        console.log(`Variable "${variableKey}" has value: "${variableValue}"`);
                        // If format template is provided, use it
                        if (element.format && variableValue) {
                            // Replace all variable placeholders in format (case-insensitive)
                            content = element.format;
                            // Replace {key}, {Key}, {KEY} patterns - try all variations
                            for (const [key, value] of variableMap.entries()) {
                                // Only process each unique key once (use lowercase as base)
                                if (key.toLowerCase() === key && value) {
                                    const regex = new RegExp(`\\{${key}\\}`, 'gi');
                                    content = content.replace(regex, value);
                                }
                            }
                            console.log(`Applied format template: "${element.format}" -> "${content}"`);
                        } else {
                            content = variableValue;
                            console.log(`Using variable value directly: "${content}"`);
                        }
                    }
                } else {
                    content = element.text || '';
                }

                if (element.allCaps) content = content.toUpperCase();

                // Check if text is multiline
                const lines = content.split('\n');
                const isMultiline = lines.length > 1;

                // For multiline text, render each line separately
                if (isMultiline) {
                    const fontSize = element.fontSize || 16;
                    const lineHeightMultiplier = element.lineHeight || 1.2;
                    const lineHeightPt = fontSize * lineHeightMultiplier;
                    const PT_TO_MM = 0.352778; // Convert pt to mm
                    
                    // Determine container width
                    let containerWidth = element.width;
                    if (!containerWidth && element.textAlign === 'right') {
                        // Calculate width to right edge for right-aligned text
                        containerWidth = pageWidthMm - element.x;
                    }
                    
                    // Render each line separately
                    lines.forEach((line, i) => {
                        const lineTop = element.y + (i * lineHeightPt * PT_TO_MM);
                        const lineStyles = [];
                        lineStyles.push(`position: absolute;`);
                        lineStyles.push(`left: ${element.x}mm;`);
                        lineStyles.push(`top: ${lineTop}mm;`);
                        
                        if (containerWidth) {
                            lineStyles.push(`width: ${containerWidth}mm;`);
                        }
                        
                        if (element.fontFamily) lineStyles.push(`font-family: ${element.fontFamily};`);
                        lineStyles.push(`font-size: ${fontSize}pt;`);
                        if (element.fontWeight) lineStyles.push(`font-weight: ${element.fontWeight};`);
                        if (element.fontStyle) lineStyles.push(`font-style: ${element.fontStyle};`);
                        
                        // Apply text alignment - check both camelCase and PascalCase
                        const textAlign = element.textAlign || element.TextAlign;
                        if (textAlign) {
                            lineStyles.push(`text-align: ${textAlign};`);
                            console.log(`Multiline text: Applied text-align: ${textAlign} for line ${i}`);
                            
                            // For center and right alignment, ensure width is set
                            if ((textAlign === 'center' || textAlign === 'right') && !containerWidth) {
                                if (textAlign === 'center') {
                                    const widthToRight = pageWidthMm - element.x;
                                    const widthToLeft = element.x;
                                    containerWidth = Math.min(widthToRight, widthToLeft) * 2;
                                } else if (textAlign === 'right') {
                                    containerWidth = pageWidthMm - element.x;
                                }
                                lineStyles.push(`width: ${containerWidth}mm;`);
                            }
                        } else {
                            console.warn(`Multiline text: No textAlign found for line ${i}`);
                        }
                        
                        lineStyles.push(`white-space: nowrap;`);
                        lineStyles.push(`overflow: hidden;`);
                        
                        // Ensure text renders above lines (z-index works with position: absolute)
                        lineStyles.push('z-index: 1;');
                        
                        // Apply color - gray italic for empty variables
                        if (isEmptyVariable) {
                            lineStyles.push('color: #999 !important;');
                            if (!element.fontStyle || element.fontStyle === 'normal') {
                                lineStyles.push('font-style: italic;');
                            }
                        } else {
                            const textColor = element.fill || element.color || '#000000';
                            lineStyles.push(`color: ${textColor};`);
                        }
                        
                        const escapedLine = escapeHtml(line);
                        html.push(`<div style="${lineStyles.join(' ')}">${escapedLine}</div>`);
                    });
                } else {
                    // Single-line text
                    const styles = [positionStyle];
                    
                    // Set width if specified
                    if (element.width) {
                        styles.push(`width: ${element.width}mm;`);
                        styles.push(`overflow: hidden;`);
                    } else if (element.textAlign === 'right') {
                        // Calculate width to right edge for right-aligned text
                        const widthToRight = pageWidthMm - element.x;
                        styles.push(`width: ${widthToRight}mm;`);
                        styles.push(`overflow: hidden;`);
                    }
                    
                    // Prevent text wrapping for single-line text
                    styles.push('white-space: nowrap;');
                    
                    // Ensure text renders above lines (z-index works with position: absolute)
                    styles.push('z-index: 1;');
                    
                    // Log alignment for debugging
                    console.log(`Text element alignment: textAlign="${element.textAlign}", element:`, JSON.stringify({
                        type: element.type,
                        text: element.text?.substring(0, 50),
                        textAlign: element.textAlign,
                        x: element.x,
                        width: element.width
                    }));
                    
                    if (element.fontFamily) styles.push(`font-family: ${element.fontFamily};`);
                    if (element.fontSize) styles.push(`font-size: ${element.fontSize}pt;`);
                    if (element.fontWeight) styles.push(`font-weight: ${element.fontWeight};`);
                    if (element.fontStyle) styles.push(`font-style: ${element.fontStyle};`);
                    
                    // Apply text alignment - check both camelCase and PascalCase
                    const textAlign = element.textAlign || element.TextAlign;
                    if (textAlign) {
                        styles.push(`text-align: ${textAlign};`);
                        console.log(`Applied text-align: ${textAlign}`);
                    } else {
                        console.warn('No textAlign found in element');
                    }
                    
                    if (element.lineHeight) styles.push(`line-height: ${element.lineHeight};`);
                    
                    // For center and right alignment, ensure width is set if not already set
                    if (textAlign === 'center' || textAlign === 'right') {
                        if (!element.width && !styles.some(s => s.startsWith('width:'))) {
                            if (textAlign === 'center') {
                                // For center alignment, calculate width from center point to edges
                                const widthToRight = pageWidthMm - element.x;
                                const widthToLeft = element.x;
                                const centerWidth = Math.min(widthToRight, widthToLeft) * 2;
                                styles.push(`width: ${centerWidth}mm;`);
                                console.log(`Calculated center width: ${centerWidth}mm`);
                            } else if (textAlign === 'right') {
                                // Already handled above, but ensure it's there
                                const widthToRight = pageWidthMm - element.x;
                                if (!styles.some(s => s.includes(`width: ${widthToRight}mm`))) {
                                    styles.push(`width: ${widthToRight}mm;`);
                                }
                            }
                            styles.push(`overflow: hidden;`);
                        }
                    }
                    
                    // Apply color - gray italic for empty variables
                    if (isEmptyVariable) {
                        styles.push('color: #999 !important;');
                        if (!element.fontStyle || element.fontStyle === 'normal') {
                            styles.push('font-style: italic;');
                        }
                    } else {
                        const textColor = element.fill || element.color || '#000000';
                        styles.push(`color: ${textColor};`);
                    }

                    const escapedContent = escapeHtml(content);
                    html.push(`<div style="${styles.join(' ')}">${escapedContent}</div>`);
                }
            }
        }

        html.push('</div>');
        return html.join('\n');
    }

    // Main export function
    global.renderDocumentForPdf = async function(documentData, profileData, titlePageData, titlePageVariables) {
        const pageDimensions = calculatePageDimensions(profileData);
        
        // Render title page - pass page dimensions for proper sizing
        let titlePageHtml = '';
        // C# serializes TitlePageData with camelCase (elements) due to JsonNamingPolicy.CamelCase
        // Also handle PascalCase (Elements) and nested format (data.elements) for compatibility
        const hasElements = titlePageData && (
            (titlePageData.elements && Array.isArray(titlePageData.elements) && titlePageData.elements.length > 0) ||
            (titlePageData.Elements && Array.isArray(titlePageData.Elements) && titlePageData.Elements.length > 0) ||
            (titlePageData.data && titlePageData.data.elements && Array.isArray(titlePageData.data.elements) && titlePageData.data.elements.length > 0)
        );
        
        if (hasElements) {
            try {
                const elementCount = titlePageData.elements?.length || titlePageData.Elements?.length || titlePageData.data?.elements?.length || 0;
                console.log('Rendering title page with', elementCount, 'elements');
                titlePageHtml = renderTitlePageToHtml(titlePageData, titlePageVariables || {}, pageDimensions);
                if (!titlePageHtml || titlePageHtml.trim() === '') {
                    console.warn('renderTitlePageToHtml returned empty HTML despite having title page data');
                } else {
                    console.log('Title page HTML generated successfully, length:', titlePageHtml.length);
                }
            } catch (error) {
                console.error('Error rendering title page:', error);
                console.error('Title page data structure:', JSON.stringify(titlePageData, null, 2));
                titlePageHtml = ''; // Continue without title page if rendering fails
            }
        } else if (titlePageData) {
            console.warn('Title page data exists but has no elements or invalid structure.');
            console.warn('Title page data keys:', Object.keys(titlePageData));
            console.warn('Title page data:', JSON.stringify(titlePageData, null, 2));
        }

        // Render document
        console.log('Rendering document content, length:', documentData?.content?.length || 0);
        const renderedHtml = renderDocument(documentData.content || '', profileData, {}, false);
        console.log('Document rendered to HTML, length:', renderedHtml?.length || 0);

        // Calculate content dimensions
        const headerContentHeight = profileData?.pageSettings?.pageNumbers?.enabled && 
            profileData.pageSettings.pageNumbers.position === 'top' 
            ? (profileData.pageSettings.pageNumbers.fontSize || 12) * 1.2 
            : 0;

        const contentHeight = pageDimensions.pageHeight - pageDimensions.marginTop - pageDimensions.marginBottom - headerContentHeight;
        const contentWidth = pageDimensions.pageWidth - pageDimensions.marginLeft - pageDimensions.marginRight;

        console.log('Content dimensions:', { contentHeight, contentWidth, headerContentHeight });

        // Split into pages (wait for images to load during pagination)
        const pages = await splitIntoPages(renderedHtml, contentHeight, contentWidth);
        console.log('Document split into pages:', pages.length, 'pages');
        if (pages.length > 0) {
            console.log('First page HTML length:', pages[0]?.length || 0);
            console.log('Last page HTML length:', pages[pages.length - 1]?.length || 0);
        } else {
            console.error('WARNING: No pages generated from document content!');
            console.error('Rendered HTML:', renderedHtml?.substring(0, 500));
        }

        return {
            titlePageHtml,
            pages,
            pageDimensions
        };
    };

})(typeof window !== 'undefined' ? window : this);