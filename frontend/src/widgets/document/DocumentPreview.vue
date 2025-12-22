<template>
	<div class="document-preview-wrapper">
		<!-- Zoom controls -->
		<div class="zoom-controls">
			<button
				class="zoom-btn"
				@click="zoom = Math.max(50, zoom - 10)"
				title="Уменьшить масштаб"
			>
				−
			</button>
			<input
				type="range"
				min="50"
				max="200"
				:value="zoom"
				@input="zoom = parseInt(($event.target as HTMLInputElement).value)"
				class="zoom-slider"
			/>
			<span class="zoom-value">{{ zoom }}%</span>
			<button
				class="zoom-btn"
				@click="zoom = Math.min(200, zoom + 10)"
				title="Увеличить масштаб"
			>
				+
			</button>
			<button
				class="zoom-btn zoom-reset"
				@click="zoom = 100"
				title="Сбросить масштаб"
			>
				100%
			</button>
		</div>

		<!-- Preview content with zoom and drag to pan -->
		<div
			ref="scrollContainerRef"
			class="preview-container"
			:class="{ dragging: isDragging && hasMoved }"
			@mousedown="handleMouseDown"
			@mousemove="handleMouseMove"
			@mouseup="handleMouseUp"
			@mouseleave="handleMouseLeave"
		>
			<div
				class="preview-content"
				:style="{
					transform: `scale(${zoom / 100})`,
					transformOrigin: 'top left',
					width: `${100 / (zoom / 100)}%`,
				}"
			>
				<div
					style="display: none"
					v-html="html"
				/>

				<!-- Document Pages -->
				<div
					v-for="(pageHtml, index) in pages"
					:key="index"
					class="document-page"
					:style="{
						width: `${dimensions.pageWidth}px`,
						height: `${dimensions.pageHeight}px`,
						margin: index > 0 ? '20px auto 0' : '0 auto',
					}"
					@click="$emit('click', $event)"
				>
					<!-- Header with page number -->
					<div
						v-if="headerContent"
						class="page-header"
						:style="{
							paddingTop: `${dimensions.marginTop}px`,
						}"
						v-html="renderPageNumber(index + 1, totalPages, 'top')"
					/>

					<!-- Content area -->
					<div
						class="page-content"
						:style="{
							padding: headerContent && footerContent
								? `${headerContentHeight}px ${dimensions.marginRight}px ${dimensions.marginBottom}px ${dimensions.marginLeft}px`
								: headerContent
								? `${headerContentHeight}px ${dimensions.marginRight}px ${dimensions.marginBottom}px ${dimensions.marginLeft}px`
								: footerContent
								? `${dimensions.marginTop}px ${dimensions.marginRight}px ${dimensions.marginBottom}px ${dimensions.marginLeft}px`
								: `${dimensions.marginTop}px ${dimensions.marginRight}px ${dimensions.marginBottom}px ${dimensions.marginLeft}px`,
						}"
					>
						<div
							v-html="pageHtml"
							style="page-break-inside: avoid"
						/>
					</div>

					<!-- Footer with page number -->
					<div
						v-if="footerContent"
						class="page-footer"
						:style="{
							height: `${dimensions.marginBottom}px`,
						}"
						v-html="renderPageNumber(index + 1, totalPages, 'bottom')"
					/>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onUnmounted } from 'vue';
import type { Profile } from '@/entities/profile/types';
import { PAGE_SIZES, MM_TO_PX } from '@/shared/constants/pageSizes';

interface Props {
	html: string;
	profile: Profile | null;
	documentVariables?: Record<string, string>;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	click: [event: MouseEvent];
}>();

interface PageDimensions {
	pageWidth: number;
	pageHeight: number;
	marginTop: number;
	marginRight: number;
	marginBottom: number;
	marginLeft: number;
}

const DEFAULT_PAGE_SETTINGS = {
	size: 'A4',
	orientation: 'portrait' as const,
	margins: { top: 20, right: 20, bottom: 20, left: 20 },
	pageNumbers: {
		enabled: false,
		position: 'bottom' as const,
		align: 'center' as const,
		format: '{n}',
		fontSize: 12,
	},
};

function calculateDimensions(profile: Profile | null): PageDimensions {
	const settings = profile?.page || DEFAULT_PAGE_SETTINGS;
	const size = PAGE_SIZES[settings.size] || PAGE_SIZES.A4;
	const isLandscape = settings.orientation === 'landscape';

	const pageWidth = (isLandscape ? size.height : size.width) * MM_TO_PX;
	const pageHeight = (isLandscape ? size.width : size.height) * MM_TO_PX;

	return {
		pageWidth,
		pageHeight,
		marginTop: settings.margins.top * MM_TO_PX,
		marginRight: settings.margins.right * MM_TO_PX,
		marginBottom: settings.margins.bottom * MM_TO_PX,
		marginLeft: settings.margins.left * MM_TO_PX,
	}
}

function renderPageNumber(
	pageNumber: number,
	totalPages: number,
	position: 'top' | 'bottom'
): string {
	const settings = props.profile?.page || DEFAULT_PAGE_SETTINGS;
	const pageNumbers = settings.pageNumbers;
	
	if (!pageNumbers?.enabled) {
		return '';
	}

	const format = pageNumbers.format
		.replace('{n}', String(pageNumber))
		.replace('{total}', String(totalPages));

	const fontSize = pageNumbers.fontSize || 12;
	const fontFamily = (pageNumbers as any).fontFamily ? `font-family: ${(pageNumbers as any).fontFamily};` : '';
	const fontStyle = (pageNumbers as any).fontStyle ? `font-style: ${(pageNumbers as any).fontStyle};` : '';
	const textAlign = `text-align: ${pageNumbers.align};`;
	
	const paddingBottom = position === 'bottom' ? 'padding-bottom: 6px;' : '';

	return `
		<div style="${fontFamily} ${fontStyle} font-size: ${fontSize}pt; ${textAlign} width: 100%; color: #000; ${paddingBottom}">
			${format}
		</div>
	`;
}

function splitIntoPages(
	html: string,
	pageContentHeight: number,
	contentWidth: number
): string[] {
	if (!html.trim()) {
		return [html];
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

	const totalHeight = totalContainer.scrollHeight;
	document.body.removeChild(totalContainer);
	
	if (totalHeight <= pageContentHeight) {
		document.body.removeChild(tempContainer);
		return [html];
	}

	const parser = new DOMParser();
	const doc = parser.parseFromString(html, 'text/html');
	const body = doc.body;
	
	const pages: string[] = [];
	const elements = Array.from(body.children);
	let currentPageElements: Element[] = [];

	const blockElements = new Set(['P', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'UL', 'OL', 'LI', 'TABLE', 'TR', 'THEAD', 'TBODY', 'DIV']);

	for (const element of elements) {
		const elementClone = element.cloneNode(true) as HTMLElement;
		tempContainer.innerHTML = '';
		tempContainer.appendChild(elementClone);
		const elementHeight = tempContainer.scrollHeight;

		const isBlockElement = blockElements.has(element.tagName);
		
		if (elementHeight > pageContentHeight && isBlockElement) {
			if (currentPageElements.length > 0) {
				const pageDiv = document.createElement('div');
				currentPageElements.forEach(el => pageDiv.appendChild(el.cloneNode(true)));
				pages.push(pageDiv.innerHTML);
				currentPageElements = [];
			}

			const pageDiv = document.createElement('div');
			pageDiv.appendChild(element.cloneNode(true));
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
			testPageDiv.appendChild(element.cloneNode(true));
			testContainer.appendChild(testPageDiv);
			document.body.appendChild(testContainer);
			
			const combinedHeight = testContainer.scrollHeight;
			document.body.removeChild(testContainer);
			
			if (combinedHeight > pageContentHeight) {
				const pageDiv = document.createElement('div');
				currentPageElements.forEach(el => pageDiv.appendChild(el.cloneNode(true)));
				pages.push(pageDiv.innerHTML);
				currentPageElements = [element];
			} else {
				currentPageElements.push(element);
			}
		} else {
			currentPageElements.push(element);
		}
	}

	if (currentPageElements.length > 0) {
		const pageDiv = document.createElement('div');
		currentPageElements.forEach(el => pageDiv.appendChild(el.cloneNode(true)));
		pages.push(pageDiv.innerHTML);
	}

	document.body.removeChild(tempContainer);
	return pages.length > 0 ? pages : [html];
}

const dimensions = computed(() => calculateDimensions(props.profile));

const settings = computed(() => props.profile?.page || DEFAULT_PAGE_SETTINGS);
const pageNumbers = computed(() => settings.value.pageNumbers);
const hasPageNumbers = computed(() => pageNumbers.value?.enabled || false);

const headerContentHeight = computed(() => {
	if (!pageNumbers.value?.enabled || pageNumbers.value.position !== 'top') {
		return 0;
	}
	const tempDiv = document.createElement('div');
	tempDiv.style.position = 'absolute';
	tempDiv.style.visibility = 'hidden';
	tempDiv.style.width = `${dimensions.value.pageWidth}px`;
	tempDiv.innerHTML = renderPageNumber(1, 1, 'top');
	document.body.appendChild(tempDiv);
	const height = tempDiv.scrollHeight;
	document.body.removeChild(tempDiv);
	return height;
});

const contentHeight = computed(() => 
	dimensions.value.pageHeight 
		- dimensions.value.marginTop 
		- dimensions.value.marginBottom 
		- headerContentHeight.value
);

const contentWidth = computed(() => 
	dimensions.value.pageWidth - dimensions.value.marginLeft - dimensions.value.marginRight
);

const headerContent = computed(() => 
	hasPageNumbers.value && pageNumbers.value?.position === 'top'
);

const footerContent = computed(() => 
	hasPageNumbers.value && pageNumbers.value?.position === 'bottom'
);

const zoom = ref(100);
const pages = ref<string[]>([props.html]);
const isUpdatingPages = ref(false);
const scrollContainerRef = ref<HTMLDivElement | null>(null);
const isDragging = ref(false);
const dragStart = ref({ x: 0, y: 0 });
const scrollPosition = ref({ left: 0, top: 0 });
const hasMoved = ref(false);

let splitTimeout: ReturnType<typeof setTimeout> | null = null;
let splitIdleCallback: number | null = null;

function scheduleSplit(callback: () => void) {
	if (typeof requestIdleCallback !== 'undefined') {
		splitIdleCallback = requestIdleCallback(
			callback,
			{ timeout: 1000 }
		);
	} else {
		splitTimeout = setTimeout(callback, 0);
	}
}

watch([() => props.html, contentHeight, contentWidth], () => {
	if (!props.html) {
		pages.value = [''];
		isUpdatingPages.value = false;
		return;
	}

	if (splitTimeout) {
		clearTimeout(splitTimeout);
		splitTimeout = null;
	}
	if (splitIdleCallback !== null) {
		if (typeof cancelIdleCallback !== 'undefined') {
			cancelIdleCallback(splitIdleCallback);
		}
		splitIdleCallback = null;
	}

	isUpdatingPages.value = true;

	scheduleSplit(() => {
		const splitPages = splitIntoPages(props.html, contentHeight.value, contentWidth.value);
		pages.value = splitPages;
		isUpdatingPages.value = false;
		splitTimeout = null;
		splitIdleCallback = null;
	});
}, { immediate: true });

onUnmounted(() => {
	if (splitTimeout) {
		clearTimeout(splitTimeout);
	}
	if (splitIdleCallback !== null && typeof cancelIdleCallback !== 'undefined') {
		cancelIdleCallback(splitIdleCallback);
	}
});

const totalPages = computed(() => pages.value.length);

function handleMouseDown(e: MouseEvent) {
	if (e.button === 0) {
		isDragging.value = true;
		hasMoved.value = false;
		dragStart.value = { x: e.clientX, y: e.clientY };
		if (scrollContainerRef.value) {
			scrollPosition.value = {
				left: scrollContainerRef.value.scrollLeft,
				top: scrollContainerRef.value.scrollTop,
			};
		}
	}
}

function handleMouseMove(e: MouseEvent) {
	if (isDragging.value) {
		const deltaX = Math.abs(dragStart.value.x - e.clientX);
		const deltaY = Math.abs(dragStart.value.y - e.clientY);
		
		if (deltaX > 5 || deltaY > 5) {
			hasMoved.value = true;
			if (scrollContainerRef.value) {
				e.preventDefault();
				const scrollDeltaX = dragStart.value.x - e.clientX;
				const scrollDeltaY = dragStart.value.y - e.clientY;
				scrollContainerRef.value.scrollLeft = scrollPosition.value.left + scrollDeltaX;
				scrollContainerRef.value.scrollTop = scrollPosition.value.top + scrollDeltaY;
			}
		}
	}
}

function handleMouseUp() {
	isDragging.value = false;
	hasMoved.value = false;
}

function handleMouseLeave() {
	isDragging.value = false;
	hasMoved.value = false;
}
</script>

<style scoped>
.document-preview-wrapper {
	display: flex;
	flex-direction: column;
	height: 100%;
	background: #525659;
}

.zoom-controls {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	padding: 0.5rem 1rem;
	background: #3d4043;
	border-bottom: 1px solid #525659;
}

.zoom-btn {
	padding: 0.25rem 0.5rem;
	background: #525659;
	color: white;
	border: 1px solid #6d7073;
	border-radius: 4px;
	cursor: pointer;
	font-size: 0.875rem;
	font-family: inherit;
	transition: all 0.2s;
}

.zoom-btn:hover {
	background: #6d7073;
}

.zoom-reset {
	margin-left: 0.5rem;
}

.zoom-slider {
	flex: 1;
	max-width: 200px;
}

.zoom-value {
	color: white;
	font-size: 0.875rem;
	min-width: 50px;
	text-align: center;
}

.preview-container {
	background: #525659;
	padding: 20px;
	flex: 1;
	overflow: auto;
	position: relative;
	cursor: default;
}

.preview-container.dragging {
	cursor: grabbing;
}

.preview-content {
	display: flex;
	flex-direction: column;
	align-items: center;
}

.document-page {
	background: white;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
	page-break-after: always;
	page-break-inside: avoid;
	position: relative;
}

.page-header {
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	width: 100%;
	z-index: 1;
}

.page-content {
	width: 100%;
	height: 100%;
	/* font-family removed - elements use inline styles from profile */
	font-size: 14pt;
	line-height: 1.5;
	color: #1a1a1a;
	overflow: visible;
	box-sizing: border-box;
}

.page-content :deep(ul.custom-dash-list) {
	list-style-type: none;
	padding-left: 1.5em;
}

.page-content :deep(ul.custom-dash-list > li::before) {
	content: '– ';
	margin-left: -1.5em;
	float: left;
	width: 1.2em;
	text-align: right;
}

.page-content :deep(ol) {
	list-style-type: decimal;
	padding-left: 1.5em;
}

.page-content :deep(li) {
	margin-left: 0.5em;
}

.page-footer {
	position: absolute;
	bottom: 0;
	left: 0;
	right: 0;
	width: 100%;
	display: flex;
	flex-direction: column;
	justify-content: flex-end;
	z-index: 1;
}
</style>
