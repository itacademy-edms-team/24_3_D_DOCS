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
						@click="handleGenerateToc"
						:disabled="isGeneratingToc || !documentId"
						title="Сгенерировать содержание"
					>
						<span v-if="!isGeneratingToc">TOC</span>
						<span v-else class="spinner-small"></span>
					</button>
					<button
						class="content-editor__header-btn"
						@click="handleResetToc"
						:disabled="isGeneratingToc || !documentId || tableOfContents.length === 0"
						title="Сбросить содержание"
					>
						<svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor">
							<path d="M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z"/>
						</svg>
					</button>
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
					<div class="versions-dropdown" ref="versionsDropdownRef">
						<button
							class="content-editor__header-btn"
							:class="{ 'content-editor__header-btn--active': showVersionsPanel }"
							@click="showVersionsPanel = !showVersionsPanel"
							:disabled="!documentId"
							title="История версий"
						>
							<svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor">
								<path d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
							</svg>
						</button>
						<Transition name="versions-panel">
							<div v-if="showVersionsPanel" class="versions-dropdown__panel">
								<button
									class="versions-dropdown__save-btn"
									@click="openSaveVersionModal"
									:disabled="isSavingVersion"
								>
									{{ isSavingVersion ? '...' : '+ Сохранить версию' }}
								</button>
								<div v-if="versionSearchScope === 'name'" class="versions-dropdown__search-row">
									<input
										v-model="versionSearchQuery"
										type="text"
										class="versions-dropdown__search"
										placeholder="Поиск по названию"
									/>
								</div>
								<div v-else class="versions-dropdown__search-row versions-dropdown__search-row--dates">
									<div class="versions-dropdown__date-range">
										<label class="versions-dropdown__date-label">С</label>
										<input v-model="versionSearchDateFrom" type="date" class="versions-dropdown__date-input" />
									</div>
									<div class="versions-dropdown__date-range">
										<label class="versions-dropdown__date-label">По</label>
										<input v-model="versionSearchDateTo" type="date" class="versions-dropdown__date-input" />
									</div>
								</div>
								<div class="versions-dropdown__search-options">
									<div class="versions-dropdown__search-scope">
										<button
											type="button"
											class="versions-dropdown__scope-btn"
											:class="{ 'versions-dropdown__scope-btn--active': versionSearchScope === 'name' }"
											@click="versionSearchScope = 'name'"
										>
											Название
										</button>
										<button
											type="button"
											class="versions-dropdown__scope-btn"
											:class="{ 'versions-dropdown__scope-btn--active': versionSearchScope === 'date' }"
											@click="versionSearchScope = 'date'"
										>
											Дата
										</button>
									</div>
									<label v-if="versionSearchScope === 'name'" class="versions-dropdown__search-case">
										<input v-model="versionSearchCaseSensitive" type="checkbox" class="versions-dropdown__search-checkbox" />
										<span>Регистр</span>
									</label>
								</div>
								<div class="versions-dropdown__list">
									<div
										v-for="v in filteredVersions"
										:key="v.id"
										class="versions-dropdown__item"
									>
										<button
											class="versions-dropdown__item-main"
											@click="handleLoadVersion(v)"
											:disabled="isLoadingVersion"
										>
											<span class="versions-dropdown__item-name">{{ v.name }}</span>
											<span class="versions-dropdown__item-date">{{ formatVersionDate(v.createdAt) }}</span>
											<span
												v-if="v.id === currentVersionId"
												class="versions-dropdown__badge versions-dropdown__badge--current"
											>
												Текущая версия
											</span>
										</button>
										<div class="versions-dropdown__item-actions">
											<button
												class="versions-dropdown__btn-tile versions-dropdown__btn-tile--restore"
												@click.stop="openRestoreConfirmModal(v)"
												:disabled="isRestoringVersion"
												title="Восстановить"
											>
												Восстановить
											</button>
											<button
												class="versions-dropdown__btn-tile versions-dropdown__btn-tile--delete"
												@click.stop="openDeleteConfirmModal(v)"
												:disabled="isDeletingVersion"
												title="Удалить"
											>
												Удалить
											</button>
										</div>
									</div>
									<div v-if="filteredVersions.length === 0" class="versions-dropdown__empty">
										{{ versions.length === 0 ? 'Нет сохранённых версий' : 'Ничего не найдено' }}
									</div>
								</div>
							</div>
						</Transition>
					</div>
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
				'content-editor__layout--swapped': swapped
			}"
		>
			<!-- Editor Panel -->
			<div class="content-editor__editor-panel" v-show="showEditor">
				<!-- Markdown Editor -->
				<MarkdownEditor
					v-if="activeTab === 'editor'"
					ref="markdownEditorRef"
					v-model="content"
					:documentId="documentId"
					@update:modelValue="handleContentChange"
					@accept-ai-change="handleAcceptAiChange"
					@undo-ai-change="handleUndoAiChange"
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
					:tableOfContents="tableOfContents"
				/>
			</div>
		</div>

		<!-- Save Version Modal -->
		<Modal
			v-model="showSaveVersionModal"
			title="Сохранить версию"
			size="sm"
		>
			<div class="save-version-modal">
				<label class="save-version-modal__label">Имя версии</label>
				<input
					ref="versionNameInputRef"
					v-model="versionNameToSave"
					type="text"
					class="save-version-modal__input"
					placeholder="Например: Первая версия"
					@keydown.enter="handleSaveVersionSubmit"
				/>
			</div>
			<template #footer>
				<div class="content-editor__modal-footer">
					<button class="content-editor__modal-btn content-editor__modal-btn--secondary" @click="showSaveVersionModal = false">
						Отмена
					</button>
					<button
						class="content-editor__modal-btn content-editor__modal-btn--primary"
						@click="handleSaveVersionSubmit"
						:disabled="!versionNameToSave.trim() || isSavingVersion"
					>
						{{ isSavingVersion ? 'Сохранение...' : 'Сохранить' }}
					</button>
				</div>
			</template>
		</Modal>

		<!-- Restore Version Confirm Modal -->
		<Modal
			v-model="showRestoreConfirmModal"
			title="Восстановить версию"
			size="sm"
		>
			<p v-if="versionToRestore" class="restore-confirm-modal__text">
				Восстановить версию «{{ versionToRestore.name }}»? Текущее содержимое будет заменено.
			</p>
			<template #footer>
				<div class="content-editor__modal-footer">
					<button class="content-editor__modal-btn content-editor__modal-btn--secondary" @click="showRestoreConfirmModal = false">
						Отмена
					</button>
					<button
						class="content-editor__modal-btn content-editor__modal-btn--primary content-editor__modal-btn--restore"
						@click="handleRestoreConfirm"
						:disabled="isRestoringVersion"
					>
						{{ isRestoringVersion ? 'Восстановление...' : 'Подтвердить' }}
					</button>
				</div>
			</template>
		</Modal>

		<!-- Delete Version Confirm Modal -->
		<Modal
			v-model="showDeleteConfirmModal"
			title="Удалить версию"
			size="sm"
		>
			<p v-if="versionToDelete" class="restore-confirm-modal__text">
				Удалить версию «{{ versionToDelete.name }}»? Это действие нельзя отменить.
			</p>
			<template #footer>
				<div class="content-editor__modal-footer">
					<button class="content-editor__modal-btn content-editor__modal-btn--secondary" @click="showDeleteConfirmModal = false">
						Отмена
					</button>
					<button
						class="content-editor__modal-btn content-editor__modal-btn--danger"
						@click="handleDeleteConfirm"
						:disabled="isDeletingVersion"
					>
						{{ isDeletingVersion ? 'Удаление...' : 'Подтвердить' }}
					</button>
				</div>
			</template>
		</Modal>

		<!-- Duplicate Version Modal -->
		<Modal
			v-model="showDuplicateVersionModal"
			title="Дубликат версии"
			size="sm"
		>
			<p class="restore-confirm-modal__text">{{ duplicateVersionMessage }}</p>
			<template #footer>
				<div class="content-editor__modal-footer">
					<button
						class="content-editor__modal-btn content-editor__modal-btn--primary"
						@click="showDuplicateVersionModal = false"
					>
						Понятно
					</button>
				</div>
			</template>
		</Modal>

		<!-- Toast notification -->
		<Transition name="toast">
			<div v-if="toastNotification.visible" class="content-editor__toast">
				{{ toastNotification.text }}
			</div>
		</Transition>

		<!-- AI Panel (Chat Dock) -->
		<ChatDock
			v-model:open="showAIPanel"
			:documentId="documentId"
			:startLine="selectedStartLine"
			:endLine="selectedEndLine"
			@clearSelection="selectedStartLine = undefined; selectedEndLine = undefined"
			@document-content-changed="handleDocumentContentChanged"
			@width-changed="handleChatDockWidthChanged"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue';
import { onClickOutside } from '@vueuse/core';
import { useRoute, useRouter } from 'vue-router';
import Button from '@/shared/ui/Button/Button.vue';
import MarkdownEditor from '@/widgets/markdown-editor/MarkdownEditor.vue';
import DocumentPreview from '@/widgets/document-preview/DocumentPreview.vue';
import TitlePageVariablesPanel from '@/widgets/title-page-variables/TitlePageVariablesPanel.vue';
import ChatDock from '@/features/agent/ChatDock.vue';
import Modal from '@/shared/ui/Modal/Modal.vue';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';
import { useDebounceFn } from '@vueuse/core';
import type { Document, DocumentMetadata, TocItem, DocumentVersion } from '@/entities/document/types';
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

const tableOfContents = ref<TocItem[]>([]);
const isGeneratingToc = ref(false);

const versions = ref<DocumentVersion[]>([]);
const showVersionsPanel = ref(false);
const versionsDropdownRef = ref<HTMLElement | null>(null);
const showSaveVersionModal = ref(false);
const versionNameToSave = ref('');
const versionNameInputRef = ref<HTMLInputElement | null>(null);
const isSavingVersion = ref(false);
const isLoadingVersion = ref(false);
const isRestoringVersion = ref(false);
const isDeletingVersion = ref(false);
const versionSearchQuery = ref('');
const versionSearchCaseSensitive = ref(false);
const versionSearchScope = ref<'name' | 'date'>('name');
const today = () => new Date().toISOString().slice(0, 10);
const versionSearchDateFrom = ref(today());
const versionSearchDateTo = ref(today());
const showRestoreConfirmModal = ref(false);
const versionToRestore = ref<DocumentVersion | null>(null);
const showDeleteConfirmModal = ref(false);
const showDuplicateVersionModal = ref(false);
const duplicateVersionMessage = ref('');
const versionToDelete = ref<DocumentVersion | null>(null);
const currentVersionId = ref<string | null>(null);
const toastNotification = ref<{ text: string; visible: boolean }>({ text: '', visible: false });
let toastTimeout: ReturnType<typeof setTimeout> | null = null;
const showToast = (text: string) => {
	if (toastTimeout) clearTimeout(toastTimeout);
	toastNotification.value = { text, visible: true };
	toastTimeout = setTimeout(() => {
		toastNotification.value = { ...toastNotification.value, visible: false };
		toastTimeout = null;
	}, 3000);
};

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

const handleDocumentContentChanged = async () => {
	try {
		const doc = await DocumentAPI.getById(documentId.value);
		if (doc && doc.content !== content.value) {
			content.value = doc.content || '';
		}
	} catch (error) {
		console.error('Failed to reload document content:', error);
	}
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

const handleAcceptAiChange = (changeId: string, changeType: 'insert' | 'delete') => {
	let updatedContent = content.value;
	
	if (changeType === 'insert') {
		const insertRegex = new RegExp(
			`<!-- AI:INSERT:${changeId} -->\\n?([\\s\\S]*?)\\n?<!-- /AI:INSERT:${changeId} -->\\n?`,
			'g'
		);
		updatedContent = updatedContent.replace(insertRegex, '$1\n');
	} else {
		const deleteRegex = new RegExp(
			`<!-- AI:DELETE:${changeId} -->[\\s\\S]*?<!-- /AI:DELETE:${changeId} -->\\n?`,
			'g'
		);
		updatedContent = updatedContent.replace(deleteRegex, '');
	}
	
	updatedContent = updatedContent.replace(/\n{3,}/g, '\n\n');
	
	content.value = updatedContent;
	handleContentChange(updatedContent);
};

const handleUndoAiChange = (changeId: string, changeType: 'insert' | 'delete') => {
	let updatedContent = content.value;
	
	if (changeType === 'insert') {
		const insertRegex = new RegExp(
			`<!-- AI:INSERT:${changeId} -->[\\s\\S]*?<!-- /AI:INSERT:${changeId} -->\\n?`,
			'g'
		);
		updatedContent = updatedContent.replace(insertRegex, '');
	} else {
		const deleteRegex = new RegExp(
			`<!-- AI:DELETE:${changeId} -->\\n?([\\s\\S]*?)\\n?<!-- /AI:DELETE:${changeId} -->\\n?`,
			'g'
		);
		updatedContent = updatedContent.replace(deleteRegex, '$1\n');
	}
	
	updatedContent = updatedContent.replace(/\n{3,}/g, '\n\n');
	
	content.value = updatedContent;
	handleContentChange(updatedContent);
};

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

const loadVersions = async () => {
	if (!documentId.value) return;
	try {
		versions.value = await DocumentAPI.getVersions(documentId.value);
	} catch (error) {
		console.error('Failed to load versions:', error);
	}
};

const loadDocument = async () => {
	try {
		const doc = await DocumentAPI.getById(documentId.value);
		document.value = doc;
		documentName.value = doc.name || 'Документ';
		content.value = doc.content || '';
		aiPendingChanges.value = [];
		selectedProfileId.value = doc.profileId || '';
		selectedTitlePageId.value = doc.titlePageId || '';
		const toc = await DocumentAPI.getTableOfContents(documentId.value);
		tableOfContents.value = toc;
		await loadVersions();
	} catch (error) {
		console.error('Failed to load document:', error);
	}
};

const filteredVersions = computed(() => {
	const scope = versionSearchScope.value;
	if (scope === 'name') {
		const q = versionSearchQuery.value.trim();
		if (!q) return versions.value;
		const caseSensitive = versionSearchCaseSensitive.value;
		const norm = (s: string) => (caseSensitive ? s : s.toLowerCase());
		const queryNorm = norm(q);
		return versions.value.filter((v) => norm(v.name).includes(queryNorm));
	}
	const from = versionSearchDateFrom.value;
	const to = versionSearchDateTo.value;
	if (!from || !to) return versions.value;
	const fromStart = new Date(from);
	fromStart.setHours(0, 0, 0, 0);
	const toEnd = new Date(to);
	toEnd.setHours(23, 59, 59, 999);
	return versions.value.filter((v) => {
		const d = new Date(v.createdAt);
		return d >= fromStart && d <= toEnd;
	});
});

const formatVersionDate = (dateStr: string) => {
	const d = new Date(dateStr);
	return d.toLocaleDateString('ru-RU', {
		day: '2-digit',
		month: '2-digit',
		year: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
	});
};

const openSaveVersionModal = () => {
	showVersionsPanel.value = false;
	versionNameToSave.value = '';
	showSaveVersionModal.value = true;
	nextTick(() => versionNameInputRef.value?.focus());
};

const handleSaveVersionSubmit = async () => {
	const name = versionNameToSave.value.trim();
	if (!name || !documentId.value || isSavingVersion.value) return;

	isSavingVersion.value = true;
	try {
		await DocumentAPI.saveVersion(documentId.value, name);
		await loadVersions();
		showSaveVersionModal.value = false;
		versionNameToSave.value = '';
		showToast(`Версия «${name}» сохранена`);
	} catch (error: any) {
		const status = error?.code ?? error?.response?.status;
		if (status === 409) {
			duplicateVersionMessage.value =
				error?.message ?? error?.response?.data?.message ?? 'Версия с таким содержимым уже существует';
			showDuplicateVersionModal.value = true;
		} else {
			console.error('Failed to save version:', error);
			alert('Не удалось сохранить версию');
		}
	} finally {
		isSavingVersion.value = false;
	}
};

const openRestoreConfirmModal = (v: DocumentVersion) => {
	showVersionsPanel.value = false;
	versionToRestore.value = v;
	showRestoreConfirmModal.value = true;
};

const handleRestoreConfirm = async () => {
	if (!versionToRestore.value || !documentId.value || isRestoringVersion.value)
		return;
	const v = versionToRestore.value;
	isRestoringVersion.value = true;
	try {
		await DocumentAPI.restoreVersion(documentId.value, v.id);
		await loadDocument();
		currentVersionId.value = v.id;
		showToast(`Текущая версия: ${v.name}`);
		showRestoreConfirmModal.value = false;
		versionToRestore.value = null;
		showVersionsPanel.value = false;
	} catch (error) {
		console.error('Failed to restore version:', error);
		alert('Не удалось восстановить версию');
	} finally {
		isRestoringVersion.value = false;
	}
};

const openDeleteConfirmModal = (v: DocumentVersion) => {
	showVersionsPanel.value = false;
	versionToDelete.value = v;
	showDeleteConfirmModal.value = true;
};

const handleDeleteConfirm = async () => {
	if (!versionToDelete.value || !documentId.value || isDeletingVersion.value) return;
	const v = versionToDelete.value;
	isDeletingVersion.value = true;
	try {
		await DocumentAPI.deleteVersion(documentId.value, v.id);
		await loadVersions();
		if (currentVersionId.value === v.id) {
			currentVersionId.value = null;
		}
		showDeleteConfirmModal.value = false;
		versionToDelete.value = null;
		showToast(`Версия «${v.name}» удалена`);
	} catch (error) {
		console.error('Failed to delete version:', error);
		alert('Не удалось удалить версию');
	} finally {
		isDeletingVersion.value = false;
	}
};

const handleLoadVersion = async (v: DocumentVersion) => {
	if (!documentId.value || isLoadingVersion.value) return;
	isLoadingVersion.value = true;
	try {
		const versionContent = await DocumentAPI.getVersionContent(documentId.value, v.id);
		content.value = versionContent;
		showVersionsPanel.value = false;
	} catch (error) {
		console.error('Failed to load version:', error);
		alert('Не удалось загрузить версию');
	} finally {
		isLoadingVersion.value = false;
	}
};

const handleGenerateToc = async () => {
	if (!documentId.value || isGeneratingToc.value) return;
	isGeneratingToc.value = true;
	try {
		tableOfContents.value = await DocumentAPI.generateTableOfContents(documentId.value);
	} catch (error) {
		console.error('Failed to generate TOC:', error);
		alert('Не удалось сгенерировать содержание');
	} finally {
		isGeneratingToc.value = false;
	}
};

const handleResetToc = async () => {
	if (!documentId.value || isGeneratingToc.value) return;
	isGeneratingToc.value = true;
	try {
		tableOfContents.value = await DocumentAPI.resetTableOfContents(documentId.value);
	} catch (error) {
		console.error('Failed to reset TOC:', error);
		alert('Не удалось сбросить содержание');
	} finally {
		isGeneratingToc.value = false;
	}
};

// Handle window resize to prevent preview "shaking"
const handleWindowResize = useDebounceFn(() => {
	// Force reflow to prevent visual glitches
	if (showPreview.value) {
		nextTick(() => {
			// Trigger a minimal reflow to stabilize layout
			const doc = typeof window !== 'undefined' ? window.document : null;
			const previewPanel = doc?.querySelector('.content-editor__preview-panel');
			if (previewPanel) {
				previewPanel.scrollTop = previewPanel.scrollTop;
			}
		});
	}
}, 100);

onClickOutside(versionsDropdownRef, () => {
	showVersionsPanel.value = false;
});

onMounted(async () => {
	await loadDocument();

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

/* Versions dropdown */
.versions-dropdown {
	position: relative;
	display: inline-flex;
}

.versions-dropdown__panel {
	position: absolute;
	top: calc(100% + 6px);
	right: 0;
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	box-shadow: var(--shadow-lg);
	z-index: 200;
	min-width: 260px;
	max-width: 320px;
	max-height: 360px;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}

.versions-dropdown__save-btn {
	width: 100%;
	padding: 10px 14px;
	background: var(--accent-light);
	color: var(--accent);
	border: none;
	border-bottom: 1px solid var(--border-color);
	font-size: 13px;
	font-weight: 600;
	cursor: pointer;
	text-align: left;
	transition: background 0.2s ease;
}

.versions-dropdown__save-btn:hover:not(:disabled) {
	background: var(--accent);
	color: white;
}

.versions-dropdown__save-btn:disabled {
	opacity: 0.6;
	cursor: not-allowed;
}

.versions-dropdown__search-row {
	padding: 0 12px 8px;
}

.versions-dropdown__search-row--dates {
	display: flex;
	gap: 12px;
	align-items: center;
}

.versions-dropdown__date-range {
	display: flex;
	align-items: center;
	gap: 6px;
	flex: 1;
	min-width: 0;
}

.versions-dropdown__date-label {
	font-size: 11px;
	color: var(--text-tertiary);
	white-space: nowrap;
}

.versions-dropdown__date-input {
	flex: 1;
	min-width: 0;
	padding: 6px 10px;
	font-size: 12px;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
}

.versions-dropdown__date-input:focus {
	outline: none;
	border-color: var(--accent);
}

.versions-dropdown__search {
	width: 100%;
	padding: 8px 12px;
	font-size: 12px;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-bottom: 1px solid var(--border-color);
	color: var(--text-primary);
}

.versions-dropdown__search::placeholder {
	color: var(--text-tertiary);
}

.versions-dropdown__search:focus {
	outline: none;
	border-color: var(--accent);
}

.versions-dropdown__search-options {
	display: flex;
	flex-direction: column;
	gap: 8px;
	padding: 6px 12px;
}

.versions-dropdown__search-scope {
	display: flex;
	gap: 4px;
}

.versions-dropdown__scope-btn {
	flex: 1;
	padding: 4px 8px;
	font-size: 10px;
	font-weight: 500;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-tertiary);
	cursor: pointer;
	transition: all 0.15s ease;
}

.versions-dropdown__scope-btn:hover {
	color: var(--text-primary);
	border-color: var(--border-hover);
}

.versions-dropdown__scope-btn--active {
	background: var(--accent-light);
	color: var(--accent);
	border-color: var(--accent);
}

.versions-dropdown__search-case {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 11px;
	color: var(--text-tertiary);
	cursor: pointer;
	user-select: none;
}

.versions-dropdown__search-checkbox {
	cursor: pointer;
}

.versions-dropdown__list {
	flex: 1;
	overflow-y: auto;
	max-height: 240px;
}

.versions-dropdown__item {
	display: flex;
	flex-direction: column;
	border-bottom: 1px solid var(--border-color);
}

.versions-dropdown__item:last-child {
	border-bottom: none;
}

.versions-dropdown__item-main {
	width: 100%;
	padding: 10px 14px;
	background: transparent;
	border: none;
	text-align: left;
	cursor: pointer;
	transition: background 0.2s ease;
}

.versions-dropdown__item-main:hover:not(:disabled) {
	background: var(--bg-secondary);
}

.versions-dropdown__item-name {
	display: block;
	font-size: 13px;
	font-weight: 500;
	color: var(--text-primary);
}

.versions-dropdown__item-date {
	display: block;
	font-size: 11px;
	color: var(--text-tertiary);
	margin-top: 2px;
}

.versions-dropdown__badge {
	display: inline-block;
	margin-top: 6px;
	padding: 2px 8px;
	font-size: 10px;
	font-weight: 600;
	border-radius: var(--radius-sm);
}

.versions-dropdown__badge--current {
	background: rgba(34, 197, 94, 0.15);
	color: #16a34a;
}

.versions-dropdown__item-actions {
	display: flex;
	gap: 8px;
	padding: 6px 14px 10px;
}

.versions-dropdown__btn-tile {
	flex: 1;
	padding: 6px 10px;
	font-size: 11px;
	font-weight: 600;
	border-radius: var(--radius-md);
	border: 1px solid transparent;
	cursor: pointer;
	transition: all 0.2s ease;
}

.versions-dropdown__btn-tile--restore {
	background: rgba(37, 99, 235, 0.12);
	color: var(--accent);
	border-color: rgba(37, 99, 235, 0.3);
}

.versions-dropdown__btn-tile--restore:hover:not(:disabled) {
	background: rgba(37, 99, 235, 0.2);
	border-color: var(--accent);
}

.versions-dropdown__btn-tile--delete {
	background: rgba(239, 68, 68, 0.1);
	color: var(--danger);
	border-color: rgba(239, 68, 68, 0.3);
}

.versions-dropdown__btn-tile--delete:hover:not(:disabled) {
	background: rgba(239, 68, 68, 0.2);
	border-color: var(--danger);
}

.versions-dropdown__btn-tile:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.versions-dropdown__empty {
	padding: 20px 14px;
	font-size: 13px;
	color: var(--text-tertiary);
	text-align: center;
}

.versions-panel-enter-active,
.versions-panel-leave-active {
	transition: opacity 0.2s ease, transform 0.2s ease;
}

.versions-panel-enter-from,
.versions-panel-leave-to {
	opacity: 0;
	transform: translateY(-8px);
}

/* Save version modal */
.save-version-modal {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-sm);
}

.save-version-modal__label {
	font-size: 13px;
	font-weight: 500;
	color: var(--text-primary);
}

.save-version-modal__input {
	padding: 10px 12px;
	font-size: 14px;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	color: var(--text-primary);
}

.save-version-modal__input:focus {
	outline: none;
	border-color: var(--accent);
}

.content-editor__modal-footer {
	display: flex;
	justify-content: center;
	align-items: center;
	gap: 12px;
	width: 100%;
}

.content-editor__modal-footer .content-editor__modal-btn {
	min-width: 120px;
}

.content-editor__modal-btn {
	padding: 8px 16px;
	font-size: 14px;
	font-weight: 500;
	border-radius: var(--radius-md);
	cursor: pointer;
	transition: all 0.2s ease;
}

.content-editor__modal-btn--secondary {
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	color: var(--text-primary);
}

.content-editor__modal-btn--secondary:hover {
	background: var(--bg-tertiary);
	border-color: var(--border-hover);
}

.content-editor__modal-btn--primary {
	background: var(--accent);
	border: 1px solid var(--accent);
	color: white;
}

.content-editor__modal-btn--primary:hover:not(:disabled) {
	background: var(--accent-hover);
	border-color: var(--accent-hover);
}

.content-editor__modal-btn--primary:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.content-editor__modal-btn--restore {
	background: var(--accent);
	border-color: var(--accent);
}

.content-editor__modal-btn--restore:hover:not(:disabled) {
	background: var(--accent-hover);
	border-color: var(--accent-hover);
}

.content-editor__modal-btn--danger {
	background: var(--danger);
	border: 1px solid var(--danger);
	color: white;
}

.content-editor__modal-btn--danger:hover:not(:disabled) {
	background: var(--danger-hover);
	border-color: var(--danger-hover);
}

.restore-confirm-modal__text {
	margin: 0;
	font-size: 14px;
	color: var(--text-primary);
	line-height: 1.5;
}

.content-editor__toast {
	position: fixed;
	top: 24px;
	left: 50%;
	transform: translateX(-50%);
	padding: 12px 20px;
	background: #16a34a;
	color: white;
	font-size: 14px;
	font-weight: 500;
	border-radius: var(--radius-md);
	box-shadow: var(--shadow-lg);
	z-index: 9999;
}

.toast-enter-active,
.toast-leave-active {
	transition: opacity 0.3s ease, transform 0.3s ease;
}

.toast-enter-from,
.toast-leave-to {
	opacity: 0;
	transform: translate(-50%, -12px);
}

</style>
