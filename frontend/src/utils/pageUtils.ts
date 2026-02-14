/**
 * Returns a Promise that resolves when all images are decoded or after timeout.
 */
function ensureImagesLoadedAsync(element: HTMLElement, timeoutMs = 5000): Promise<void> {
	const images = Array.from(element.querySelectorAll('img'));
	if (images.length === 0) return Promise.resolve();

	const promises = images.map((img) => {
		try {
			if (img.complete && img.naturalWidth > 0 && img.naturalHeight > 0) {
				return Promise.resolve();
			}
			if (typeof img.decode === 'function') {
				return img.decode().catch(() => {
					return new Promise<void>((res) => {
						if (img.complete) return res();
						img.addEventListener('load', () => res(), { once: true });
						img.addEventListener('error', () => res(), { once: true });
					});
				});
			}
			return new Promise<void>((resolve) => {
				if (img.complete) return resolve();
				img.addEventListener('load', () => resolve(), { once: true });
				img.addEventListener('error', () => resolve(), { once: true });
			});
		} catch (e) {
			return Promise.resolve();
		}
	});

	const all = Promise.all(promises);
	const timeout = new Promise<void>((resolve) => setTimeout(resolve, timeoutMs));
	return Promise.race([all, timeout]).then(() => {
		// Trigger a layout recalculation
		void element.offsetHeight;
	});
}

function applyMeasurementStyles(element: HTMLElement, contentWidth: number): void {
	element.style.position = 'absolute';
	element.style.visibility = 'hidden';
	element.style.width = `${contentWidth}px`;
	element.style.fontFamily = "'Times New Roman', Times, serif";
	element.style.fontSize = '14pt';
	element.style.lineHeight = '1.5';
	element.style.color = '#1a1a1a';
}

function measureElementHeight(element: HTMLElement): number {
	const rect = element.getBoundingClientRect();
	const styles = window.getComputedStyle(element);
	const marginTop = Number.parseFloat(styles.marginTop) || 0;
	const marginBottom = Number.parseFloat(styles.marginBottom) || 0;
	return rect.height + marginTop + marginBottom;
}

export interface SplitIntoPagesOptions {
	returnElementPageMap?: boolean;
}

export interface SplitIntoPagesResult {
	pages: string[];
	elementPageMap: Record<number, number>;
}

/**
 * Async function to split HTML content into pages based on content height
 * Elements are never split - they are moved to next page if they don't fit
 */
export async function splitIntoPages(
	html: string,
	pageContentHeight: number,
	contentWidth: number,
	options?: SplitIntoPagesOptions
): Promise<string[] | SplitIntoPagesResult> {
	const returnElementPageMap = options?.returnElementPageMap ?? false;
	if (!html.trim()) {
		return returnElementPageMap
			? { pages: [''], elementPageMap: {} }
			: [''];
	}

	const measurementContainer = document.createElement('div');
	applyMeasurementStyles(measurementContainer, contentWidth);
	measurementContainer.innerHTML = html;
	document.body.appendChild(measurementContainer);

	try {
		// Wait once for images before doing all measurements.
		await ensureImagesLoadedAsync(measurementContainer);

		const totalHeight = measurementContainer.scrollHeight;

		// Parse HTML to get source elements used for final page assembly.
		const parser = new DOMParser();
		const doc = parser.parseFromString(html, 'text/html');
		const sourceElements = Array.from(doc.body.children);

		if (totalHeight <= pageContentHeight) {
			const elementPageMap: Record<number, number> = {};
			if (returnElementPageMap) {
				sourceElements.forEach((_, idx) => {
					elementPageMap[idx] = 0;
				});
				return { pages: [html], elementPageMap };
			}
			return [html];
		}

		// These elements are in strict top-to-bottom order as rendered.
		const measuredElements = Array.from(measurementContainer.children) as HTMLElement[];

		const pages: string[] = [];
		let currentPageElements: { el: Element; idx: number }[] = [];
		let currentPageHeight = 0;
		const elementPageMap: Record<number, number> = {};
		let nextPageIndex = 0;

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

		const fallbackMeasureContainer = document.createElement('div');
		applyMeasurementStyles(fallbackMeasureContainer, contentWidth);
		document.body.appendChild(fallbackMeasureContainer);

		const flushCurrentPage = () => {
			if (currentPageElements.length === 0) return;
			currentPageElements.forEach(({ idx }) => {
				elementPageMap[idx] = nextPageIndex;
			});
			const pageDiv = document.createElement('div');
			currentPageElements.forEach(({ el }) => {
				pageDiv.appendChild(el.cloneNode(true));
			});
			pages.push(pageDiv.innerHTML);
			nextPageIndex++;
			currentPageElements = [];
			currentPageHeight = 0;
		};

		for (let index = 0; index < sourceElements.length; index += 1) {
			const sourceElement = sourceElements[index];
			const measuredElement = measuredElements[index];

			let elementHeight = 0;
			if (measuredElement) {
				elementHeight = measureElementHeight(measuredElement);
			} else {
				// Rare fallback: keep order and measure element in an isolated container.
				fallbackMeasureContainer.innerHTML = '';
				fallbackMeasureContainer.appendChild(sourceElement.cloneNode(true));
				elementHeight = fallbackMeasureContainer.scrollHeight;
			}

			const isBlockElement = blockElements.has(sourceElement.tagName);

			// Large block elements get their own page, but sequence is preserved.
			if (elementHeight > pageContentHeight && isBlockElement) {
				flushCurrentPage();
				elementPageMap[index] = nextPageIndex;
				const pageDiv = document.createElement('div');
				pageDiv.appendChild(sourceElement.cloneNode(true));
				pages.push(pageDiv.innerHTML);
				nextPageIndex++;
				continue;
			}

			if (currentPageHeight > 0 && currentPageHeight + elementHeight > pageContentHeight) {
				flushCurrentPage();
			}

			currentPageElements.push({ el: sourceElement, idx: index });
			currentPageHeight += elementHeight;
		}

		flushCurrentPage();
		document.body.removeChild(fallbackMeasureContainer);
		const resultPages = pages.length > 0 ? pages : [html];
		if (returnElementPageMap) {
			return { pages: resultPages, elementPageMap };
		}
		return resultPages;
	} finally {
		document.body.removeChild(measurementContainer);
	}
}
