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

			<!-- Document Pages -->
			<div
				v-for="(page, index) in pages"
				:key="index"
				class="document-preview__page-wrapper"
			>
				<div
					class="document-preview__page"
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
import 'katex/dist/katex.css';

interface Props {
	content: string;
	profileId?: string;
	titlePageId?: string;
	titlePageVariables?: Record<string, string>;
	documentId: string;
}

const props = defineProps<Props>();

const previewContainerRef = ref<HTMLElement | null>(null);
const { width: containerWidth } = useElementSize(previewContainerRef);

const profileData = ref<ProfileData | null>(null);
const titlePage = ref<TitlePageWithData | null>(null);
const titlePageHtml = ref<string>('');
const pages = ref<string[]>([]);
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

	return `
		<div style="${fontFamily} ${fontStyle} font-size: ${fontSize}pt; ${textAlign} width: 100%; color: #000;">
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

	// Render markdown with profile styles
	const renderedHtml = renderDocument({
		markdown: props.content,
		profile: profileData.value,
		overrides: {},
		selectable: false,
	});
	
	// #region agent log
	fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:230',message:'Rendered HTML generated',data:{htmlLength:renderedHtml.length,hasImages:renderedHtml.includes('<img'),imageCount:(renderedHtml.match(/<img/g)||[]).length},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
	// #endregion

	return renderedHtml;
}

/**
 * Update pages reactively
 */
async function updatePages() {
	if (!props.content.trim()) {
		pages.value = [];
		isUpdatingPages.value = false;
		return;
	}

	isUpdatingPages.value = true;

	try {
		// Generate rendered HTML
		const renderedHtml = await generateRenderedHTML();

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

		scheduleSplit(() => {
			try {
				const splitPages = splitIntoPages(
					renderedHtml,
					contentHeight.value,
					contentWidth.value
				);
				// #region agent log
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:272',message:'Pages split, setting pages value',data:{pagesCount:splitPages.length,htmlLength:renderedHtml.length,hasImages:renderedHtml.includes('<img')},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
				// #endregion
				pages.value = splitPages;
				
				// After pages are set, handle image loading in the DOM
				nextTick(() => {
					// #region agent log
					fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:280',message:'nextTick after splitPages, calling handleImageLoading',data:{},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
					// #endregion
					handleImageLoading();
				});
			} catch (error) {
				console.error('Failed to split pages:', error);
				// #region agent log
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:284',message:'Split pages error, using full HTML',data:{error:error?.toString()},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
				// #endregion
				pages.value = [renderedHtml];
				nextTick(() => {
					handleImageLoading();
				});
			} finally {
				isUpdatingPages.value = false;
			}
		});
	} catch (error) {
		console.error('Failed to update pages:', error);
		pages.value = [];
		isUpdatingPages.value = false;
	}
}

let handleImageLoadingTimeout: ReturnType<typeof setTimeout> | null = null;

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
		// #region agent log
		const logData1 = {location:'DocumentPreview.vue:318',message:'handleImageLoading called',data:{contentElementsCount:contentElements.length},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'};
		console.log('[DEBUG]', logData1);
		fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(logData1)}).catch((e)=>console.warn('[DEBUG] Log fetch failed:',e));
		// #endregion
	
		contentElements.forEach((contentEl) => {
		const images = contentEl.querySelectorAll('img');
		// #region agent log
		const logData2 = {location:'DocumentPreview.vue:322',message:'Images found in content element',data:{imagesCount:images.length},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'};
		console.log('[DEBUG]', logData2);
		fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(logData2)}).catch((e)=>console.warn('[DEBUG] Log fetch failed:',e));
		// #endregion
		
		images.forEach((img) => {
			// Skip if already processed
			if (img.dataset.processed === 'true') {
				// #region agent log
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:334',message:'Image already processed, skipping',data:{src:img.src,complete:img.complete},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
				// #endregion
				return;
			}
			
			img.dataset.processed = 'true';
			
			// #region agent log
			const logData3 = {location:'DocumentPreview.vue:341',message:'Processing image',data:{src:img.src,hasToken:img.src.includes('token'),complete:img.complete,naturalWidth:img.naturalWidth},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'C'};
			console.log('[DEBUG]', logData3);
			fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(logData3)}).catch((e)=>console.warn('[DEBUG] Log fetch failed:',e));
			// #endregion
			
			// Check if image is already loaded (from cache or previous load)
			if (img.complete && img.naturalWidth > 0) {
				// Image already loaded, show it immediately
				img.style.opacity = '1';
				// #region agent log
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:351',message:'Image already loaded from cache',data:{src:img.src},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'C'})}).catch(()=>{});
				// #endregion
				return;
			}
			
			// Add loading state only if image is not already loaded
			img.style.opacity = '0';
			img.style.transition = 'opacity 0.3s ease';
			
			// Handle successful load
			img.onload = () => {
				// #region agent log
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:360',message:'Image loaded successfully',data:{src:img.src,naturalWidth:img.naturalWidth},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'C'})}).catch(()=>{});
				// #endregion
				img.style.opacity = '1';
			};
			
			// Handle load error
			img.onerror = (event) => {
				// #region agent log
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:362',message:'Image load error',data:{src:img.src,hasToken:img.src.includes('token'),retryCount:parseInt(img.dataset.retryCount||'0')},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'C'})}).catch(()=>{});
				// #endregion
				console.error('Failed to load image:', img.src, event);
				
				// Prevent infinite retry loops
				const retryCount = parseInt(img.dataset.retryCount || '0');
				if (retryCount >= 2) {
					// Max retries reached, show error styling
					img.style.opacity = '1';
					img.style.border = '2px dashed #ff4444';
					img.style.padding = '10px';
					img.style.backgroundColor = '#ffeeee';
					return;
				}
				
				img.dataset.retryCount = String(retryCount + 1);
				img.style.opacity = '1';
				
				// Try to reload with fresh token if current one failed
				try {
					const currentToken = getCurrentAuthToken();
					// #region agent log
					fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:377',message:'Getting token for retry',data:{hasToken:!!currentToken,retryCount:retryCount+1},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'C'})}).catch(()=>{});
					// #endregion
					if (currentToken && img.src) {
						let url: URL;
						try {
							// Try to parse URL - handle both absolute and relative
							if (img.src.startsWith('http://') || img.src.startsWith('https://')) {
								url = new URL(img.src);
							} else {
								url = new URL(img.src, window.location.origin);
							}
						} catch (parseError) {
							// If URL parsing fails, try to fix it manually
							// Remove any malformed query parameters and rebuild
							const baseUrl = img.src.split('?')[0].split('&')[0];
							url = new URL(baseUrl, window.location.origin);
						}
						
						// Always update token to ensure it's fresh
						url.searchParams.set('token', currentToken);
						const newSrc = url.toString();
						
						// #region agent log
						fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:394',message:'Retrying with new token',data:{newSrc,oldSrc:img.src,retryCount:retryCount+1},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'C'})}).catch(()=>{});
						// #endregion
						
						// Remove error handlers temporarily to avoid recursion
						const oldOnError = img.onerror;
						img.onerror = null;
						
						// Use a small delay to ensure previous request is cancelled
						setTimeout(() => {
							img.src = newSrc;
							// Restore error handler after image starts loading
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
					// #region agent log
					fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:410',message:'URL parse error',data:{error:urlError?.toString(),src:img.src},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'C'})}).catch(()=>{});
					// #endregion
				}
				
				// Add error styling only if reload didn't help
				img.style.border = '2px dashed #ff4444';
				img.style.padding = '10px';
				img.style.backgroundColor = '#ffeeee';
			};
			
			// #region agent log
			fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'DocumentPreview.vue:463',message:'Image handlers attached',data:{src:img.src,complete:img.complete},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'C'})}).catch(()=>{});
			// #endregion
		});
		});
	}, 100); // Debounce delay
}

/**
 * Get current auth token from localStorage
 */
function getCurrentAuthToken(): string | null {
	try {
		const authData = localStorage.getItem('auth-storage');
		if (authData) {
			const parsed = JSON.parse(authData);
			return parsed?.state?.accessToken || parsed?.accessToken || null;
		}
	} catch (error) {
		console.error('Failed to get auth token:', error);
	}
	return null;
}

// Load profile when profileId changes
watch(
	() => props.profileId,
	async (newProfileId, oldProfileId) => {
		if (newProfileId === oldProfileId) return;
		
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

// Update pages when content or profile changes
watch(
	[() => props.content, () => profileData.value],
	() => {
		updatePages();
	},
	{ immediate: false } // Don't run immediately, wait for profile to load first
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
	await updatePages();
	
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
</style>
