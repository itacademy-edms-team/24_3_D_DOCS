<template>
	<div class="document-print-page">
		<div v-if="isLoading" class="document-print-page__status">Подготовка печати...</div>
		<div v-else-if="errorMessage" class="document-print-page__status document-print-page__status--error">
			{{ errorMessage }}
		</div>
		<DocumentPreview
			v-else
			:content="content"
			:profileId="effectiveProfileId"
			:titlePageId="effectiveTitlePageId"
			:titlePageVariables="titlePageVariables"
			:documentId="documentId"
			:tableOfContents="tableOfContents"
			:printMode="true"
		/>
	</div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import DocumentPreview from '@/widgets/document-preview/DocumentPreview.vue';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import type { Document, TocItem } from '@/entities/document/types';

declare global {
	interface Window {
		__PRINT_READY__?: boolean;
	}
}

const route = useRoute();
const documentId = computed(() => route.params.id as string);
const requestedTitlePageId = computed(() => {
	const value = route.query.titlePageId;
	return typeof value === 'string' && value.length > 0 ? value : undefined;
});

const requestedProfileId = computed(() => {
	const value = route.query.profileId;
	return typeof value === 'string' && value.length > 0 ? value : undefined;
});

const forceNoTitlePage = computed(() => route.query.noTitlePage === '1');

const isLoading = ref(true);
const errorMessage = ref('');
const documentData = ref<Document | null>(null);
const content = ref('');
const tableOfContents = ref<TocItem[]>([]);
const stablePasses = ref(0);

let stabilityTimer: ReturnType<typeof setInterval> | null = null;

const titlePageVariables = computed<Record<string, string>>(() => {
	const meta = documentData.value?.metadata;
	if (!meta) return {};

	const vars: Record<string, string> = {};
	if (meta.title) vars.Title = meta.title;
	if (meta.author) vars.Author = meta.author;
	if (meta.year) vars.Year = meta.year;
	if (meta.group) vars.Group = meta.group;
	if (meta.city) vars.City = meta.city;
	if (meta.supervisor) vars.Supervisor = meta.supervisor;
	if (meta.documentType) vars.DocumentType = meta.documentType;
	if (meta.additionalFields) {
		Object.assign(vars, meta.additionalFields);
	}
	return vars;
});

const effectiveProfileId = computed(() => {
	return requestedProfileId.value ?? documentData.value?.profileId;
});

const effectiveTitlePageId = computed(() => {
	if (forceNoTitlePage.value) return undefined;
	return requestedTitlePageId.value ?? documentData.value?.titlePageId;
});

function setPrintReady(value: boolean) {
	window.__PRINT_READY__ = value;
}

function stopStabilityCheck() {
	if (stabilityTimer) {
		clearInterval(stabilityTimer);
		stabilityTimer = null;
	}
}

function isPreviewStable(): boolean {
	const dom = window.document;
	const pages = dom.querySelectorAll('.document-preview__page');
	if (pages.length === 0) {
		return false;
	}

	const hasLoadingIndicator = dom.querySelector('.document-preview__loading');
	if (hasLoadingIndicator) {
		return false;
	}

	// Pagination/title HTML can run without the "Загрузка..." row; don't signal ready mid-update.
	if (dom.querySelector('.document-preview--paginating')) {
		return false;
	}

	const images = Array.from(dom.querySelectorAll('img')) as HTMLImageElement[];
	return images.every((img) => img.complete);
}

function startStabilityCheck() {
	stopStabilityCheck();
	stablePasses.value = 0;

	stabilityTimer = setInterval(() => {
		if (!isPreviewStable()) {
			stablePasses.value = 0;
			setPrintReady(false);
			return;
		}

		stablePasses.value += 1;
		if (stablePasses.value >= 3) {
			setPrintReady(true);
			stopStabilityCheck();
		}
	}, 250);
}

async function loadPrintData() {
	setPrintReady(false);
	isLoading.value = true;
	errorMessage.value = '';

	try {
		const [doc, toc] = await Promise.all([
			DocumentAPI.getById(documentId.value),
			DocumentAPI.getTableOfContents(documentId.value),
		]);
		documentData.value = doc;
		content.value = doc.content || '';
		tableOfContents.value = toc;
		await nextTick();
		startStabilityCheck();
	} catch (error) {
		console.error('Failed to prepare print page:', error);
		errorMessage.value = 'Не удалось подготовить данные для печати';
		setPrintReady(false);
	} finally {
		isLoading.value = false;
	}
}

onMounted(async () => {
	await loadPrintData();
});

onBeforeUnmount(() => {
	stopStabilityCheck();
	setPrintReady(false);
});
</script>

<style scoped>
.document-print-page {
	min-height: 100vh;
	background: #fff;
}

.document-print-page__status {
	padding: 24px;
	font-size: 14px;
	color: #111827;
}

.document-print-page__status--error {
	color: #b91c1c;
}
</style>
