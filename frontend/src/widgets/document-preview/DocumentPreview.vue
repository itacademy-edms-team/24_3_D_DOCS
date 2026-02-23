<template>
	<div class="document-preview">
		<div v-if="isLoading" class="document-preview__loading">Загрузка...</div>
		<div v-else ref="previewContainerRef" class="document-preview__container">
			<!-- Title Page -->
			<div
				v-if="titlePageHtml"
				class="document-preview__page-wrapper"
			>
				<div
					class="document-preview__page document-preview__page--title"
					:style="getTitlePageStyle()"
				>
					<div
						class="document-preview__title-page-content"
						v-html="titlePageHtml"
					/>
				</div>
			</div>

			<!-- TOC and Document Pages -->
			<div
				v-for="(page, index) in pages"
				:key="index"
				class="document-preview__page-wrapper"
			>
				<div
					class="document-preview__page"
					:class="{ 'document-preview__page--toc': index < tocPageCount }"
					:style="getPageStyle(index, titlePageHtml ? 1 : 0)"
				>
				<!-- Header with page number - absolutely positioned at top -->
				<div
					v-if="showHeader"
					class="document-preview__header"
					:style="getHeaderStyle()"
					v-html="renderPageNumber(getPageNumber(index), totalPages)"
				/>

				<!-- Content area -->
				<div
					class="document-preview__content"
					:style="getContentStyle(showHeader)"
					v-html="page"
				/>

				<!-- Footer with page number - absolutely positioned at bottom -->
				<div
					v-if="showFooter"
					class="document-preview__footer"
					:style="getFooterStyle()"
					v-html="renderPageNumber(getPageNumber(index), totalPages)"
				/>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, nextTick } from 'vue';
import { useElementSize } from '@vueuse/core';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';
import { renderDocument } from '@/utils/documentRenderer';
import { renderTitlePageToHtml } from '@/utils/titlePageRenderer';
import { splitIntoPages } from '@/utils/pageUtils';
import {
	calculatePageDimensions,
	type PageDimensions,
} from '@/utils/pageConstants';
import type { ProfileData } from '@/entities/profile/types';
import type { TitlePageWithData } from '@/entities/title-page/api/TitlePageAPI';
import type { TocItem } from '@/entities/document/types';
import { getAccessToken } from '@/shared/auth/tokenStorage';
import { renderTableOfContentsToHtml } from '@/utils/tableOfContentsRenderer';
import 'katex/dist/katex.css';

interface Props {
	content: string;
	profileId?: string;
	titlePageId?: string;
	titlePageVariables?: Record<string, string>;
	documentId: string;
	tableOfContents?: TocItem[] | null;
}

const props = defineProps<Props>();

const previewContainerRef = ref<HTMLElement | null>(null);
const { width: containerWidth } = useElementSize(previewContainerRef);

const profileData = ref<ProfileData | null>(null);
const titlePage = ref<TitlePageWithData | null>(null);
const titlePageHtml = ref<string>('');
const pages = ref<string[]>([]);
const tocPageCount = ref(0);
const isLoading = ref(false);
const isUpdatingPages = ref(false);
const isLoadingTitlePage = ref(false);

// Calculate page dimensions reactively based on profile
const dimensions = computed<PageDimensions>(() => {
	return calculatePageDimensions(profileData.value);
});

// Calculate available content height (accounting for margins and header/footer)
const headerContentHeight = computed(() => {
	const pageNumbers = profileData.value?.pageSettings?.pageNumbers;
	if (!pageNumbers?.enabled || pageNumbers.position !== 'top') {
		return 0;
	}
	// Approximate height for page number header
	return (pageNumbers.fontSize || 12) * 1.2; // fontSize * lineHeight
});

const contentHeight = computed(() => {
	return (
		dimensions.value.pageHeight -
		dimensions.value.marginTop -
		dimensions.value.marginBottom -
		headerContentHeight.value
	);
});

const contentWidth = computed(() => {
	return (
		dimensions.value.pageWidth -
		dimensions.value.marginLeft -
		dimensions.value.marginRight
	);
});

const showHeader = computed(() => {
	const pageNumbers = profileData.value?.pageSettings.pageNumbers;
	return pageNumbers?.enabled && pageNumbers.position === 'top';
});

const showFooter = computed(() => {
	const pageNumbers = profileData.value?.pageSettings.pageNumbers;
	return pageNumbers?.enabled && pageNumbers.position === 'bottom';
});

/**
 * Render page number based on settings
 * Pure function - no side effects
 */
function renderPageNumber(
	pageNumber: number,
	totalPagesCount: number
): string {
	const pageNumbers = profileData.value?.pageSettings.pageNumbers;
	if (!pageNumbers?.enabled) {
		return '';
	}

	const format = (pageNumbers.format || '{n}')
		.replace('{n}', String(pageNumber))
		.replace('{total}', String(totalPagesCount));

	const fontSize = pageNumbers.fontSize || 12;
	const fontFamily = pageNumbers.fontFamily
		? `font-family: ${pageNumbers.fontFamily};`
		: '';
	const fontStyle = pageNumbers.fontStyle
		? `font-style: ${pageNumbers.fontStyle};`
		: '';
	const textAlign = `text-align: ${pageNumbers.align || 'center'};`;
	// Get bottomOffset from profile settings (supports null/undefined, converts to number)
	const bottomOffset = (pageNumbers.bottomOffset !== undefined && pageNumbers.bottomOffset !== null) ? Number(pageNumbers.bottomOffset) : 0;
	// Use margin-bottom instead of padding-bottom to create space from the bottom of the footer
	const marginBottom = bottomOffset > 0 ? `margin-bottom: ${bottomOffset}px;` : '';

	return `
		<div style="${fontFamily} ${fontStyle} font-size: ${fontSize}pt; ${textAlign} width: 100%; color: #000; ${marginBottom}">
			${format}
		</div>
	`;
}

/**
 * Load and render title page
 */
async function loadTitlePage() {
	if (!props.titlePageId) {
		titlePage.value = null;
		titlePageHtml.value = '';
		return;
	}

	isLoadingTitlePage.value = true;
	try {
		const loadedTitlePage = await TitlePageAPI.getById(props.titlePageId);
		titlePage.value = loadedTitlePage;
		
		const variables = props.titlePageVariables || {};
		titlePageHtml.value = renderTitlePageToHtml(loadedTitlePage, variables);
	} catch (error) {
		console.error('Failed to load title page:', error);
		titlePage.value = null;
		titlePageHtml.value = '';
	} finally {
		isLoadingTitlePage.value = false;
	}
}

const totalPages = computed(() => {
	return pages.value.length + (titlePageHtml.value ? 1 : 0);
});

const getPageNumber = (index: number): number => {
	return index + 1 + (titlePageHtml.value ? 1 : 0);
};

// Calculate scale factor to fit container width
const scaleFactor = computed(() => {
	if (!containerWidth.value || !dimensions.value.pageWidth) return 1;
	// Account for padding on both sides (spacing-lg typically 16px each side = 32px total)
	const padding = 32; 
	const availableWidth = Math.max(200, containerWidth.value - padding);
	const scale = availableWidth / dimensions.value.pageWidth;
	// Ensure scale never exceeds 1 and is at least 0.2 for readability
	return Math.min(1, Math.max(0.2, scale));
});

/**
 * Get title page style
 */
function getTitlePageStyle() {
	const MM_TO_PX = 3.7795275591;
	const A4_WIDTH_MM = 210;
	const A4_HEIGHT_MM = 297;
	const A4_WIDTH_PX = A4_WIDTH_MM * MM_TO_PX;
	const A4_HEIGHT_PX = A4_HEIGHT_MM * MM_TO_PX;
	
	const scale = scaleFactor.value;
	const scaledWidth = A4_WIDTH_PX * scale;
	
	return {
		width: `${A4_WIDTH_PX}px`,
		height: `${A4_HEIGHT_PX}px`,
		margin: '0 auto',
		background: 'white',
		boxShadow: '0 2px 8px rgba(0,0,0,0.3)',
		pageBreakAfter: 'always' as const,
		pageBreakInside: 'avoid' as const,
		position: 'relative' as const,
		overflow: 'hidden' as const,
		transform: `scale(${scale})`,
		transformOrigin: 'top center',
		marginBottom: `${20 * scale}px`, // Use scaled margin between pages
		maxWidth: `${scaledWidth}px`,
		minWidth: `${scaledWidth}px`,
	};
}

/**
 * Get page style computed property
 */
function getPageStyle(index: number, offset: number = 0) {
	const scale = scaleFactor.value;
	const baseMarginTop = (index + offset) > 0 ? 20 : 0;
	const scaledWidth = dimensions.value.pageWidth * scale;
	
	return {
		width: `${dimensions.value.pageWidth}px`,
		height: `${dimensions.value.pageHeight}px`,
		margin: `${baseMarginTop}px auto 0`,
		background: 'white',
		boxShadow: '0 2px 8px rgba(0,0,0,0.3)',
		pageBreakAfter: 'always' as const,
		pageBreakInside: 'avoid' as const,
		position: 'relative' as const,
		overflow: 'hidden' as const,
		transform: `scale(${scale})`,
		transformOrigin: 'top center',
		marginBottom: `${20 * scale}px`, // Use scaled margin between pages
		maxWidth: `${scaledWidth}px`,
		minWidth: `${scaledWidth}px`,
	};
}

/**
 * Get header style computed property
 */
function getHeaderStyle() {
	return {
		position: 'absolute' as const,
		top: '0',
		left: '0',
		right: '0',
		padding: `${dimensions.value.marginTop}px 0 0 0`,
		width: '100%',
		zIndex: 1,
	};
}

/**
 * Get content style computed property
 */
function getContentStyle(hasHeader: boolean = false) {
	const paddingTop = hasHeader
		? dimensions.value.marginTop + headerContentHeight.value
		: dimensions.value.marginTop;

	return {
		width: '100%',
		height: '100%',
		padding: `${paddingTop}px ${dimensions.value.marginRight}px ${dimensions.value.marginBottom}px ${dimensions.value.marginLeft}px`,
		fontFamily: "'Times New Roman', Times, serif",
		fontSize: '14pt',
		lineHeight: 1.5,
		color: '#1a1a1a',
		boxSizing: 'border-box' as const,
		pageBreakInside: 'avoid' as const,
	};
}

/**
 * Get footer style computed property
 */
function getFooterStyle() {
	return {
		position: 'absolute' as const,
		bottom: '0',
		left: '0',
		right: '0',
		height: `${dimensions.value.marginBottom}px`,
		width: '100%',
		display: 'flex' as const,
		flexDirection: 'column' as const,
		justifyContent: 'flex-end' as const,
		zIndex: 1,
	};
}

/**
 * Generate rendered HTML from markdown with profile styles
 */
async function generateRenderedHTML(): Promise<string> {
	if (!props.content.trim()) {
		return '';
	}

	// Load profile if needed
	if (props.profileId && !profileData.value) {
		try {
			const profile = await ProfileAPI.getById(props.profileId);
			if (profile.data) {
				profileData.value = profile.data;
			}
		} catch (error) {
			console.error('Failed to load profile:', error);
		}
	}

	// Clean AI markers from content for preview
	const cleanedContent = cleanAiMarkersForPreview(props.content);
	
	// Reuse previous HTML when both content and profile object are unchanged.
	if (
		renderCache &&
		renderCache.content === cleanedContent &&
		renderCache.profileRef === profileData.value
	) {
		return renderCache.html;
	}

	// Render markdown with profile styles
	const renderedHtml = renderDocument({
		markdown: cleanedContent,
		profile: profileData.value,
		overrides: {},
		selectable: false,
	});
	renderCache = {
		content: cleanedContent,
		profileRef: profileData.value,
		html: renderedHtml,
	};
	return renderedHtml;
}

function cleanAiMarkersForPreview(content: string): string {
	let cleaned = content;
	
	// Remove AI:DELETE blocks entirely (content marked for deletion should not appear in preview)
	cleaned = cleaned.replace(
		/<!-- AI:DELETE:\w+ -->[\s\S]*?<!-- \/AI:DELETE:\w+ -->\n?/g,
		''
	);
	
	// Keep AI:INSERT content but remove markers
	cleaned = cleaned.replace(
		/<!-- AI:INSERT:(\w+) -->\n?([\s\S]*?)\n?<!-- \/AI:INSERT:\1 -->/g,
		'$2'
	);
	
	// Clean up multiple consecutive newlines
	cleaned = cleaned.replace(/\n{3,}/g, '\n\n');
	
	return cleaned;
}

/**
 * Update pages reactively
 */
async function updatePages(requestId: number) {
	activeUpdateRequestId = requestId;

	if (!props.content.trim()) {
		if (requestId === latestUpdateRequestId) {
			pages.value = [];
			isUpdatingPages.value = false;
		}
		return;
	}

	if (requestId === latestUpdateRequestId) {
		isUpdatingPages.value = true;
	}

	try {
		// Generate rendered HTML
		const renderedHtml = await generateRenderedHTML();
		if (requestId !== latestUpdateRequestId || requestId !== activeUpdateRequestId) {
			return;
		}

		if (!renderedHtml) {
			pages.value = [];
			return;
		}

		// Split into pages asynchronously using requestIdleCallback if available
		const scheduleSplit = (callback: () => void) => {
			if (typeof requestIdleCallback !== 'undefined') {
				requestIdleCallback(callback, { timeout: 1000 });
			} else {
				setTimeout(callback, 0);
			}
		};

		scheduleSplit(async () => {
			if (requestId !== latestUpdateRequestId || requestId !== activeUpdateRequestId) {
				return;
			}
			try {
				const hasToc =
					props.tableOfContents &&
					Array.isArray(props.tableOfContents) &&
					props.tableOfContents.length > 0;
				let contentPages: string[];
				let tocPages: string[] = [];

				if (hasToc && props.tableOfContents) {
					const splitResult = (await splitIntoPages(
						renderedHtml,
						contentHeight.value,
						contentWidth.value,
						{ returnElementPageMap: true }
					)) as { pages: string[]; elementPageMap: Record<number, number> };
					contentPages = splitResult.pages;
					const elementPageMap = splitResult.elementPageMap || {};
					const parser = new DOMParser();
					const doc = parser.parseFromString(renderedHtml, 'text/html');
					const elements = Array.from(doc.body.children);
					const headingPageMapByIndex: number[] = [];
					elements.forEach((el, idx) => {
						if (/^H[1-6]$/.test(el.tagName)) {
							headingPageMapByIndex.push(
								elementPageMap[idx] !== undefined ? elementPageMap[idx] : 0
							);
						}
					});
					const tocSettings = profileData.value?.tableOfContents || undefined;
					const tocHtml = renderTableOfContentsToHtml(
						props.tableOfContents,
						tocSettings,
						headingPageMapByIndex,
						0
					);
					tocPages = await splitIntoPages(
						tocHtml,
						contentHeight.value,
						contentWidth.value
					) as string[];
					const pageOffset = 1 + (titlePageHtml.value ? 1 : 0) + tocPages.length;
					const tocHtmlWithPages = renderTableOfContentsToHtml(
						props.tableOfContents,
						tocSettings,
						headingPageMapByIndex,
						pageOffset
					);
					tocPages = (await splitIntoPages(
						tocHtmlWithPages,
						contentHeight.value,
						contentWidth.value
					)) as string[];
				} else {
					contentPages = (await splitIntoPages(
						renderedHtml,
						contentHeight.value,
						contentWidth.value
					)) as string[];
				}

				if (requestId !== latestUpdateRequestId || requestId !== activeUpdateRequestId) {
					return;
				}
				tocPageCount.value = tocPages.length;
				pages.value = [...tocPages, ...contentPages];
				
				// After pages are set, handle image loading in the DOM
				nextTick(() => {
					handleImageLoading();
				});
			} catch (error) {
				console.error('Failed to split pages:', error);
				if (requestId !== latestUpdateRequestId || requestId !== activeUpdateRequestId) {
					return;
				}
				tocPageCount.value = 0;
				pages.value = [renderedHtml];
				nextTick(() => {
					handleImageLoading();
				});
			} finally {
				if (requestId === latestUpdateRequestId) {
					isUpdatingPages.value = false;
				}
			}
		});
	} catch (error) {
		console.error('Failed to update pages:', error);
		if (requestId === latestUpdateRequestId) {
			pages.value = [];
			isUpdatingPages.value = false;
		}
	}
}

let handleImageLoadingTimeout: ReturnType<typeof setTimeout> | null = null;
let updatePagesDebounceTimeout: ReturnType<typeof setTimeout> | null = null;
let latestUpdateRequestId = 0;
let activeUpdateRequestId = 0;

interface RenderCache {
	content: string;
	profileRef: ProfileData | null;
	html: string;
}

let renderCache: RenderCache | null = null;

/**
 * Debounced version of updatePages to prevent excessive re-renders
 */
function debouncedUpdatePages() {
	const requestId = ++latestUpdateRequestId;
	if (updatePagesDebounceTimeout) {
		clearTimeout(updatePagesDebounceTimeout);
	}
	updatePagesDebounceTimeout = setTimeout(() => {
		void updatePages(requestId);
	}, 300);
}

/**
 * Handle image loading errors and add loading states
 */
function handleImageLoading() {
	// Debounce to prevent multiple rapid calls
	if (handleImageLoadingTimeout) {
		clearTimeout(handleImageLoadingTimeout);
	}

	handleImageLoadingTimeout = setTimeout(() => {
		const contentElements = document.querySelectorAll('.document-preview__content');

		contentElements.forEach((contentEl) => {
			const images = contentEl.querySelectorAll('img');

			images.forEach((img) => {
				// Skip if already processed
				if (img.dataset.processed === 'true') {
					return;
				}

				img.dataset.processed = 'true';

				if (img.complete && img.naturalWidth > 0) {
					img.style.opacity = '1';
					return;
				}

				img.style.opacity = '0';
				img.style.transition = 'opacity 0.3s ease';

				img.onload = () => {
					img.style.opacity = '1';
					// Rerun split-to-pages pass whenever an image initially loads asynchronously
					// to keep headers/footers from being overlapped by layout shifts
					if (!isUpdatingPages.value) {
						debouncedUpdatePages();
					}
				};

				img.onerror = (event) => {
					console.error('Failed to load image:', img.src, event);

					const retryCount = parseInt(img.dataset.retryCount || '0');
					if (retryCount >= 2) {
						img.style.opacity = '1';
						img.style.border = '2px dashed #ff4444';
						img.style.padding = '10px';
						img.style.backgroundColor = '#ffeeee';
						return;
					}

					img.dataset.retryCount = String(retryCount + 1);
					img.style.opacity = '1';

					try {
						const currentToken = getCurrentAuthToken();
						if (currentToken && img.src) {
							let url: URL;
							const base = typeof BASE_URI !== 'undefined' ? BASE_URI : window.location.origin;
							
							try {
								url = img.src.startsWith('http://') || img.src.startsWith('https://')
									? new URL(img.src)
									: new URL(img.src, base);
							} catch (parseError) {
								const baseUrl = img.src.split('?')[0].split('&')[0];
								url = new URL(baseUrl, base);
							}

							url.searchParams.set('token', currentToken);
							const newSrc = url.toString();

							const oldOnError = img.onerror;
							img.onerror = null;

							setTimeout(() => {
								img.src = newSrc;
								setTimeout(() => {
									if (!img.complete && !img.naturalWidth) {
										img.onerror = oldOnError;
									}
								}, 50);
							}, 100);
							return;
						}
					} catch (urlError) {
						console.error('Failed to parse image URL:', urlError);
					}

					img.style.border = '2px dashed #ff4444';
					img.style.padding = '10px';
					img.style.backgroundColor = '#ffeeee';
				};
			});
		});
	}, 100); // Debounce delay
}

/**
 * Get current auth token from localStorage
 */
function getCurrentAuthToken(): string | null {
	return getAccessToken();
}

// Load profile when profileId changes
watch(
	() => props.profileId,
	async (newProfileId, oldProfileId) => {
		if (newProfileId === oldProfileId) return;
		renderCache = null;
		
		if (newProfileId) {
			isLoading.value = true;
			try {
				const profile = await ProfileAPI.getById(newProfileId);
				if (profile.data) {
					profileData.value = profile.data;
				}
			} catch (error) {
				console.error('Failed to load profile:', error);
				profileData.value = null;
			} finally {
				isLoading.value = false;
			}
		} else {
			profileData.value = null;
		}
	},
	{ immediate: true }
);

// Load title page when titlePageId or titlePageVariables change
watch(
	[() => props.titlePageId, () => props.titlePageVariables],
	async () => {
		await loadTitlePage();
	},
	{ immediate: true, deep: true }
);

// Update pages when content, profile, TOC, or title page changes
watch(
	[
		() => props.content,
		() => profileData.value,
		() => props.tableOfContents,
		() => titlePageHtml.value,
	],
	() => {
		debouncedUpdatePages();
	},
	{ immediate: false }
);

onMounted(async () => {
	// Load profile first if needed
	if (props.profileId) {
		try {
			const profile = await ProfileAPI.getById(props.profileId);
			if (profile.data) {
				profileData.value = profile.data;
			}
		} catch (error) {
			console.error('Failed to load profile:', error);
		}
	}
	
	// Load title page if needed
	await loadTitlePage();
	
	// Then update pages
	latestUpdateRequestId += 1;
	await updatePages(latestUpdateRequestId);
	
	// Handle images after initial render
	nextTick(() => {
		handleImageLoading();
	});
});
</script>

<style scoped>
.document-preview {
	height: 100%;
	display: flex;
	flex-direction: column;
	background: var(--bg-tertiary);
	padding: var(--spacing-lg);
}

.document-preview__loading {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 100%;
	color: var(--text-secondary);
}

.document-preview__container {
	flex: 1;
	display: flex;
	flex-direction: column;
	align-items: center;
	overflow-y: auto;
	overflow-x: auto;
	padding: var(--spacing-lg);
	min-width: 0;
	width: 100%;
	box-sizing: border-box;
}

.document-preview__page-wrapper {
	flex-shrink: 0;
	width: 100%;
	max-width: 100%;
	display: flex;
	justify-content: center;
	align-items: flex-start;
	overflow: visible;
	min-width: 0;
	padding: 0;
	margin: 0;
}

.document-preview__page {
	flex-shrink: 0;
	box-sizing: border-box;
	overflow: visible;
}

.document-preview__header,
.document-preview__footer {
	/* Styles applied via inline styles */
	position: relative;
}

.document-preview__content {
	/* Styles applied via inline styles */
	overflow: visible;
}

.document-preview__title-page-content {
	width: 210mm;
	height: 297mm;
	position: relative;
	font-family: 'Times New Roman', Times, serif;
	margin: 0;
	padding: 0;
}

.document-preview__content :deep(*) {
	page-break-inside: avoid;
}

.document-preview__content :deep(p),
.document-preview__content :deep(h1),
.document-preview__content :deep(h2),
.document-preview__content :deep(h3),
.document-preview__content :deep(h4),
.document-preview__content :deep(h5),
.document-preview__content :deep(h6),
.document-preview__content :deep(ul),
.document-preview__content :deep(ol),
.document-preview__content :deep(li),
.document-preview__content :deep(table),
.document-preview__content :deep(blockquote),
.document-preview__content :deep(pre) {
	page-break-inside: avoid;
}


/* Keep KaTeX size aligned with Profile fontSize.
   KaTeX default CSS sets .katex font-size to 1.21em, which inflates formulas.
   Override to 1em so formula wrapper font-size (from profile) is respected exactly. */
.document-preview__content :deep(.formula-block .katex),
.document-preview__content :deep(.formula-inline .katex) {
	font-size: 1em !important;
}

/* Keep formula block inside page width and avoid clipping artifacts. */
.document-preview__content :deep(.formula-block) {
	display: block;
	clear: both;
	position: relative;
	max-width: 100%;
	overflow: visible;
	padding-top: 0.15em;
	padding-bottom: 0.15em;
}

/* Try to wrap long display formulas instead of showing horizontal scrollbar. */
.document-preview__content :deep(.formula-block .katex-display) {
	margin: 0;
	max-width: 100%;
	overflow: visible;
	line-height: 1.35;
}

.document-preview__content :deep(.formula-block .katex-display > .katex) {
	display: block;
	max-width: 100%;
	white-space: normal !important;
	overflow: visible;
}

.document-preview__content :deep(.formula-block .katex-display > .katex > .katex-html) {
	display: inline;
	max-width: 100%;
	overflow-wrap: anywhere;
	word-break: break-word;
	overflow: visible;
}

.document-preview__content :deep(.formula-block .katex-display > .katex > .katex-html .base) {
	display: inline-block;
	vertical-align: baseline;
	margin-bottom: 0.1em;
}
</style>
