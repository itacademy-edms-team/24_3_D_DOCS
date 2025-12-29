<template>
	<div class="content-editor">
		<!-- Header -->
		<div class="content-editor__header">
			<div class="content-editor__header-left">
				<button class="content-editor__back-btn" @click="handleBack" title="Вернуться к списку">
					<svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor">
						<path d="M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z"/>
					</svg>
				</button>
				<div class="content-editor__title-wrapper">
					<input
						class="content-editor__title"
						type="text"
						v-model="documentName"
						@input="handleNameChange"
						placeholder="Название документа"
					/>
				</div>
			</div>

			<div class="content-editor__header-center">
				<div class="content-editor__header-tabs">
					<button
						class="content-editor__tab-btn"
						:class="{ 'content-editor__tab-btn--active': activeTab === 'editor' }"
						@click="activeTab = 'editor'"
					>
						Редактор
					</button>
					<button
						class="content-editor__tab-btn"
						:class="{ 'content-editor__tab-btn--active': activeTab === 'titlePage' }"
						@click="activeTab = 'titlePage'"
					>
						Титульник
					</button>
				</div>
				<div class="content-editor__header-selectors">
					<div class="select-group">
						<span class="select-label">Стиль</span>
						<div class="select-wrapper">
							<select
								v-model="selectedProfileId"
								class="content-editor__profile-select"
								:disabled="isLoadingProfiles || isUpdatingProfile"
								@change="handleProfileChange"
							>
								<option value="">Без профиля</option>
								<option
									v-for="profile in profiles"
									:key="profile.id"
									:value="profile.id"
								>
									{{ profile.name }}
								</option>
							</select>
						</div>
					</div>
					<div class="select-group">
						<span class="select-label">Титул</span>
						<div class="select-wrapper">
							<select
								v-model="selectedTitlePageId"
								class="content-editor__title-page-select"
								:disabled="isLoadingTitlePages || isUpdatingTitlePage"
								@change="handleTitlePageChange"
							>
								<option value="">Без титульника</option>
								<option
									v-for="titlePage in titlePages"
									:key="titlePage.id"
									:value="titlePage.id"
								>
									{{ titlePage.name }}
								</option>
							</select>
						</div>
					</div>
				</div>
			</div>

			<div class="content-editor__header-actions">
				<div class="action-group">
					<button
						class="content-editor__header-btn"
						@click="handleDownloadPdf"
						:disabled="isDownloadingPdf || !documentId"
						title="Скачать PDF"
					>
						<svg v-if="!isDownloadingPdf" viewBox="0 0 24 24" width="18" height="18" fill="currentColor">
							<path d="M20 2H8c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H8V4h12v12zM10 10h1.5v1H10v-1zm0 3h1.5v1H10v-1zm0-6h1.5v1H10V7zm4 3h1.5v1H14v-1zm0 3h1.5v1H14v-1zm0-6h1.5v1H14V7zm4 3h1.5v1H18v-1zm0 3h1.5v1H18v-1zm0-6h1.5v1H18V7z"/>
						</svg>
						<span v-else class="spinner-small"></span>
					</button>
					<button
						class="content-editor__header-btn"
						@click="handleExportDdoc"
						:disabled="isExportingDdoc || !documentId"
						title="Экспорт .ddoc"
					>
						<svg v-if="!isExportingDdoc" viewBox="0 0 24 24" width="18" height="18" fill="currentColor">
							<path d="M20 6h-8l-2-2H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm0 12H4V8h16v10zM8 13.01l1.41 1.41L11 12.84V17h2v-4.16l1.59 1.59L16 13.01 12 9l-4 4.01z"/>
						</svg>
						<span v-else class="spinner-small"></span>
					</button>
				</div>
				
				<div class="action-group">
					<button
						class="content-editor__header-btn"
						:class="{ 'content-editor__header-btn--active': showPreview }"
						@click="showPreview = !showPreview"
						:title="showPreview ? 'Скрыть превью' : 'Показать превью'"
					>
						<svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor">
							<path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z"/>
						</svg>
					</button>
					<button
						class="content-editor__header-btn"
						@click="swapped = !swapped"
						v-if="showEditor && showPreview"
						title="Поменять местами"
					>
						<svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor">
							<path d="M6.99 11L3 15l3.99 4v-3H14v-2H6.99v-3zM21 9l-3.99-4v3H10v2h7.01v3L21 9z"/>
						</svg>
					</button>
				</div>

				<div class="content-editor__embedding-controls">
					<EmbeddingCoverageChart
						v-if="embeddingCoverage !== null"
						:coverage-percentage="embeddingCoverage"
					/>
					<button
						class="content-editor__header-btn"
						@click="handleUpdateEmbeddings"
						:disabled="isUpdatingEmbeddings"
						title="Обновить эмбеддинги"
					>
						<svg v-if="!isUpdatingEmbeddings" viewBox="0 0 24 24" width="18" height="18" fill="currentColor">
							<path d="M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z"/>
						</svg>
						<span v-else class="spinner-small"></span>
					</button>
				</div>
				
				<div class="action-group">
					<button 
						class="content-editor__header-btn ai-btn"
						:class="{ 'ai-btn--active': showAIPanel }"
						@click="handleAIClick"
						title="AI Помощник"
					>
						AI
					</button>
					<button class="content-editor__header-btn" @click="handleSettings" title="Настройки">
						<svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor">
							<path d="M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"/>
						</svg>
					</button>
				</div>
			</div>
		</div>

		<!-- Editor Layout -->
		<div
			class="content-editor__layout"
			:class="{
				'content-editor__layout--swapped': swapped,
				'content-editor__layout--with-ai': showAIPanel
			}"
			:style="showAIPanel ? { marginRight: `${chatDockWidth}px` } : {}"
		>
			<!-- Editor Panel -->
			<div class="content-editor__editor-panel" v-show="showEditor">
				<!-- Markdown Editor -->
				<MarkdownEditor
					v-if="activeTab === 'editor'"
					ref="markdownEditorRef"
					v-model="content"
					:documentId="documentId"
					:diffChanges="diffChanges"
					@update:modelValue="handleContentChange"
				/>
				<!-- Title Page Variables Panel -->
				<TitlePageVariablesPanel
					v-else-if="activeTab === 'titlePage'"
					:titlePageId="selectedTitlePageId"
					:documentId="documentId"
					:variables="titlePageVariables"
					@update:variables="handleVariablesUpdate"
				/>
			</div>

			<!-- Preview Panel -->
			<div class="content-editor__preview-panel" v-show="showPreview">
				<DocumentPreview
					:content="content"
					:profileId="document?.profileId"
					:titlePageId="selectedTitlePageId"
					:titlePageVariables="titlePageVariables"
					:documentId="documentId"
				/>
			</div>
		</div>

		<!-- AI Panel (Chat Dock) -->
		<ChatDock
			v-model:open="showAIPanel"
			:documentId="documentId"
			:startLine="selectedStartLine"
			:endLine="selectedEndLine"
			:pendingChangesByChat="pendingChangesByChat"
			@clearSelection="selectedStartLine = undefined; selectedEndLine = undefined"
			@documentUpdated="handleDocumentUpdated"
			@documentChanged="handleDocumentChanged"
			@acceptChanges="handleAcceptChanges"
			@rejectChanges="handleRejectChanges"
			@discardChatChanges="handleDiscardChatChanges"
			@width-changed="handleChatDockWidthChanged"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Button from '@/shared/ui/Button/Button.vue';
import MarkdownEditor from '@/widgets/markdown-editor/MarkdownEditor.vue';
import DocumentPreview from '@/widgets/document-preview/DocumentPreview.vue';
import TitlePageVariablesPanel from '@/widgets/title-page-variables/TitlePageVariablesPanel.vue';
import ChatDock from '@/features/agent/ChatDock.vue';
import EmbeddingCoverageChart from '@/shared/ui/EmbeddingCoverageChart/EmbeddingCoverageChart.vue';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';
import { useDebounceFn } from '@vueuse/core';
import type { Document, DocumentMetadata } from '@/entities/document/types';
import type { Profile } from '@/entities/profile/types';

const route = useRoute();
const router = useRouter();
const documentId = computed(() => route.params.id as string);

const document = ref<Document | null>(null);
const content = ref('');
const showEditor = ref(true);
const showPreview = ref(true);
const swapped = ref(false);
const activeTab = ref<'editor' | 'titlePage'>('editor');
const markdownEditorRef = ref<InstanceType<typeof MarkdownEditor> | null>(null);
const embeddingCoverage = ref<number | null>(null);
const isUpdatingEmbeddings = ref(false);
const isDownloadingPdf = ref(false);
const isExportingDdoc = ref(false);

const profiles = ref<Profile[]>([]);
const isLoadingProfiles = ref(false);
const selectedProfileId = ref<string>('');
const isUpdatingProfile = ref(false);

const titlePages = ref<Array<{ id: string; name: string }>>([]);
const isLoadingTitlePages = ref(false);
const selectedTitlePageId = ref<string>('');
const isUpdatingTitlePage = ref(false);

const currentProfileId = computed(() => {
	return document.value?.profileId || '';
});

const currentTitlePageId = computed(() => {
	return document.value?.titlePageId || '';
});

const titlePageVariables = computed<Record<string, string>>(() => {
	if (!document.value?.metadata) {
		return {};
	}
	const meta = document.value.metadata;
	const vars: Record<string, string> = {};
	
	// Маппинг стандартных полей
	if (meta.title) vars.Title = meta.title;
	if (meta.author) vars.Author = meta.author;
	if (meta.year) vars.Year = meta.year;
	if (meta.group) vars.Group = meta.group;
	if (meta.city) vars.City = meta.city;
	if (meta.supervisor) vars.Supervisor = meta.supervisor;
	if (meta.documentType) vars.DocumentType = meta.documentType;
	
	// Дополнительные поля
	if (meta.additionalFields) {
		Object.assign(vars, meta.additionalFields);
	}
	
	return vars;
});

const handleBack = () => {
	router.push('/dashboard');
};

const showAIPanel = ref(false);
const selectedStartLine = ref<number | undefined>();
const selectedEndLine = ref<number | undefined>();
const chatDockWidth = ref(400);

const handleAIClick = () => {
	showAIPanel.value = !showAIPanel.value;
};

const handleChatDockWidthChanged = (width: number) => {
	chatDockWidth.value = width;
};

const handleSettings = () => {
	// TODO: Открыть настройки документа
	console.log('Settings clicked');
};

const downloadFile = (blob: Blob, filename: string) => {
	try {
		const url = URL.createObjectURL(blob);
		// Используем window.document чтобы избежать проблем с минификацией
		const doc = typeof window !== 'undefined' ? window.document : document;
		const link = doc.createElement('a');
		link.href = url;
		link.download = filename;
		doc.body.appendChild(link);
		link.click();
		doc.body.removeChild(link);
		URL.revokeObjectURL(url);
	} catch (error) {
		console.error('Error in downloadFile:', error);
		throw error;
	}
};

const handleDownloadPdf = async () => {
	if (!documentId.value || isDownloadingPdf.value) return;

	isDownloadingPdf.value = true;

	try {
		const filename = document.value?.name 
			? `${document.value.name}.pdf` 
			: `document-${documentId.value}.pdf`;
		
		// Always generate a new PDF to ensure it's up-to-date with the latest content
		// Use selectedTitlePageId if available
		const blob = await DocumentAPI.generatePdf(
			documentId.value, 
			selectedTitlePageId.value || undefined
		);
		
		downloadFile(blob, filename);
	} catch (error: any) {
		console.error('Failed to generate PDF:', error);
		
		let errorMessage = 'Не удалось сгенерировать PDF. ';
		if (error?.response?.status === 500) {
			errorMessage += 'Ошибка на сервере при генерации PDF. Проверьте логи бэкенда.';
		} else if (error?.response?.status === 404) {
			errorMessage += 'Документ не найден.';
		} else if (error?.message) {
			errorMessage += error.message;
		} else {
			errorMessage += 'Попробуйте позже.';
		}
		
		alert(errorMessage);
	} finally {
		isDownloadingPdf.value = false;
	}
};

const handleExportDdoc = async () => {
	if (!documentId.value || isExportingDdoc.value) return;

	isExportingDdoc.value = true;

	try {
		const filename = document.value?.name 
			? `${document.value.name}.ddoc` 
			: `document-${documentId.value}.ddoc`;
		
		const blob = await DocumentAPI.exportDocument(documentId.value);
		downloadFile(blob, filename);
	} catch (error: any) {
		console.error('Failed to export .ddoc:', error);
		
		let errorMessage = 'Не удалось экспортировать документ. ';
		if (error?.response?.status === 500) {
			errorMessage += 'Ошибка на сервере при экспорте. Проверьте логи бэкенда.';
		} else if (error?.response?.status === 404) {
			errorMessage += 'Документ не найден.';
		} else if (error?.message) {
			errorMessage += error.message;
		} else {
			errorMessage += 'Неизвестная ошибка.';
		}
		
		alert(errorMessage);
	} finally {
		isExportingDdoc.value = false;
	}
};

const handleUpdateEmbeddings = async () => {
	if (!documentId.value || isUpdatingEmbeddings.value) return;

	const startTime = Date.now();
	isUpdatingEmbeddings.value = true;
	console.log('🔄 Начато обновление эмбеддингов...');
	
	// #region agent log
	fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'ContentEditor.vue:239',message:'handleUpdateEmbeddings started',data:{documentId:documentId.value,startTime},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
	// #endregion
	
	try {
		const apiStartTime = Date.now();
		await DocumentAPI.updateEmbeddings(documentId.value);
		const apiEndTime = Date.now();
		const apiDuration = apiEndTime - apiStartTime;
		
		// #region agent log
		fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'ContentEditor.vue:250',message:'API updateEmbeddings completed',data:{apiDuration,apiStartTime,apiEndTime},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
		// #endregion
		
		console.log('✅ Эмбеддинги успешно обновлены');
		
		// Reload embedding status after update
		const statusStartTime = Date.now();
		await nextTick();
		if (markdownEditorRef.value) {
			await markdownEditorRef.value.loadEmbeddingStatus();
		}
		const statusEndTime = Date.now();
		const statusDuration = statusEndTime - statusStartTime;
		
		// #region agent log
		fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'ContentEditor.vue:260',message:'Status reload completed',data:{statusDuration},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
		// #endregion
		
		const totalDuration = Date.now() - startTime;
		// #region agent log
		fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'ContentEditor.vue:265',message:'handleUpdateEmbeddings completed',data:{totalDuration,apiDuration,statusDuration},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
		// #endregion
	} catch (error: any) {
		const errorTime = Date.now();
		const errorDuration = errorTime - startTime;
		console.error('❌ Ошибка при обновлении эмбеддингов:', error);
		// #region agent log
		fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'ContentEditor.vue:272',message:'handleUpdateEmbeddings error',data:{errorDuration,errorType:error?.constructor?.name,errorMessage:error?.message,errorResponse:error?.response?.data,errorStatus:error?.response?.status,errorStack:error?.stack?.substring(0,500)},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
		// #endregion
		alert(`Ошибка при обновлении эмбеддингов: ${error?.message || error?.toString() || 'Неизвестная ошибка'}. Проверьте консоль для подробностей.`);
	} finally {
		isUpdatingEmbeddings.value = false;
	}
};

const documentName = ref<string>('');

const handleNameChange = useDebounceFn(async () => {
	if (!document.value) return;
	
	const newName = documentName.value.trim() || 'Документ';
	const previousName = document.value.name;
	
	// Пропускаем если название не изменилось
	if (newName === previousName) return;
	
	try {
		await DocumentAPI.update(documentId.value, {
			name: newName,
		});
		// Обновляем локальное состояние после успешного сохранения
		document.value.name = newName;
	} catch (error) {
		console.error('Failed to update document name:', error);
		// Откатываем к предыдущему значению при ошибке
		documentName.value = previousName;
		alert('Ошибка при сохранении названия документа');
	}
}, 1000);

const handleContentChange = useDebounceFn(async (newContent: string) => {
	if (document.value) {
		try {
			await DocumentAPI.updateContent(documentId.value, newContent);
		} catch (error) {
			console.error('Failed to save content:', error);
		}
	}
}, 1000);

const handleProfileChange = useDebounceFn(async (event: Event) => {
	if (!document.value) return;

	const target = event.target as HTMLSelectElement;
	const newProfileId = target.value || undefined;

	isUpdatingProfile.value = true;
	try {
		await DocumentAPI.update(documentId.value, {
			profileId: newProfileId,
		});
		document.value.profileId = newProfileId;
	} catch (error) {
		console.error('Failed to update profile:', error);
		// Revert selection on error
		selectedProfileId.value = currentProfileId.value;
	} finally {
		isUpdatingProfile.value = false;
	}
}, 300);

const handleTitlePageChange = useDebounceFn(async (event: Event) => {
	if (!document.value) return;

	const target = event.target as HTMLSelectElement;
	const newTitlePageId = target.value || undefined;

	isUpdatingTitlePage.value = true;
	try {
		await DocumentAPI.update(documentId.value, {
			titlePageId: newTitlePageId,
		});
		document.value.titlePageId = newTitlePageId;
	} catch (error) {
		console.error('Failed to update title page:', error);
		// Revert selection on error
		selectedTitlePageId.value = currentTitlePageId.value;
	} finally {
		isUpdatingTitlePage.value = false;
	}
}, 300);

const handleVariablesUpdate = async (newVariables: Record<string, string>) => {
	if (!document.value) return;
	
	// Преобразуем переменные обратно в metadata
	const metadata: DocumentMetadata = {
		...document.value.metadata,
		title: newVariables.Title,
		author: newVariables.Author,
		year: newVariables.Year,
		group: newVariables.Group,
		city: newVariables.City,
		supervisor: newVariables.Supervisor,
		documentType: newVariables.DocumentType,
		additionalFields: {},
	};
	
	// Сохраняем дополнительные поля
	const standardFields = ['Title', 'Author', 'Year', 'Group', 'City', 'Supervisor', 'DocumentType'];
	for (const [key, value] of Object.entries(newVariables)) {
		if (!standardFields.includes(key)) {
			if (!metadata.additionalFields) {
				metadata.additionalFields = {};
			}
			metadata.additionalFields[key] = value;
		}
	}
	
	try {
		await DocumentAPI.updateMetadata(documentId.value, metadata);
		if (document.value) {
			document.value.metadata = metadata;
		}
	} catch (error) {
		console.error('Failed to update metadata:', error);
	}
};

const loadProfiles = async () => {
	isLoadingProfiles.value = true;
	try {
		const loadedProfiles = await ProfileAPI.getAll(true);
		profiles.value = loadedProfiles;
	} catch (error) {
		console.error('Failed to load profiles:', error);
	} finally {
		isLoadingProfiles.value = false;
	}
};

const loadTitlePages = async () => {
	isLoadingTitlePages.value = true;
	try {
		const loadedTitlePages = await TitlePageAPI.getAll();
		titlePages.value = loadedTitlePages;
	} catch (error) {
		console.error('Failed to load title pages:', error);
	} finally {
		isLoadingTitlePages.value = false;
	}
};

const loadDocument = async () => {
	try {
		const doc = await DocumentAPI.getById(documentId.value);
		document.value = doc;
		documentName.value = doc.name || 'Документ';
		content.value = doc.content || '';
		selectedProfileId.value = doc.profileId || '';
		selectedTitlePageId.value = doc.titlePageId || '';
	} catch (error) {
		console.error('Failed to load document:', error);
	}
};

const handleDocumentUpdated = async () => {
	// Reload document content after agent edits
	await loadDocument();
	// Update embedding status after document is updated
	await nextTick();
	if (markdownEditorRef.value) {
		markdownEditorRef.value.loadEmbeddingStatus();
	}
	// Don't clear diff changes - they should remain visible until accepted/rejected
};

// Track diff changes for visualization (only added and deleted, no modified)
// Structure: { lineNumber, type, timestamp, stepNumber, operation, originalText?, newText?, status, chatId? }
interface DiffChange {
	lineNumber: number; // 0-based
	type: 'added' | 'deleted';
	timestamp: number;
	stepNumber: number;
	operation: 'insert' | 'update' | 'delete';
	originalText?: string; // для deleted/update
	newText?: string; // для added/update
	status: 'pending' | 'applied' | 'rejected';
	chatId?: string;
}

const diffChanges = ref<DiffChange[]>([]);

// Storage key for persisted diff changes
const getDiffStorageKey = () => `diff-changes-${documentId.value}`;

// Load persisted diff changes
const loadPersistedDiffChanges = () => {
	try {
		const saved = localStorage.getItem(getDiffStorageKey());
		if (saved) {
			const parsed = JSON.parse(saved);
			if (Array.isArray(parsed)) {
				diffChanges.value = parsed;
			}
		}
	} catch (e) {
		console.error('Error loading persisted diff changes:', e);
	}
};

// Save diff changes to localStorage
const saveDiffChanges = () => {
	try {
		localStorage.setItem(getDiffStorageKey(), JSON.stringify(diffChanges.value));
	} catch (e) {
		console.error('Error saving diff changes:', e);
	}
};

// Store old content before update to show deleted lines
const oldContent = ref<string>('');

const handleDocumentChanged = async (change: { 
	stepNumber: number; 
	operation: string; 
	chatId: string;
	changes: Array<{ lineNumber: number; type: 'added' | 'deleted'; text?: string }>;
}) => {
	// Save old content before reloading (for showing deleted lines in update operations)
	if (change.operation === 'update' && content.value) {
		oldContent.value = content.value;
	}
	
	// Reload document content immediately when agent makes changes
	await loadDocument();
	
	// Add diff changes for visualization
	if (change.changes && change.changes.length > 0) {
		const timestamp = Date.now();
		
		// Get old content lines for update operations
		const oldLines = oldContent.value ? oldContent.value.split('\n') : [];
		const currentLines = content.value ? content.value.split('\n') : [];
		
		// For update operations, show deleted lines first, then added lines
		if (change.operation === 'update') {
			// First, add deleted lines with original text
			change.changes
				.filter(c => c.type === 'deleted')
				.forEach(changeItem => {
					const originalText = oldLines[changeItem.lineNumber] || '';
					diffChanges.value.push({
						lineNumber: changeItem.lineNumber,
						type: 'deleted',
						timestamp,
						stepNumber: change.stepNumber,
						operation: change.operation as 'insert' | 'update' | 'delete',
						originalText,
						chatId: change.chatId,
						status: 'pending'
					});
				});
			
			// Then add new lines
			change.changes
				.filter(c => c.type === 'added')
				.forEach(changeItem => {
					diffChanges.value.push({
						lineNumber: changeItem.lineNumber,
						type: 'added',
						timestamp: Date.now(),
						stepNumber: change.stepNumber,
						operation: change.operation as 'insert' | 'update' | 'delete',
						newText: changeItem.text,
						chatId: change.chatId,
						status: 'pending'
					});
				});
		} else {
			// For insert and delete, add all changes immediately
			change.changes.forEach(changeItem => {
				// Для delete операций нужно сохранить originalText
				const originalText = changeItem.type === 'deleted' 
					? (oldLines[changeItem.lineNumber] || currentLines[changeItem.lineNumber] || '')
					: undefined;
				
				diffChanges.value.push({
					lineNumber: changeItem.lineNumber,
					type: changeItem.type,
					timestamp,
					stepNumber: change.stepNumber,
					operation: change.operation as 'insert' | 'update' | 'delete',
					newText: changeItem.text,
					originalText,
					chatId: change.chatId,
					status: 'pending'
				});
			});
		}
		
		// Save changes to localStorage (persistent)
		saveDiffChanges();
	}
};

// Handle accept/reject diff actions from editor
const handleDiffAction = async (action: { lineNumber: number; type: 'added' | 'deleted'; action: 'accept' | 'reject' }) => {
	const changeIndex = diffChanges.value.findIndex(
		c => c.lineNumber === action.lineNumber && c.type === action.type && c.status === 'pending'
	);
	
	if (changeIndex === -1) return;
	
	const change = diffChanges.value[changeIndex];
	
	if (action.action === 'accept') {
		// Accept: mark as applied and remove from diff
		change.status = 'applied';
		diffChanges.value.splice(changeIndex, 1);
		saveDiffChanges();
	} else {
		// Reject: revert the change
		if (change.type === 'added') {
			// Remove added line
			const lines = content.value.split('\n');
			if (change.lineNumber < lines.length) {
				lines.splice(change.lineNumber, 1);
				content.value = lines.join('\n');
				await handleContentChange(content.value);
			}
		} else if (change.type === 'deleted' && change.originalText) {
			// Restore deleted line
			const lines = content.value.split('\n');
			lines.splice(change.lineNumber, 0, change.originalText);
			content.value = lines.join('\n');
			await handleContentChange(content.value);
		}
		
		// Mark as rejected and remove from diff
		change.status = 'rejected';
		diffChanges.value.splice(changeIndex, 1);
		saveDiffChanges();
	}
};

// Handle accept/reject changes from chat (by step number)
const handleAcceptChanges = (stepNumber: number) => {
	// Mark all pending changes from this step as applied and remove from diff
	const indices: number[] = [];
	diffChanges.value.forEach((c, index) => {
		if (c.stepNumber === stepNumber && c.status === 'pending') {
			c.status = 'applied';
			indices.push(index);
		}
	});
	// Remove in reverse order
	indices.reverse().forEach(index => {
		diffChanges.value.splice(index, 1);
	});
	saveDiffChanges();
};

const handleRejectChanges = async (stepNumber: number) => {
	// Revert all pending changes from this step
	const changesToRevert = diffChanges.value.filter(
		c => c.stepNumber === stepNumber && c.status === 'pending'
	);
	const lines = content.value.split('\n');
	
	// Sort by line number in reverse order to avoid index shifting issues
	changesToRevert.sort((a, b) => b.lineNumber - a.lineNumber);
	
	for (const change of changesToRevert) {
		if (change.type === 'added') {
			// Remove added line
			if (change.lineNumber < lines.length) {
				lines.splice(change.lineNumber, 1);
			}
		} else if (change.type === 'deleted' && change.originalText) {
			// Restore deleted line
			lines.splice(change.lineNumber, 0, change.originalText);
		}
		// Mark as rejected
		change.status = 'rejected';
	}
	
	content.value = lines.join('\n');
	await handleContentChange(content.value);
	
	// Remove from diff changes
	const indices: number[] = [];
	diffChanges.value.forEach((c, index) => {
		if (c.stepNumber === stepNumber && c.status === 'rejected') {
			indices.push(index);
		}
	});
	indices.reverse().forEach(index => {
		diffChanges.value.splice(index, 1);
	});
	saveDiffChanges();
};

const pendingChangesByChat = computed<Record<string, boolean>>(() => {
	const map: Record<string, boolean> = {};
	diffChanges.value.forEach((c) => {
		if (c.status === 'pending' && c.chatId) {
			map[c.chatId] = true;
		}
	});
	return map;
});

const handleDiscardChatChanges = async (chatId: string) => {
	// Откатываем все pending‑изменения для указанного чата
	const changesToRevert = diffChanges.value.filter(
		(c) => c.chatId === chatId && c.status === 'pending'
	);

	if (changesToRevert.length === 0) {
		return;
	}

	const lines = content.value.split('\n');

	// Сортируем в обратном порядке по lineNumber, чтобы не ломать индексы
	changesToRevert.sort((a, b) => b.lineNumber - a.lineNumber);

	for (const change of changesToRevert) {
		if (change.type === 'added') {
			if (change.lineNumber < lines.length) {
				lines.splice(change.lineNumber, 1);
			}
		} else if (change.type === 'deleted' && change.originalText) {
			lines.splice(change.lineNumber, 0, change.originalText);
		}
		change.status = 'rejected';
	}

	content.value = lines.join('\n');
	await handleContentChange(content.value);

	// Удаляем все изменения этого чата из списка diffChanges
	const indices: number[] = [];
	diffChanges.value.forEach((c, index) => {
		if (c.chatId === chatId && c.status === 'rejected') {
			indices.push(index);
		}
	});
	indices.reverse().forEach((index) => {
		diffChanges.value.splice(index, 1);
	});
	saveDiffChanges();
};

// Update embedding coverage from editor status
watch(
	() => markdownEditorRef.value?.embeddingStatus,
	(status) => {
		if (status) {
			embeddingCoverage.value = status.coveragePercentage;
		} else {
			embeddingCoverage.value = null;
		}
	},
	{ deep: true }
);

// Update status when AI panel opens (as embeddings are updated when agent processes)
watch(
	() => showAIPanel.value,
	(isOpen) => {
		if (isOpen && markdownEditorRef.value) {
			// Status will be updated when agent processes the document
			// We can trigger a refresh after a delay to account for embedding update
			setTimeout(() => {
				if (markdownEditorRef.value) {
					markdownEditorRef.value.loadEmbeddingStatus();
				}
			}, 2000);
		}
	}
);

// Handle window resize to prevent preview "shaking"
const handleWindowResize = useDebounceFn(() => {
	// Force reflow to prevent visual glitches
	if (showPreview.value) {
		nextTick(() => {
			// Trigger a minimal reflow to stabilize layout
			const previewPanel = document.querySelector('.content-editor__preview-panel');
			if (previewPanel) {
				previewPanel.scrollTop = previewPanel.scrollTop;
			}
		});
	}
}, 100);

onMounted(async () => {
	await loadDocument();
	
	// Load persisted diff changes
	loadPersistedDiffChanges();

	// Load profiles and title pages for selectors
	await Promise.all([loadProfiles(), loadTitlePages()]);
	
	// Add window resize listener
	window.addEventListener('resize', handleWindowResize);
});

onUnmounted(() => {
	window.removeEventListener('resize', handleWindowResize);
});

// Sync selectedProfileId with document profileId when document changes
watch(
	() => document.value?.profileId,
	(newProfileId) => {
		if (selectedProfileId.value !== (newProfileId || '')) {
			selectedProfileId.value = newProfileId || '';
		}
	},
	{ immediate: true }
);

// Sync selectedTitlePageId with document titlePageId when document changes
watch(
	() => document.value?.titlePageId,
	(newTitlePageId) => {
		if (selectedTitlePageId.value !== (newTitlePageId || '')) {
			selectedTitlePageId.value = newTitlePageId || '';
		}
	},
	{ immediate: true }
);

// Sync documentName with document.name when document changes
watch(
	() => document.value?.name,
	(newName) => {
		if (newName && documentName.value !== newName) {
			documentName.value = newName;
		}
	},
	{ immediate: true, flush: 'post' }
);
</script>

<style scoped>
.content-editor {
	display: flex;
	flex-direction: column;
	height: 100vh;
	background: var(--bg-primary);
}

.content-editor__header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: var(--spacing-lg);
	padding: 0 var(--spacing-lg);
	height: 60px;
	min-height: 60px;
	background: var(--bg-primary);
	border-bottom: 1px solid var(--border-color);
	backdrop-filter: blur(12px);
	z-index: 100;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.02);
}

.content-editor__header-left {
	display: flex;
	align-items: center;
	gap: var(--spacing-md);
	flex: 1;
	min-width: 0;
}

.content-editor__back-btn {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 32px;
	height: 32px;
	background: transparent;
	border: 1px solid transparent;
	border-radius: var(--radius-md);
	color: var(--text-secondary);
	cursor: pointer;
	transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}

.content-editor__back-btn:hover {
	background: var(--bg-secondary);
	color: var(--text-primary);
	border-color: var(--border-color);
}

.content-editor__title-wrapper {
	flex: 1;
	min-width: 0;
	max-width: 320px;
}

.content-editor__title {
	width: 100%;
	font-size: 15px;
	font-weight: 600;
	color: var(--text-primary);
	background: transparent;
	border: 1px solid transparent;
	border-radius: var(--radius-sm);
	padding: 6px 10px;
	transition: all 0.2s ease;
	font-family: inherit;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.content-editor__title:hover {
	background: var(--bg-secondary);
	border-color: var(--border-color);
}

.content-editor__title:focus {
	outline: none;
	background: var(--bg-primary);
	border-color: var(--accent);
	box-shadow: 0 0 0 3px var(--accent-light);
}

.content-editor__header-center {
	display: flex;
	align-items: center;
	justify-content: center;
	gap: var(--spacing-xl);
	flex: 2;
}

.content-editor__header-tabs {
	display: flex;
	background: var(--bg-secondary);
	padding: 3px;
	border-radius: var(--radius-lg);
	border: 1px solid var(--border-color);
}

.content-editor__tab-btn {
	padding: 5px 14px;
	font-size: 12px;
	font-weight: 600;
	background: transparent;
	border: none;
	border-radius: var(--radius-md);
	color: var(--text-secondary);
	cursor: pointer;
	transition: all 0.2s ease;
}

.content-editor__tab-btn:hover {
	color: var(--text-primary);
}

.content-editor__tab-btn--active {
	background: var(--bg-primary);
	color: var(--accent);
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.content-editor__header-selectors {
	display: flex;
	gap: var(--spacing-lg);
}

.select-group {
	display: flex;
	align-items: center;
	gap: 10px;
}

.select-label {
	font-size: 10px;
	font-weight: 700;
	text-transform: uppercase;
	letter-spacing: 0.08em;
	color: var(--text-tertiary);
	user-select: none;
}

.select-wrapper {
	position: relative;
	display: flex;
	align-items: center;
}

.content-editor__profile-select,
.content-editor__title-page-select {
	padding: 5px 10px;
	font-size: 12px;
	font-weight: 500;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	color: var(--text-primary);
	cursor: pointer;
	transition: all 0.2s ease;
	min-width: 120px;
	max-width: 180px;
	appearance: none;
	padding-right: 24px;
	background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%23a1a1aa' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'%3E%3C/polyline%3E%3C/svg%3E");
	background-repeat: no-repeat;
	background-position: right 6px center;
}

.content-editor__profile-select:hover:not(:disabled),
.content-editor__title-page-select:hover:not(:disabled) {
	border-color: var(--border-hover);
	background-color: var(--bg-tertiary);
}

.content-editor__profile-select:focus,
.content-editor__title-page-select:focus {
	outline: none;
	border-color: var(--accent);
	background-color: var(--bg-primary);
	box-shadow: 0 0 0 3px var(--accent-light);
}

.content-editor__header-actions {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	gap: var(--spacing-md);
	flex: 1;
	animation: actionsFadeIn 0.4s cubic-bezier(0.4, 0, 0.2, 1) forwards;
}

@keyframes actionsFadeIn {
	from {
		opacity: 0;
		transform: translateX(20px);
	}
	to {
		opacity: 1;
		transform: translateX(0);
	}
}

.action-group {
	display: flex;
	gap: 6px;
	padding-right: var(--spacing-md);
	border-right: 1px solid var(--border-color);
	animation: groupFadeIn 0.3s cubic-bezier(0.4, 0, 0.2, 1) forwards;
}

@keyframes groupFadeIn {
	from {
		opacity: 0;
		transform: translateX(10px);
	}
	to {
		opacity: 1;
		transform: translateX(0);
	}
}

.action-group:last-child {
	border-right: none;
	padding-right: 0;
}

.content-editor__header-btn {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 32px;
	height: 32px;
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	color: var(--text-secondary);
	cursor: pointer;
	transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
	position: relative;
	overflow: hidden;
	opacity: 0;
	transform: translateY(-4px) scale(0.95);
	animation: buttonFadeIn 0.4s cubic-bezier(0.4, 0, 0.2, 1) forwards;
}

/* Stagger animation for buttons */
.content-editor__header-btn:nth-child(1) {
	animation-delay: 0.05s;
}

.content-editor__header-btn:nth-child(2) {
	animation-delay: 0.1s;
}

.content-editor__header-btn:nth-child(3) {
	animation-delay: 0.15s;
}

.content-editor__header-btn:nth-child(4) {
	animation-delay: 0.2s;
}

.content-editor__header-btn:nth-child(5) {
	animation-delay: 0.25s;
}

.content-editor__header-btn:nth-child(6) {
	animation-delay: 0.3s;
}

.content-editor__header-btn:hover:not(:disabled) {
	background: var(--bg-secondary);
	color: var(--text-primary);
	border-color: var(--border-hover);
	transform: translateY(-2px) scale(1.05);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.content-editor__header-btn--active {
	color: var(--accent);
	background: var(--accent-light);
	border-color: var(--accent);
	box-shadow: 0 2px 8px var(--accent-light);
}

.content-editor__header-btn:active:not(:disabled) {
	transform: translateY(0) scale(0.98);
	transition: transform 0.1s cubic-bezier(0.4, 0, 0.2, 1);
}

/* Ripple effect on click */
.content-editor__header-btn::after {
	content: '';
	position: absolute;
	top: 50%;
	left: 50%;
	width: 0;
	height: 0;
	border-radius: 50%;
	background: rgba(255, 255, 255, 0.4);
	transform: translate(-50%, -50%);
	transition: width 0.5s cubic-bezier(0.4, 0, 0.2, 1), 
	            height 0.5s cubic-bezier(0.4, 0, 0.2, 1), 
	            opacity 0.5s cubic-bezier(0.4, 0, 0.2, 1);
	opacity: 0;
	pointer-events: none;
	z-index: 0;
}

.content-editor__header-btn > * {
	position: relative;
	z-index: 1;
}

.content-editor__header-btn:active:not(:disabled)::after {
	width: 300px;
	height: 300px;
	opacity: 0;
	transition: width 0.4s cubic-bezier(0.4, 0, 0.2, 1), 
	            height 0.4s cubic-bezier(0.4, 0, 0.2, 1), 
	            opacity 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes buttonFadeIn {
	from {
		opacity: 0;
		transform: translateY(-4px) scale(0.95);
	}
	to {
		opacity: 1;
		transform: translateY(0) scale(1);
	}
}

.content-editor__embedding-controls {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 0 12px;
	border-right: 1px solid var(--border-color);
}

.ai-btn {
	background: var(--bg-secondary) !important;
	font-size: 11px !important;
	font-weight: 800 !important;
	letter-spacing: -0.02em !important;
}

.ai-btn--active {
	background: var(--accent) !important;
	color: white !important;
	border-color: var(--accent) !important;
	box-shadow: 0 4px 12px var(--accent-light) !important;
}

.content-editor__header-btn:disabled {
	opacity: 0.4;
	cursor: not-allowed;
	filter: grayscale(1);
}

.spinner-small {
	width: 14px;
	height: 14px;
	border: 2px solid var(--border-color);
	border-top-color: var(--accent);
	border-radius: 50%;
	animation: spin 0.8s linear infinite;
}

@keyframes spin {
	to { transform: rotate(360deg); }
}

@keyframes pulse {
	0%, 100% {
		opacity: 0.6;
	}
	50% {
		opacity: 0.8;
	}
}

.content-editor__layout {
	flex: 1;
	display: flex;
	overflow: hidden;
	transition: margin-right 0.2s ease;
	will-change: margin-right;
}

.content-editor__layout--swapped {
	flex-direction: row-reverse;
}

.content-editor__layout--with-ai {
	/* margin-right is set dynamically via inline style */
}

.content-editor__editor-panel {
	flex: 1;
	overflow: hidden;
	border-right: 1px solid var(--border-color);
}

.content-editor__preview-panel {
	flex: 1;
	overflow-y: auto;
	background: var(--bg-secondary);
	will-change: transform;
	transform: translateZ(0);
}

.content-editor__ai-panel {
	position: fixed;
	top: 0;
	right: 0;
	width: 420px;
	height: 100vh;
	background: var(--bg-primary);
	border-left: 1px solid var(--border-color);
	box-shadow: -2px 0 8px rgba(0, 0, 0, 0.1);
	display: flex;
	flex-direction: column;
	z-index: 1000;
}

.content-editor__ai-panel-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: var(--spacing-md);
	border-bottom: 1px solid var(--border-color);
	background: var(--bg-secondary);
}

.content-editor__ai-panel-header h3 {
	margin: 0;
	font-size: 16px;
	font-weight: 600;
	color: var(--text-primary);
}

.content-editor__ai-panel-close {
	background: transparent;
	border: none;
	font-size: 24px;
	color: var(--text-secondary);
	cursor: pointer;
	width: 32px;
	height: 32px;
	display: flex;
	align-items: center;
	justify-content: center;
	border-radius: var(--radius-sm);
	transition: all 0.2s ease;
}

.content-editor__ai-panel-close:hover {
	background: var(--bg-hover);
	color: var(--text-primary);
}

</style>
