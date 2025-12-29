/**
 * Pure function to split HTML content into pages based on content height
 * Elements are never split - they are moved to next page if they don't fit
 */
export function splitIntoPages(
	html: string,
	pageContentHeight: number,
	contentWidth: number
): string[] {
	if (!html.trim()) {
		return [''];
	}

	// Create a temporary container WITHOUT padding to measure elements accurately
	const tempContainer = document.createElement('div');
	tempContainer.style.position = 'absolute';
	tempContainer.style.visibility = 'hidden';
	tempContainer.style.width = `${contentWidth}px`;
	// NO padding here - we measure elements without padding
	tempContainer.style.fontFamily = "'Times New Roman', Times, serif";
	tempContainer.style.fontSize = '14pt';
	tempContainer.style.lineHeight = '1.5';
	tempContainer.style.color = '#1a1a1a';
	document.body.appendChild(tempContainer);

	// First, measure total content height WITHOUT padding to check if splitting is needed
	const totalContainer = document.createElement('div');
	totalContainer.style.position = 'absolute';
	totalContainer.style.visibility = 'hidden';
	totalContainer.style.width = `${contentWidth}px`;
	// NO padding here - we measure content height without padding to match pageContentHeight
	totalContainer.style.fontFamily = "'Times New Roman', Times, serif";
	totalContainer.style.fontSize = '14pt';
	totalContainer.style.lineHeight = '1.5';
	totalContainer.style.color = '#1a1a1a';
	totalContainer.innerHTML = html;
	document.body.appendChild(totalContainer);

	const totalHeight = totalContainer.scrollHeight;
	document.body.removeChild(totalContainer);

	// If content fits in one page, return as is
	if (totalHeight <= pageContentHeight) {
		document.body.removeChild(tempContainer);
		return [html];
	}

	// Parse HTML to get individual elements
	const parser = new DOMParser();
	const doc = parser.parseFromString(html, 'text/html');
	const body = doc.body;

	const pages: string[] = [];
	const elements = Array.from(body.children);
	let currentPageElements: Element[] = [];

	// Elements that should not be split (block-level elements)
	const blockElements = new Set([
		'P',
		'H1',
		'H2',
		'H3',
		'H4',
		'H5',
		'H6',
		'UL',
		'OL',
		'LI',
		'TABLE',
		'TR',
		'THEAD',
		'TBODY',
		'DIV',
		'BLOCKQUOTE',
		'PRE',
		'HR',
	]);

	for (const element of elements) {
		// Measure this element WITHOUT padding
		const elementClone = element.cloneNode(true) as HTMLElement;
		tempContainer.innerHTML = '';
		tempContainer.appendChild(elementClone);
		const elementHeight = tempContainer.scrollHeight;

		// Check if element should not be split
		const isBlockElement = blockElements.has(element.tagName);

		// If element alone exceeds page height and it's a block element, put it on its own page
		if (elementHeight > pageContentHeight && isBlockElement) {
			// Save current page if it has content
			if (currentPageElements.length > 0) {
				const pageDiv = document.createElement('div');
				currentPageElements.forEach((el) =>
					pageDiv.appendChild(el.cloneNode(true))
				);
				pages.push(pageDiv.innerHTML);
				currentPageElements = [];
			}

			// Put large element on its own page
			const pageDiv = document.createElement('div');
			pageDiv.appendChild(element.cloneNode(true));
			pages.push(pageDiv.innerHTML);
			continue;
		}

		// Measure combined height of current page + new element
		// This gives us more accurate measurement than just summing heights
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
			currentPageElements.forEach((el) =>
				testPageDiv.appendChild(el.cloneNode(true))
			);
			testPageDiv.appendChild(element.cloneNode(true));
			testContainer.appendChild(testPageDiv);
			document.body.appendChild(testContainer);

			const combinedHeight = testContainer.scrollHeight;
			document.body.removeChild(testContainer);

			// Check if combined height exceeds page height
			if (combinedHeight > pageContentHeight) {
				// Start new page
				const pageDiv = document.createElement('div');
				currentPageElements.forEach((el) =>
					pageDiv.appendChild(el.cloneNode(true))
				);
				pages.push(pageDiv.innerHTML);
				currentPageElements = [element];
			} else {
				// Add to current page
				currentPageElements.push(element);
			}
		} else {
			// First element on page
			currentPageElements.push(element);
		}
	}

	// Add remaining elements as last page
	if (currentPageElements.length > 0) {
		const pageDiv = document.createElement('div');
		currentPageElements.forEach((el) => pageDiv.appendChild(el.cloneNode(true)));
		pages.push(pageDiv.innerHTML);
	}

	document.body.removeChild(tempContainer);
	return pages.length > 0 ? pages : [html];
}
