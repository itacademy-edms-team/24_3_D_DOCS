/**
 * PDF Preview Renderer - Ported from frontend
 * This file contains all rendering logic from the frontend to ensure 100% visual consistency
 */

(function(global) {
    'use strict';

    // ============================================================================
    // CONSTANTS
    // ============================================================================
    
    const MM_TO_PX = 3.7795275591;
    const A4_WIDTH_MM = 210;
    const A4_HEIGHT_MM = 297;
    const PAGE_SIZES = {
        A4: { width: 210, height: 297 },
        A5: { width: 148, height: 210 }
    };
    const PT_TO_MM = 25.4 / 72; // Конвертация пунктов в миллиметры: 1pt = 25.4/72mm

    // ============================================================================
    // UTILITY FUNCTIONS
    // ============================================================================

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
        let baseStyle = profile?.entityStyles?.[entityType] || profile?.entityStyles?.[entityType.replace('-', '_')];
        
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
        element.setAttribute('style', styleToCSS(style));
        if (selectable) element.classList.add('element-selectable');
    }

    // ============================================================================
    // PAGE DIMENSIONS
    // ============================================================================

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

    // ============================================================================
    // PAGE SPLITTING
    // ============================================================================

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

    async function splitIntoPages(html, pageContentHeight, contentWidth, options) {
        options = options || {};
        const returnElementPageMap = options.returnElementPageMap || false;

        if (!html.trim()) {
            return returnElementPageMap ? { pages: [''], elementPageMap: {} } : [''];
        }

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
        await ensureImagesLoadedAsync(totalContainer);

        const totalHeight = totalContainer.scrollHeight;
        document.body.removeChild(totalContainer);

        if (totalHeight <= pageContentHeight) {
            document.body.removeChild(tempContainer);
            const elementPageMap = {};
            if (returnElementPageMap) {
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, 'text/html');
                const elements = Array.from(doc.body.children);
                elements.forEach((el, idx) => {
                    elementPageMap[idx] = 0;
                });
                return { pages: [html], elementPageMap };
            }
            return [html];
        }

        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        const body = doc.body;

        const pages = [];
        const elements = Array.from(body.children);
        let currentPageElements = [];
        const elementPageMap = {};
        let nextPageIndex = 0;

        const blockElements = new Set([
            'P', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6',
            'UL', 'OL', 'LI', 'TABLE', 'TR', 'THEAD', 'TBODY',
            'DIV', 'BLOCKQUOTE', 'PRE', 'HR'
        ]);

        function flushPage() {
            if (currentPageElements.length === 0) return;
            currentPageElements.forEach(({ idx }) => {
                elementPageMap[idx] = nextPageIndex;
            });
            const pageDiv = document.createElement('div');
            currentPageElements.forEach(({ el }) => pageDiv.appendChild(el.cloneNode(true)));
            pages.push(pageDiv.innerHTML);
            nextPageIndex++;
            currentPageElements = [];
        }

        for (let idx = 0; idx < elements.length; idx++) {
            const element = elements[idx];
            const elementClone = element.cloneNode(true);
            tempContainer.innerHTML = '';
            tempContainer.appendChild(elementClone);
            await ensureImagesLoadedAsync(elementClone);
            const elementHeight = tempContainer.scrollHeight;

            const isBlockElement = blockElements.has(element.tagName);

            if (elementHeight > pageContentHeight && isBlockElement) {
                if (currentPageElements.length > 0) {
                    flushPage();
                }
                elementPageMap[idx] = nextPageIndex;
                const pageDiv = document.createElement('div');
                pageDiv.appendChild(element.cloneNode(true));
                pages.push(pageDiv.innerHTML);
                nextPageIndex++;
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
                currentPageElements.forEach(({ el }) =>
                    testPageDiv.appendChild(el.cloneNode(true))
                );
                testPageDiv.appendChild(element.cloneNode(true));
                testContainer.appendChild(testPageDiv);
                document.body.appendChild(testContainer);
                await ensureImagesLoadedAsync(testContainer);
                const combinedHeight = testContainer.scrollHeight;
                document.body.removeChild(testContainer);

                if (combinedHeight > pageContentHeight) {
                    flushPage();
                    currentPageElements.push({ el: element, idx });
                } else {
                    currentPageElements.push({ el: element, idx });
                }
            } else {
                currentPageElements.push({ el: element, idx });
            }
        }

        if (currentPageElements.length > 0) {
            flushPage();
        }

        document.body.removeChild(tempContainer);
        const resultPages = pages.length > 0 ? pages : [html];
        if (returnElementPageMap) {
            return { pages: resultPages, elementPageMap };
        }
        return resultPages;
    }

    // ============================================================================
    // LATEX RENDERING
    // ============================================================================

    function renderLatex(text) {
        if (typeof katex === 'undefined') {
            console.warn('KaTeX is not loaded');
            return text;
        }

        // Block formulas: $$...$$ or \[...\]
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

        text = text.replace(/\\\[([^\]]+)\\\]/g, (_, formula) => {
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

        // Inline formulas: $...$ or \(...\)
        text = text.replace(/\$([^$\n]+)\$/g, (_, formula) => {
            try {
                return katex.renderToString(formula.trim(), {
                    displayMode: false,
                    throwOnError: false,
                });
            } catch {
                return `<span class="formula-error">${formula}</span>`;
            }
        });

        text = text.replace(/\\\(([^)]+)\\\)/g, (_, formula) => {
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

    // ============================================================================
    // RENDERERS
    // ============================================================================

    function renderFormulas(doc, usedIds, profile, overrides, selectable) {
        const formulas = Array.from(doc.querySelectorAll('.formula-block'));

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
                    caption.setAttribute('style', styleToCSS(captionStyle));
                    caption.textContent = formattedCaption;
                    if (selectable) caption.classList.add('element-selectable');

                    parent.insertBefore(caption, formulaWrapper.nextSibling);
                }
            }
        });
    }

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

    function calculateHeadingNumbers(headings, settings) {
        const numbers = new Map();
        const counters = [0, 0, 0, 0, 0, 0];

        headings.forEach((heading) => {
            const level = parseInt(heading.tagName[1], 10);
            const levelIndex = level - 1;
            const template = settings?.templates?.[level];

            if (template?.enabled) {
                for (let i = levelIndex + 1; i < 6; i++) {
                    counters[i] = 0;
                }

                counters[levelIndex]++;

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

    function renderImages(doc, usedIds, profile, overrides, selectable) {
        const images = Array.from(doc.querySelectorAll('img'));

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
                                imageParagraph: prevSibling,
                            });
                            break;
                        }
                    }
                    prevSibling = prevSibling.previousElementSibling;
                }
            }
        });

        images.forEach((img) => {
            let src = img.getAttribute('src') || '';
            const alt = img.getAttribute('alt') || '';
            const elId = generateElementId('img', src + alt, usedIds);

            // Note: In PDF generation, images should already have proper URLs
            // We don't need to update tokens here as we're in a server context

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

    const BLOCK_TAGS = new Set([
        'P', 'DIV', 'BLOCKQUOTE', 'PRE', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6',
        'UL', 'OL', 'TABLE', 'FIGURE', 'HR'
    ]);

    function injectMarkerIntoFirstBlock(li, markerText) {
        const firstChild = li.firstElementChild;
        if (!firstChild || !BLOCK_TAGS.has(firstChild.tagName)) {
            return false;
        }
        if (firstChild.tagName === 'INPUT' || firstChild.tagName === 'LABEL') {
            return false;
        }
        const span = li.ownerDocument.createElement('span');
        span.className = 'list-item-marker';
        span.textContent = markerText;
        firstChild.insertBefore(span, firstChild.firstChild);
        return true;
    }

    function getFirstItemTextIndentCm(style, profile) {
        if (style.listUseParagraphTextIndent === true) {
            const paragraphStyle = profile?.entityStyles?.['paragraph'];
            if (paragraphStyle?.textIndent !== undefined && paragraphStyle.textIndent > 0) {
                return paragraphStyle.textIndent;
            }
            return 0;
        }
        if (style.textIndent !== undefined && style.textIndent > 0) {
            return style.textIndent;
        }
        return 0;
    }

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

    function renderUnorderedLists(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('ul').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('ul', content.slice(0, 50), usedIds);
            const style = getFinalStyle('unordered-list', elId, profile, overrides);

            const listStyle = Object.assign({}, style);
            delete listStyle.textIndent;

            el.id = elId;
            el.setAttribute('data-type', 'unordered-list');

            const firstItemTextIndentCm = getFirstItemTextIndentCm(style, profile);
            const listItems = Array.from(el.querySelectorAll(':scope > li'));
            let hasInjectedMarker = false;
            listItems.forEach((li, index) => {
                const injected = injectMarkerIntoFirstBlock(li, '\u2022 ');
                if (injected) hasInjectedMarker = true;

                const nestingLevel = calculateListItemLevel(li);
                const parts = [];

                const indentValue = calculateListIndent(style, profile, nestingLevel);
                if (indentValue !== null && indentValue !== 0) {
                    const indentPt = indentValue * 2.83465;
                    parts.push('margin-left: ' + indentPt + 'pt');
                }

                if (index === 0 && firstItemTextIndentCm > 0) {
                    parts.push('text-indent: ' + firstItemTextIndentCm + 'cm');
                } else {
                    parts.push('text-indent: 0');
                }

                const currentStyle = li.getAttribute('style') || '';
                const newStyle = [currentStyle].concat(parts).filter(Boolean).join('; ').replace(/;+/g, ';').trim();
                li.setAttribute('style', newStyle);
            });

            const listCss = styleToCSS(listStyle) + (hasInjectedMarker ? '; list-style: none' : '; list-style-position: inside');
            el.setAttribute('style', listCss);
            if (selectable) el.classList.add('element-selectable');
        });
    }

    function renderOrderedLists(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('ol').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('ol', content.slice(0, 50), usedIds);
            const style = getFinalStyle('ordered-list', elId, profile, overrides);

            const listStyle = Object.assign({}, style);
            delete listStyle.textIndent;

            el.id = elId;
            el.setAttribute('data-type', 'ordered-list');

            const firstItemTextIndentCm = getFirstItemTextIndentCm(style, profile);
            const start = el.start !== undefined ? el.start : 1;

            const listItems = Array.from(el.querySelectorAll(':scope > li'));
            let hasInjectedMarker = false;
            listItems.forEach((li, index) => {
                const markerNum = start + index;
                const injected = injectMarkerIntoFirstBlock(li, markerNum + '. ');
                if (injected) hasInjectedMarker = true;

                const nestingLevel = calculateListItemLevel(li);
                const parts = [];

                const indentValue = calculateListIndent(style, profile, nestingLevel);
                if (indentValue !== null && indentValue !== 0) {
                    const indentPt = indentValue * 2.83465;
                    parts.push('margin-left: ' + indentPt + 'pt');
                }

                if (index === 0 && firstItemTextIndentCm > 0) {
                    parts.push('text-indent: ' + firstItemTextIndentCm + 'cm');
                } else {
                    parts.push('text-indent: 0');
                }

                const currentStyle = li.getAttribute('style') || '';
                const newStyle = [currentStyle].concat(parts).filter(Boolean).join('; ').replace(/;+/g, ';').trim();
                li.setAttribute('style', newStyle);
            });

            const listCss = styleToCSS(listStyle) + (hasInjectedMarker ? '; list-style: none' : '; list-style-position: inside');
            el.setAttribute('style', listCss);
            if (selectable) el.classList.add('element-selectable');
        });
    }

    function renderTaskLists(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('ul').forEach((ul) => {
            const hasCheckboxes = ul.querySelector('li input[type="checkbox"]');
            if (hasCheckboxes) {
                renderUnorderedLists(doc, usedIds, profile, overrides, selectable);
            }
        });
    }

    function renderTables(doc, usedIds, profile, overrides, selectable) {
        const tables = Array.from(doc.querySelectorAll('table'));

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
            tableWrapper.setAttribute('style', styleToCSS(style));
            if (selectable) tableWrapper.classList.add('element-selectable');

            const parent = table.parentNode;
            if (parent) {
                parent.insertBefore(tableWrapper, table);
                tableWrapper.appendChild(table);

                const tableFinalStyle = Object.assign({}, style);
                delete tableFinalStyle.marginTop;
                delete tableFinalStyle.marginBottom;
                delete tableFinalStyle.marginLeft;
                delete tableFinalStyle.marginRight;
                table.setAttribute('style', styleToCSS(tableFinalStyle) + '; border-collapse: collapse; width: 100%;');

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
                    caption.setAttribute('style', styleToCSS(captionStyle));
                    caption.textContent = formattedCaption;
                    if (selectable) caption.classList.add('element-selectable');

                    parent.insertBefore(caption, tableWrapper.nextSibling);
                }
            }
        });
    }

    function renderCodeBlocks(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('pre > code, pre code').forEach((codeElement) => {
            const preElement = codeElement.parentElement;
            if (!preElement || preElement.tagName !== 'PRE') return;

            const content = codeElement.textContent || '';
            const elId = generateElementId('code', content.slice(0, 50), usedIds);

            applyStyles(preElement, 'code', elId, profile, overrides, selectable);
        });
    }

    function renderInlineCode(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('code').forEach((codeElement) => {
            if (codeElement.parentElement?.tagName === 'PRE') return;

            const content = codeElement.textContent || '';
            const elId = generateElementId('code-inline', content.slice(0, 50), usedIds);

            codeElement.id = elId;
            codeElement.setAttribute('data-type', 'code-inline');
            if (selectable) codeElement.classList.add('element-selectable');

            const style = profile?.entityStyles?.['code-inline'] || {};
            if (style.backgroundColor) {
                codeElement.setAttribute('style', `background-color: ${style.backgroundColor}; padding: 2px 4px; border-radius: 3px;`);
            }
        });
    }

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

            if (styleParts.length > 0) {
                styleParts.push('padding: 2px 4px; border-radius: 2px');
                el.setAttribute('style', currentStyle ? `${currentStyle}; ${styleParts.join('; ')}` : styleParts.join('; '));
            } else if (!currentStyle.includes('background-color')) {
                el.setAttribute('style', 'background-color: #ffeb3b; padding: 2px 4px; border-radius: 2px;');
            }
        });
    }

    function renderSuperscript(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('sup').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('sup', content.slice(0, 50), usedIds);
            applyStyles(el, 'superscript', elId, profile, overrides, selectable);

            const currentStyle = el.getAttribute('style') || '';
            if (!currentStyle.includes('vertical-align')) {
                el.setAttribute('style', currentStyle ? `${currentStyle}; vertical-align: super; font-size: 0.8em;` : 'vertical-align: super; font-size: 0.8em;');
            }
        });
    }

    function renderSubscript(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('sub').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('sub', content.slice(0, 50), usedIds);
            applyStyles(el, 'subscript', elId, profile, overrides, selectable);

            const currentStyle = el.getAttribute('style') || '';
            if (!currentStyle.includes('vertical-align')) {
                el.setAttribute('style', currentStyle ? `${currentStyle}; vertical-align: sub; font-size: 0.8em;` : 'vertical-align: sub; font-size: 0.8em;');
            }
        });
    }

    function renderStrikethrough(doc, usedIds, profile, overrides, selectable) {
        doc.querySelectorAll('del, s').forEach((el) => {
            const content = el.textContent || '';
            const elId = generateElementId('del', content.slice(0, 50), usedIds);
            applyStyles(el, 'strikethrough', elId, profile, overrides, selectable);

            const currentStyle = el.getAttribute('style') || '';
            if (!currentStyle.includes('text-decoration')) {
                el.setAttribute('style', currentStyle ? `${currentStyle}; text-decoration: line-through;` : 'text-decoration: line-through;');
            }
        });
    }

    // ============================================================================
    // DOCUMENT RENDERING
    // ============================================================================

    function renderDocument(markdown, profile, overrides, selectable) {
        if (!markdown || !markdown.trim()) {
            return '';
        }

        let preprocessedMarkdown = markdown;
        
        preprocessedMarkdown = preprocessedMarkdown.replace(/\^\^([^^]+)\^\^/g, '<sup>$1</sup>');
        preprocessedMarkdown = preprocessedMarkdown.replace(/~~([^~]+)~~/g, '<del>$1</del>');

        preprocessedMarkdown = preprocessedMarkdown.replace(
            /\[(IMAGE|TABLE|FORMULA)-CAPTION:\s*([^\]]+)\]/g,
            (match, type, text) => {
                const escapedText = text
                    .replace(/&/g, '&amp;')
                    .replace(/"/g, '&quot;')
                    .replace(/'/g, '&#39;');
                return `<x-caption type="${type}" text="${escapedText}"></x-caption>`;
            }
        );

        const contentWithFormulas = renderLatex(preprocessedMarkdown);

        if (typeof markdownit === 'undefined') {
            console.warn('markdown-it is not loaded');
            return contentWithFormulas;
        }

        const md = new markdownit({
            html: true,
            linkify: true,
            breaks: false,
        });

        md.enable(['table']);

        if (typeof markdownItSup !== 'undefined') md.use(markdownItSup);
        if (typeof markdownItSub !== 'undefined') md.use(markdownItSub);
        if (typeof markdownItMark !== 'undefined') md.use(markdownItMark);

        const rawHtml = md.render(contentWithFormulas);

        const parser = new DOMParser();
        const doc = parser.parseFromString(rawHtml, 'text/html');
        const usedIds = new Set();

        const captionPlaceholders = Array.from(doc.querySelectorAll('x-caption'));
        captionPlaceholders.forEach((placeholder) => {
            const type = placeholder.getAttribute('type');
            const escapedText = placeholder.getAttribute('text') || '';
            
            const text = escapedText
                .replace(/&quot;/g, '"')
                .replace(/&#39;/g, "'")
                .replace(/&amp;/g, '&');
            
            const captionText = `[${type}-CAPTION: ${text}]`;
            const textNode = doc.createTextNode(captionText);
            const parent = placeholder.parentNode;
            if (parent) {
                parent.replaceChild(textNode, placeholder);
            }
        });

        renderFormulas(doc, usedIds, profile, overrides, selectable);
        renderParagraphs(doc, usedIds, profile, overrides, selectable);
        renderHeadings(doc, usedIds, profile, overrides, selectable);
        renderImages(doc, usedIds, profile, overrides, selectable);
        renderUnorderedLists(doc, usedIds, profile, overrides, selectable);
        renderOrderedLists(doc, usedIds, profile, overrides, selectable);
        renderTaskLists(doc, usedIds, profile, overrides, selectable);
        renderTables(doc, usedIds, profile, overrides, selectable);
        renderCodeBlocks(doc, usedIds, profile, overrides, selectable);
        renderInlineCode(doc, usedIds, profile, overrides, selectable);
        renderBlockquotes(doc, usedIds, profile, overrides, selectable);
        renderLinks(doc, usedIds, profile, overrides, selectable);
        renderHorizontalRules(doc, usedIds, profile, overrides, selectable);
        renderHighlight(doc, usedIds, profile, overrides, selectable);
        renderSuperscript(doc, usedIds, profile, overrides, selectable);
        renderSubscript(doc, usedIds, profile, overrides, selectable);
        renderStrikethrough(doc, usedIds, profile, overrides, selectable);

        return doc.body.innerHTML;
    }

    // ============================================================================
    // TITLE PAGE RENDERING
    // ============================================================================

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function measureTextWidth(text, fontSize, fontFamily, fontWeight, fontStyle) {
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        if (!ctx) return 0;
        
        ctx.font = `${fontStyle} ${fontWeight} ${fontSize}pt ${fontFamily}`;
        const metrics = ctx.measureText(text);
        return metrics.width;
    }

    function renderTitlePageElement(element, variables) {
        const positionStyle = `position: absolute; left: ${element.x}mm; top: ${element.y}mm;`;
        
        if (element.type === 'line') {
            const lineColor = element.stroke || element.color || '#000';
            let lineStyle = `border-bottom: ${element.thickness ?? 1}mm ${element.lineStyle ?? 'solid'} ${lineColor};`;
            
            if (element.stretchToPageWidth) {
                lineStyle += ' width: 100%;';
            } else if (element.length) {
                lineStyle += ` width: ${element.length}mm;`;
            }
            
            return `<div style="${positionStyle} ${lineStyle}"></div>`;
        }
        
        let content = '';
        let isEmptyVariable = false;
        
        if (element.type === 'variable') {
            const variableKey = element.text 
                ? element.text.replace(/[{}]/g, '').trim() 
                : (element.variableType || '');
            
            const variableValue = variableKey ? (variables[variableKey] || '') : '';
            
            if (!variableValue) {
                const displayKey = variableKey || 'Переменная';
                content = `${displayKey} не задана`;
                isEmptyVariable = true;
            } else {
                if (element.format && variableValue) {
                    content = element.format.replace(/\{(\w+)\}/g, (match, key) => {
                        return variables[key] || match;
                    });
                } else {
                    content = variableValue;
                }
            }
        } else {
            content = element.text || '';
        }
        
        if (element.allCaps) {
            content = content.toUpperCase();
        }
        
        const lines = content.split('\n');
        const isMultiline = lines.length > 1;
        
        if (element.maxLines && element.maxLines > 0) {
            const styles = [positionStyle];
            styles.push(`display: -webkit-box;`);
            styles.push(`-webkit-line-clamp: ${element.maxLines};`);
            styles.push(`-webkit-box-orient: vertical;`);
            styles.push(`overflow: hidden;`);
            
            if (element.width) {
                styles.push(`width: ${element.width}mm;`);
            } else if (element.textAlign === 'right') {
                const widthToRight = A4_WIDTH_MM - element.x;
                styles.push(`width: ${widthToRight}mm;`);
            }
            
            if (element.height) {
                styles.push(`height: ${element.height}mm;`);
            }
            if (element.fontFamily) {
                styles.push(`font-family: ${element.fontFamily};`);
            }
            if (element.fontSize) {
                styles.push(`font-size: ${element.fontSize}pt;`);
            }
            if (element.fontWeight) {
                styles.push(`font-weight: ${element.fontWeight};`);
            }
            if (element.fontStyle) {
                styles.push(`font-style: ${element.fontStyle};`);
            }
            if (element.textAlign) {
                styles.push(`text-align: ${element.textAlign};`);
            }
            if (element.lineHeight) {
                styles.push(`line-height: ${element.lineHeight};`);
            }
            
            if (isEmptyVariable) {
                styles.push('color: #999 !important;');
                if (!element.fontStyle || element.fontStyle === 'normal') {
                    styles.push('font-style: italic;');
                }
            } else {
                const textColor = element.fill || element.color || '#000000';
                styles.push(`color: ${textColor} !important;`);
            }
            
            const styleString = styles.join(' ');
            return `<div style="${styleString}">${escapeHtml(content)}</div>`;
        }
        
        if (isMultiline) {
            const fontSize = element.fontSize || 16;
            const lineHeightMultiplier = element.lineHeight || 1.2;
            const fontSizePt = fontSize;
            const lineHeightPt = fontSizePt * lineHeightMultiplier;
            const textAlign = element.textAlign || 'left';
            const fontFamily = element.fontFamily || 'Times New Roman';
            const fontWeight = element.fontWeight || 'normal';
            const fontStyle = element.fontStyle || 'normal';
            
            const containerLeft = element.x;
            
            let containerWidth = element.width;
            if (!containerWidth) {
                let maxWidth = 0;
                lines.forEach(line => {
                    const lineWidth = measureTextWidth(line, fontSize, fontFamily, fontWeight, fontStyle);
                    if (lineWidth > maxWidth) maxWidth = lineWidth;
                });
                containerWidth = maxWidth / MM_TO_PX;
            }
            
            const linesHtml = lines.map((line, i) => {
                const lineTop = element.y + (i * lineHeightPt * PT_TO_MM);
                
                const lineStyles = [];
                lineStyles.push(`position: absolute;`);
                lineStyles.push(`left: ${containerLeft}mm;`);
                lineStyles.push(`top: ${lineTop}mm;`);
                lineStyles.push(`width: ${containerWidth}mm;`);
                
                if (fontFamily) {
                    lineStyles.push(`font-family: ${fontFamily};`);
                }
                lineStyles.push(`font-size: ${fontSizePt}pt;`);
                if (fontWeight) {
                    lineStyles.push(`font-weight: ${fontWeight};`);
                }
                if (fontStyle) {
                    lineStyles.push(`font-style: ${fontStyle};`);
                }
                lineStyles.push(`text-align: ${textAlign};`);
                lineStyles.push(`white-space: nowrap;`);
                lineStyles.push(`overflow: hidden;`);
                
                if (isEmptyVariable) {
                    lineStyles.push('color: #999 !important;');
                    if (!fontStyle || fontStyle === 'normal') {
                        lineStyles.push('font-style: italic;');
                    }
                } else {
                    const textColor = element.fill || element.color || '#000000';
                    lineStyles.push(`color: ${textColor} !important;`);
                }
                
                const lineStyleString = lineStyles.join(' ');
                return `<div style="${lineStyleString}">${escapeHtml(line)}</div>`;
            }).join('\n');
            
            return linesHtml;
        }
        
        const styles = [];
        styles.push(`position: absolute; left: ${element.x}mm; top: ${element.y}mm;`);
        
        if (element.width) {
            styles.push(`width: ${element.width}mm;`);
            styles.push(`overflow: hidden;`);
        }
        
        if (element.textAlign) {
            styles.push(`text-align: ${element.textAlign};`);
        }
        
        styles.push('white-space: nowrap;');
        
        if (element.height) {
            styles.push(`height: ${element.height}mm;`);
        }
        if (element.fontFamily) {
            styles.push(`font-family: ${element.fontFamily};`);
        }
        if (element.fontSize) {
            styles.push(`font-size: ${element.fontSize}pt;`);
        }
        if (element.fontWeight) {
            styles.push(`font-weight: ${element.fontWeight};`);
        }
        if (element.fontStyle) {
            styles.push(`font-style: ${element.fontStyle};`);
        }
        if (element.lineHeight) {
            styles.push(`line-height: ${element.lineHeight};`);
        }
        
        if (isEmptyVariable) {
            styles.push('color: #999 !important;');
            if (!element.fontStyle || element.fontStyle === 'normal') {
                styles.push('font-style: italic;');
            }
        } else {
            const textColor = element.fill || element.color || '#000000';
            styles.push(`color: ${textColor} !important;`);
        }
        
        const styleString = styles.join(' ');
        return `<div style="${styleString}">${escapeHtml(content)}</div>`;
    }

    function renderTableOfContentsToHtml(tocItems, settings, headingPageMapByIndex, pageOffset) {
        if (!tocItems || tocItems.length === 0) return '';
        const s = settings || {};
        const fontStyle = s.fontStyle || 'normal';
        const fontWeight = s.fontWeight || 'normal';
        const fontSize = s.fontSize || 14;
        const indentPerLevel = (s.indentPerLevel || 5) * MM_TO_PX;
        const nestingEnabled = s.nestingEnabled !== false;
        const numberingEnabled = s.numberingEnabled !== false;

        const counters = [0, 0, 0, 0, 0, 0];
        const lines = [];

        lines.push('<div class="toc-wrapper" style="font-family: \'Times New Roman\', Times, serif; width: 100%; max-width: 100%; overflow: hidden; box-sizing: border-box;">');
        lines.push(`<div style="text-align: center; font-size: ${fontSize + 2}pt; font-weight: bold; margin-bottom: 16pt;">СОДЕРЖАНИЕ</div>`);

        tocItems.forEach((item, idx) => {
            const level = Math.max(1, Math.min(6, item.level || 1));
            let text = item.text || '';
            const pageNum = headingPageMapByIndex[idx] !== undefined
                ? pageOffset + headingPageMapByIndex[idx]
                : (item.pageNumber || 0);
            const displayPage = pageNum > 0 ? String(pageNum) : '';

            let numberPrefix = '';
            if (numberingEnabled) {
                for (let i = level; i < 6; i++) counters[i] = 0;
                counters[level - 1]++;
                const parts = [];
                for (let i = 0; i < level; i++) parts.push(Math.max(1, counters[i]));
                numberPrefix = parts.join('.') + ' ';
            }

            const displayText = numberPrefix + text;
            const textWithFormulas = typeof renderLatex === 'function' ? renderLatex(displayText) : escapeHtml(displayText);

            const indent = nestingEnabled ? (level - 1) * indentPerLevel : 0;
            const marginLeft = indent > 0 ? `${indent}px` : '0';
            const lineStyle = `font-size: ${fontSize}pt; font-style: ${fontStyle}; font-weight: ${fontWeight}; margin-left: ${marginLeft};`;
            const dotChar = '\u00B7';
            const dotsContent = dotChar.repeat(300);
            const letterSpacingEm = 0.15;
            const gapToPageEm = letterSpacingEm * 3;
            const dotsStyle = `font-size: ${fontSize}pt; font-weight: ${fontWeight}; color: #333; letter-spacing: ${letterSpacingEm}em; overflow: hidden; white-space: nowrap;`;
            lines.push(`<div class="toc-line" style="${lineStyle} display: flex; align-items: flex-end; margin-bottom: 4pt; min-width: 0;">`);
            lines.push(`<span class="toc-text" style="flex: 0 1 auto; min-width: 0; max-width: 60%; overflow-wrap: break-word; word-break: break-word;">${textWithFormulas}</span>`);
            lines.push(`<span class="toc-dots" style="flex: 1 0 2em; min-width: 2em; padding: 0 ${gapToPageEm}em 0 6px; ${dotsStyle}">${escapeHtml(dotsContent)}</span>`);
            lines.push(`<span class="toc-page" style="flex-shrink: 0; margin-left: ${gapToPageEm}em;">${escapeHtml(displayPage)}</span>`);
            lines.push('</div>');
        });

        lines.push('</div>');
        return lines.join('\n');
    }

    function renderTitlePageToHtml(titlePage, variables) {
        if (!titlePage?.data?.elements) {
            return '';
        }
        
        const html = [];
        html.push(`<div class="title-page" style="position: relative; width: ${A4_WIDTH_MM}mm; height: ${A4_HEIGHT_MM}mm; font-family: 'Times New Roman', Times, serif; margin: 0; padding: 0;">`);
        
        for (const element of titlePage.data.elements) {
            html.push(renderTitlePageElement(element, variables));
        }
        
        html.push('</div>');
        
        return html.join('\n');
    }

    // ============================================================================
    // MAIN RENDERING FUNCTION
    // ============================================================================

    async function renderPreview(data) {
        const {
            markdown,
            profile,
            titlePage,
            titlePageVariables,
            overrides,
            baseUrl,
            tableOfContents,
            tableOfContentsSettings
        } = data;

        const root = document.getElementById('document-preview-root');
        if (!root) {
            console.error('Root element not found');
            return;
        }

        root.innerHTML = '';
        root.className = 'document-preview';

        const container = document.createElement('div');
        container.className = 'document-preview__container';
        root.appendChild(container);

        const dimensions = calculatePageDimensions(profile);
        const pageNumbers = profile?.pageSettings?.pageNumbers;

        // Отладка конвертированных размеров в пикселях
        console.log(`Calculated page dimensions (px): width=${dimensions.pageWidth}, height=${dimensions.pageHeight}`);
        console.log(`MM_TO_PX constant: ${MM_TO_PX}`);

        // Add @page CSS rule for PDF generation
        const pageSize = profile?.pageSettings?.size || 'A4';
        const isLandscape = profile?.pageSettings?.orientation === 'landscape';
        const pageSizeMm = PAGE_SIZES[pageSize] || PAGE_SIZES.A4;
        const pageWidthMm = isLandscape ? pageSizeMm.height : pageSizeMm.width;
        const pageHeightMm = isLandscape ? pageSizeMm.width : pageSizeMm.height;

        // Отладка физического размера страницы
        console.log(`Physical page size (@page): ${pageWidthMm}mm x ${pageHeightMm}mm`);

        const style = document.createElement('style');
        style.textContent = `
            @page {
                size: ${pageWidthMm}mm ${pageHeightMm}mm;
                margin: 0;
            }
        `;
        document.head.appendChild(style);

        // Render title page if provided
        let titlePageHtml = '';
        if (titlePage) {
            titlePageHtml = renderTitlePageToHtml(titlePage, titlePageVariables || {});
        }

        // Render document
        const renderedHtml = renderDocument(markdown || '', profile, overrides || {}, false);
        
        const headerContentHeight = (pageNumbers?.enabled && pageNumbers.position === 'top')
            ? (pageNumbers.fontSize || 12) * 1.2
            : 0;
        
        const footerContentHeight = (pageNumbers?.enabled && pageNumbers.position === 'bottom')
            ? (pageNumbers.fontSize || 12) * 1.2
            : 0;

        const contentHeight = dimensions.pageHeight
            - dimensions.marginTop
            - dimensions.marginBottom
            - headerContentHeight
            - footerContentHeight;
        const contentWidth = dimensions.pageWidth - dimensions.marginLeft - dimensions.marginRight;

        let pages;
        let headingPageMapByIndex = [];
        const hasToc = tableOfContents && Array.isArray(tableOfContents) && tableOfContents.length > 0;

        if (hasToc) {
            const splitResult = await splitIntoPages(renderedHtml, contentHeight, contentWidth, { returnElementPageMap: true });
            pages = splitResult.pages;
            const elementPageMap = splitResult.elementPageMap || {};
            const parser = new DOMParser();
            const doc = parser.parseFromString(renderedHtml, 'text/html');
            const elements = Array.from(doc.body.children);
            elements.forEach((el, idx) => {
                if (/^H[1-6]$/.test(el.tagName)) {
                    headingPageMapByIndex.push(elementPageMap[idx] !== undefined ? elementPageMap[idx] : 0);
                }
            });
        } else {
            pages = await splitIntoPages(renderedHtml, contentHeight, contentWidth);
        }

        let tocPages = [];
        let tocPageCount = 0;
        if (hasToc && tableOfContents.length > 0) {
            const tocSettings = tableOfContentsSettings || {};
            const tocHtml = renderTableOfContentsToHtml(tableOfContents, tocSettings, headingPageMapByIndex, 0);
            tocPages = await splitIntoPages(tocHtml, contentHeight, contentWidth);
            tocPageCount = tocPages.length;
            const pageOffset = 1 + (titlePageHtml ? 1 : 0) + tocPageCount;
            const tocHtmlWithPages = renderTableOfContentsToHtml(tableOfContents, tocSettings, headingPageMapByIndex, pageOffset);
            tocPages = await splitIntoPages(tocHtmlWithPages, contentHeight, contentWidth);
        }

        const showHeader = pageNumbers?.enabled && pageNumbers.position === 'top';
        const showFooter = pageNumbers?.enabled && pageNumbers.position === 'bottom';
        const totalPages = (titlePageHtml ? 1 : 0) + tocPageCount + pages.length;

        // Render title page
        if (titlePageHtml) {
            const titlePageDiv = document.createElement('div');
            titlePageDiv.className = 'document-preview__page document-preview__page--title';
            // Синхронизируем размеры титульника с остальными страницами контента.
            // Раньше титульник считался в mm (A4_WIDTH_MM * MM_TO_PX), но остальные страницы -
            // в px через dimensions.pageWidth/pageHeight. Из-за этого возникал сдвиг в начале
            // документа: титульник занимал ровно 1 лист, а первая страница контента начиналась
            // с небольшим смещением (полоса сверху), что запускало каскад смещений футеров.
            titlePageDiv.style.width = `${dimensions.pageWidth}px`;
            titlePageDiv.style.height = `${dimensions.pageHeight}px`;
            titlePageDiv.style.margin = '0 auto';
            titlePageDiv.style.background = 'white';
            titlePageDiv.style.boxShadow = '0 2px 8px rgba(0,0,0,0.3)';
            titlePageDiv.style.pageBreakAfter = 'always';
            titlePageDiv.style.pageBreakInside = 'avoid';
            titlePageDiv.style.position = 'relative';
            titlePageDiv.style.overflow = 'hidden';

            const titlePageContent = document.createElement('div');
            titlePageContent.className = 'document-preview__title-page-content';
            titlePageContent.innerHTML = titlePageHtml;
            titlePageDiv.appendChild(titlePageContent);

            container.appendChild(titlePageDiv);
        }

        // Render TOC pages
        if (tocPages.length > 0) {
            tocPages.forEach((tocPageHtml, tocIndex) => {
                const pageDiv = document.createElement('div');
                pageDiv.className = 'document-preview__page document-preview__page--toc';
                pageDiv.style.width = `${dimensions.pageWidth}px`;
                pageDiv.style.height = `${dimensions.pageHeight}px`;
                pageDiv.style.margin = '0 auto';
                pageDiv.style.background = 'white';
                pageDiv.style.boxShadow = '0 2px 8px rgba(0,0,0,0.3)';
                pageDiv.style.pageBreakAfter = 'always';
                pageDiv.style.pageBreakInside = 'avoid';
                pageDiv.style.position = 'relative';
                pageDiv.style.overflow = 'hidden';

                const content = document.createElement('div');
                content.className = 'document-preview__content';
                content.style.padding = `${dimensions.marginTop}px ${dimensions.marginRight}px ${dimensions.marginBottom}px ${dimensions.marginLeft}px`;
                content.style.width = '100%';
                content.style.height = '100%';
                content.style.boxSizing = 'border-box';
                content.style.pageBreakInside = 'avoid';
                content.innerHTML = tocPageHtml;

                if (showFooter) {
                    const pageNumber = (titlePageHtml ? 1 : 0) + tocIndex + 1;
                    const format = (pageNumbers.format || '{n}')
                        .replace('{n}', String(pageNumber))
                        .replace('{total}', String(totalPages));
                    const footer = document.createElement('div');
                    footer.className = 'document-preview__footer';
                    footer.style.position = 'absolute';
                    footer.style.bottom = '0';
                    footer.style.left = '0';
                    footer.style.right = '0';
                    footer.style.height = `${dimensions.marginBottom}px`;
                    footer.style.display = 'flex';
                    footer.style.flexDirection = 'column';
                    footer.style.justifyContent = 'flex-end';
                    footer.style.alignItems = 'center';
                    footer.innerHTML = `<div style="font-size: ${pageNumbers.fontSize || 12}pt;">${format}</div>`;
                    pageDiv.appendChild(content);
                    pageDiv.appendChild(footer);
                } else {
                    pageDiv.appendChild(content);
                }
                container.appendChild(pageDiv);
            });
        }

        // Render document pages
        const contentPageOffset = (titlePageHtml ? 1 : 0) + tocPageCount;
        pages.forEach((pageHtml, index) => {
            const pageDiv = document.createElement('div');
            pageDiv.className = 'document-preview__page';
            pageDiv.style.width = `${dimensions.pageWidth}px`;
            pageDiv.style.height = `${dimensions.pageHeight}px`;
            // Для корректного соответствия виртуальной страницы физическому листу
            // убираем внешние отступы между страницами при генерации PDF.
            // Ранее у нас стоял '20px' сверху у всех страниц, кроме первой,
            // что создавалo видимую полосу (ровно высоте номера страницы) на второй странице.
            pageDiv.style.margin = '0 auto';
            pageDiv.style.background = 'white';
            pageDiv.style.boxShadow = '0 2px 8px rgba(0,0,0,0.3)';
            pageDiv.style.pageBreakAfter = 'always';
            pageDiv.style.pageBreakInside = 'avoid';
            pageDiv.style.position = 'relative';
            pageDiv.style.overflow = 'hidden';
            pageDiv.style.padding = '0'; // Ensure no padding
            pageDiv.style.marginTop = '0';
            pageDiv.style.marginBottom = '0';
            pageDiv.style.marginLeft = 'auto';
            pageDiv.style.marginRight = 'auto';

            // Отладка размеров первой страницы контента
            if (index === 0) {
                console.log(`First content page rendered: width=${dimensions.pageWidth}px, height=${dimensions.pageHeight}px, marginTop=${pageDiv.style.marginTop}`);
            }

            // Header
            if (showHeader) {
                const header = document.createElement('div');
                header.className = 'document-preview__header';
                header.style.position = 'absolute';
                header.style.top = '0';
                header.style.left = '0';
                header.style.right = '0';
                header.style.padding = `${dimensions.marginTop}px 0 0 0`;
                header.style.width = '100%';
                header.style.zIndex = '1';
                header.style.boxShadow = 'none'; // Remove shadow from header
                header.style.pageBreakInside = 'avoid'; // Prevent header from breaking across pages
                header.style.pageBreakAfter = 'avoid'; // Prevent page break after header
                header.style.background = 'transparent'; // Ensure no background

                const pageNumber = index + 1 + contentPageOffset;
                const format = (pageNumbers.format || '{n}')
                    .replace('{n}', String(pageNumber))
                    .replace('{total}', String(totalPages));

                const fontSize = pageNumbers.fontSize || 12;
                const fontFamily = pageNumbers.fontFamily ? `font-family: ${pageNumbers.fontFamily};` : '';
                const fontStyle = pageNumbers.fontStyle ? `font-style: ${pageNumbers.fontStyle};` : '';
                const textAlign = `text-align: ${pageNumbers.align || 'center'};`;

                header.innerHTML = `<div style="${fontFamily} ${fontStyle} font-size: ${fontSize}pt; ${textAlign} width: 100%; color: #000;">${format}</div>`;
                pageDiv.appendChild(header);
            }

            // Content
            const content = document.createElement('div');
            content.className = 'document-preview__content';
            // Padding top: marginTop + header height if header is shown
            const paddingTop = showHeader
                ? dimensions.marginTop + headerContentHeight
                : dimensions.marginTop;
            // Padding bottom: marginBottom + footer height if footer is shown (to prevent overlap)
            const paddingBottom = showFooter
                ? dimensions.marginBottom + footerContentHeight
                : dimensions.marginBottom;
            content.style.width = '100%';
            content.style.height = '100%';
            content.style.padding = `${paddingTop}px ${dimensions.marginRight}px ${paddingBottom}px ${dimensions.marginLeft}px`;
            content.style.fontFamily = "'Times New Roman', Times, serif";
            content.style.fontSize = '14pt';
            content.style.lineHeight = '1.5';
            content.style.color = '#1a1a1a';
            content.style.boxSizing = 'border-box';
            content.style.pageBreakInside = 'avoid';
            content.style.position = 'relative'; // Ensure content is positioned relative to page
            content.innerHTML = pageHtml;

            // Convert relative image URLs to absolute if baseUrl is provided
            if (baseUrl) {
                const images = content.querySelectorAll('img');
                images.forEach((img) => {
                    let src = img.getAttribute('src') || '';
                    if (src && !src.startsWith('http://') && !src.startsWith('https://') && !src.startsWith('data:')) {
                        if (src.startsWith('/')) {
                            src = baseUrl + src;
                        } else {
                            src = baseUrl + '/' + src;
                        }
                        img.setAttribute('src', src);
                    }
                });
            }

            pageDiv.appendChild(content);

            // Footer - append after content so it's on top in z-index
            if (showFooter) {
                // Отладка футера
                if (index === 0) {
                    console.log(`Adding footer to first content page (page ${index + 1 + (titlePageHtml ? 1 : 0)}), showFooter=${showFooter}, marginBottom=${dimensions.marginBottom}px`);
                }

                const footer = document.createElement('div');
                footer.className = 'document-preview__footer';
                footer.style.position = 'absolute';
                footer.style.bottom = '0';
                footer.style.left = '0';
                footer.style.right = '0';
                footer.style.height = `${dimensions.marginBottom}px`;
                footer.style.minHeight = `${dimensions.marginBottom}px`;
                footer.style.width = '100%';
                footer.style.display = 'flex';
                footer.style.flexDirection = 'column';
                footer.style.justifyContent = 'flex-end';
                footer.style.alignItems = 'center';
                footer.style.zIndex = '10'; // Higher z-index to ensure it's on top
                footer.style.boxShadow = 'none'; // Remove shadow from footer
                footer.style.pageBreakInside = 'avoid'; // Prevent footer from breaking across pages
                footer.style.pageBreakAfter = 'avoid'; // Prevent page break after footer
                footer.style.background = 'transparent'; // Ensure no background
                footer.style.pointerEvents = 'none'; // Allow clicks to pass through

                const pageNumber = index + 1 + contentPageOffset;
                const format = (pageNumbers.format || '{n}')
                    .replace('{n}', String(pageNumber))
                    .replace('{total}', String(totalPages));

                const fontSize = pageNumbers.fontSize || 12;
                const fontFamily = pageNumbers.fontFamily ? `font-family: ${pageNumbers.fontFamily};` : '';
                const fontStyle = pageNumbers.fontStyle ? `font-style: ${pageNumbers.fontStyle};` : '';
                const textAlign = `text-align: ${pageNumbers.align || 'center'};`;
                // Get bottomOffset from profile settings (supports null/undefined, converts to number)
                const bottomOffset = (pageNumbers.bottomOffset !== undefined && pageNumbers.bottomOffset !== null) ? Number(pageNumbers.bottomOffset) : 0;
                // Use margin-bottom instead of padding-bottom to create space from the bottom of the footer
                const marginBottom = bottomOffset > 0 ? `margin-bottom: ${bottomOffset}px;` : '';

                footer.innerHTML = `<div style="${fontFamily} ${fontStyle} font-size: ${fontSize}pt; ${textAlign} width: 100%; color: #000; ${marginBottom}">${format}</div>`;
                pageDiv.appendChild(footer);
            }

            container.appendChild(pageDiv);
        });

        // Debug helper: highlight elements that overflow their page container
        (function debugPageLayout() {
            try {
                const pagesList = container.querySelectorAll('.document-preview__page');
                pagesList.forEach((pageEl, pageIndex) => {
                    const pageRect = pageEl.getBoundingClientRect();

                    // Highlight any child element that lies outside the visible page rect
                    const allChildren = pageEl.querySelectorAll('*');
                    allChildren.forEach((child) => {
                        const r = child.getBoundingClientRect();
                        // Consider small rounding tolerance
                        const tol = 1;
                        if (r.top < pageRect.top - tol || r.bottom > pageRect.bottom + tol) {
                            console.log(`Overflow detected on page ${pageIndex + 1}: tag=${child.tagName}, top=${Math.round(r.top - pageRect.top)}px, bottom=${Math.round(r.bottom - pageRect.top)}px, height=${Math.round(r.height)}px`, child);
                            try {
                                child.style.outline = '2px dashed red';
                                child.style.background = 'rgba(255,0,0,0.05)';
                            } catch (e) {}
                        }
                    });

                });
            } catch (err) {
                console.warn('debugPageLayout failed', err);
            }
        })();
    }

    // ============================================================================
    // EXPORT
    // ============================================================================

    if (typeof global !== 'undefined') {
        global.renderPreview = renderPreview;
    }

    if (typeof window !== 'undefined') {
        window.renderPreview = renderPreview;
    }

})(typeof window !== 'undefined' ? window : typeof global !== 'undefined' ? global : this);
