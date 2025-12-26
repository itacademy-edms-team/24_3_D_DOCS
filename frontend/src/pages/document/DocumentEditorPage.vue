<template>
	<div class="document-editor-page">
		<div v-if="loading" class="loading-state">
			<div class="loading-text">Загрузка...</div>
		</div>

		<div v-else-if="!document" class="error-state">
			<div class="error-text">Документ не найден</div>
		</div>

		<div v-else class="editor-container">
			<!-- Header -->
			<div class="editor-header">
				<div class="header-left">
					<button class="back-btn" @click="handleBack">
						← Назад
					</button>
					<input
						type="text"
						class="document-name-input"
						:value="document.name"
						@input="handleNameChange($event)"
					/>
					<select
						class="profile-select"
						:value="document.profileId || ''"
						@change="handleProfileChange($event)"
					>
						<option value="">Без профиля</option>
						<option
							v-for="p in profiles"
							:key="p.id"
							:value="p.id"
						>
							{{ p.name }}
						</option>
					</select>
					<select
						class="profile-select title-page-select"
						:value="document.titlePageId || ''"
						@change="handleTitlePageChange($event)"
					>
						<option value="">Без титульного листа</option>
						<option
							v-for="tp in titlePages"
							:key="tp.id"
							:value="tp.id"
						>
							{{ tp.name }}
						</option>
					</select>
				</div>
				<div class="header-right">
					<button
						class="variables-btn"
						@click="isVariablesModalOpen = true"
						:disabled="!document.titlePageId"
					>
						Переменные
					</button>
					<button class="save-btn" @click="handleSave" :disabled="saving">
						{{ saving ? 'Сохранение...' : 'Сохранить' }}
					</button>
				</div>
			</div>

			<!-- Main Content -->
			<div class="editor-content">
				<ResizableSplitView :initial-left-width="55">
					<template #left>
						<div class="editor-pane" :class="{ 'is-dragging': isDragging }">
							<textarea
								ref="textareaRef"
								class="markdown-editor"
								:value="document.content"
								@input="handleContentChange($event)"
								@dragover="handleDragOver"
								@dragleave="handleDragLeave"
								@drop="handleDrop"
								@paste="handlePaste"
								placeholder="Введите Markdown...

# Заголовок
Обычный параграф текста.

Формулы: $E=mc^2$ или $$\int_0^1 x^2 dx$$

Перетащите изображение или Ctrl+V"
								spellcheck="false"
							/>
							<div v-if="uploading || isDragging" class="upload-overlay">
								<div v-if="uploading" class="upload-indicator">
									⏳ Загрузка изображения...
								</div>
								<div v-else-if="isDragging" class="drag-indicator">
									📷 Отпустите для загрузки
								</div>
							</div>
						</div>
					</template>
					<template #right>
						<div class="preview-and-style">
							<div class="preview-pane">
								<DocumentPreview
									:html="renderedHtml"
									:profile="profile"
									:document-variables="documentVariables"
									:title-page-id="document?.titlePageId"
									:selectable="true"
									@elementSelect="handleElementSelect"
								/>
							</div>
							<div
								v-if="currentStyle && selectedElementType"
								class="style-pane"
							>
								<EntityStyleEditor
									:entity-type="selectedElementType"
									:style="currentStyle"
									:profile="profile"
									:show-reset="!!document.overrides && !!selectedElementId && !!document.overrides[selectedElementId]"
									@change="handleStyleChange"
									@reset="handleResetStyle"
									@close="handleCloseStyleEditor"
								/>
							</div>
						</div>
					</template>
				</ResizableSplitView>
			</div>
		</div>

		<!-- Variables Modal -->
		<div
			v-if="isVariablesModalOpen"
			class="modal-overlay"
			@click.self="isVariablesModalOpen = false"
		>
			<div class="modal-content">
				<div class="modal-header">
					<h2 class="modal-title">Редактор переменных</h2>
					<button
						class="modal-close-btn"
						@click="isVariablesModalOpen = false"
					>
						×
					</button>
				</div>
				<VariablesEditor
					:variables="variables"
					:title-page-variable-keys="titlePageVariableKeys"
					:saving="savingVariables"
					@update="handleVariablesUpdate"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDocumentEditor } from '@/widgets/document/useDocumentEditor';
import ResizableSplitView from '@/widgets/document/ResizableSplitView.vue';
import DocumentPreview from '@/widgets/document/DocumentPreview.vue';
import { renderDocument } from '@/shared/services/markdown/documentRenderer';
import type { EntityType } from '@/entities/profile/constants';
import { getBaseStyle } from '@/shared/services/markdown/renderUtils';
import { computeStyleDelta, isDeltaEmpty } from '@/shared/services/markdown/styleDiff';
import EntityStyleEditor from '@/widgets/document/EntityStyleEditor.vue';
import VariablesEditor from '@/widgets/document/VariablesEditor.vue';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import { getTitlePageVariableKeys } from '@/shared/utils/titlePageUtils';
import type { TitlePage } from '@/entities/title-page/types';

const route = useRoute();
const router = useRouter();
const documentId = route.params.id as string;

const {
	document,
	profile,
	profiles,
	titlePages,
	loading,
	saving,
	uploading,
	textareaRef,
	handleSave: handleSaveInternal,
	handleNameChange: handleNameChangeInternal,
	handleContentChange: handleContentChangeInternal,
	handleProfileChange: handleProfileChangeInternal,
	handleTitlePageChange: handleTitlePageChangeInternal,
	handleImageUpload: handleImageUploadInternal,
} = useDocumentEditor(documentId);

const isDragging = ref(false);
const selectedElementId = ref<string | null>(null);
const selectedElementType = ref<EntityType | null>(null);
const isVariablesModalOpen = ref(false);
const variables = ref<Record<string, string>>({});
const currentTitlePage = ref<TitlePage | null>(null);
const savingVariables = ref(false);

const currentStyle = computed(() => {
	if (!document.value || !selectedElementId.value || !selectedElementType.value) {
		return null;
	}

	const base = getBaseStyle(selectedElementType.value, profile.value);
	const override = document.value.overrides?.[selectedElementId.value] || {};
	return { ...base, ...override };
});

function handleBack() {
	router.push('/dashboard');
}

function handleSave() {
	handleSaveInternal();
}

function handleNameChange(event: Event) {
	const target = event.target as HTMLInputElement;
	handleNameChangeInternal(target.value);
}

function handleContentChange(event: Event) {
	const target = event.target as HTMLTextAreaElement;
	handleContentChangeInternal(target.value);
}

function handleProfileChange(event: Event) {
	const target = event.target as HTMLSelectElement;
	handleProfileChangeInternal(target.value);
}

function handleTitlePageChange(event: Event) {
	const target = event.target as HTMLSelectElement;
	handleTitlePageChangeInternal(target.value);
}

function handleDragOver(event: DragEvent) {
	event.preventDefault();
	isDragging.value = true;
}

function handleDragLeave(event: DragEvent) {
	event.preventDefault();
	isDragging.value = false;
}

function handleDrop(event: DragEvent) {
	event.preventDefault();
	isDragging.value = false;

	const files = event.dataTransfer?.files;
	if (files && files.length > 0 && files[0].type.startsWith('image/')) {
		handleImageUploadInternal(files[0]);
	}
}

function handlePaste(event: ClipboardEvent) {
	const items = event.clipboardData?.items;
	if (!items) return;

	for (let i = 0; i < items.length; i++) {
		const item = items[i];
		if (item.type.startsWith('image/')) {
			event.preventDefault();
			const file = item.getAsFile();
			if (file) {
				handleImageUploadInternal(file);
			}
			break;
		}
	}
}

function handleElementSelect(payload: { id: string; type: string }) {
	selectedElementId.value = payload.id;
	selectedElementType.value = payload.type as EntityType;
	
	// Update selection visual in DOM
	nextTick(() => {
		// Remove previous selection
		document.querySelectorAll('.element-selectable.selected').forEach((el) => {
			el.classList.remove('selected');
		});
		
		// Add selection to current element
		const element = document.getElementById(payload.id);
		if (element && element.classList.contains('element-selectable')) {
			element.classList.add('selected');
		}
	});
}

function handleStyleChange(style: any) {
	if (!document.value || !selectedElementId.value || !selectedElementType.value) return;

	const base = getBaseStyle(selectedElementType.value, profile.value);
	const delta = computeStyleDelta(style, base);

	const currentOverrides = document.value.overrides || {};
	const newOverrides = { ...currentOverrides };

	if (isDeltaEmpty(delta)) {
		delete newOverrides[selectedElementId.value];
	} else {
		newOverrides[selectedElementId.value] = delta;
	}

	document.value = { ...document.value, overrides: newOverrides };
}

function handleResetStyle() {
	if (!document.value || !selectedElementId.value) return;

	const currentOverrides = document.value.overrides || {};
	if (!currentOverrides[selectedElementId.value]) return;

	const newOverrides = { ...currentOverrides };
	delete newOverrides[selectedElementId.value];

	document.value = { ...document.value, overrides: newOverrides };
}

function handleCloseStyleEditor() {
	selectedElementId.value = null;
	selectedElementType.value = null;
	// Remove selection from DOM
	nextTick(() => {
		document.querySelectorAll('.element-selectable.selected').forEach((el) => {
			el.classList.remove('selected');
		});
	});
}

const titlePageVariableKeys = computed(() => {
	if (!currentTitlePage.value) return [];
	return getTitlePageVariableKeys(currentTitlePage.value);
});

const documentVariables = computed(() => {
	return variables.value;
});

// Initialize variables from document when document loads
watch(
	() => document.value,
	(newDocument) => {
		if (newDocument) {
			variables.value = { ...(newDocument.variables || {}) };
		}
	},
	{ immediate: true }
);

// Load title page when titlePageId changes
watch(
	() => document.value?.titlePageId,
	async (newTitlePageId) => {
		if (newTitlePageId) {
			try {
				const loadedTitlePage = await TitlePageAPI.getById(newTitlePageId);
				currentTitlePage.value = loadedTitlePage;
			} catch (error) {
				console.error('Failed to load title page:', error);
				currentTitlePage.value = null;
			}
		} else {
			currentTitlePage.value = null;
		}
	},
	{ immediate: true }
);

async function handleVariablesUpdate(newVariables: Record<string, string>) {
	if (!document.value || !documentId) return;
	
	variables.value = newVariables;
	savingVariables.value = true;
	
	try {
		const updated = await DocumentAPI.update(documentId, {
			variables: newVariables,
		});
		if (updated) {
			document.value = updated;
			isVariablesModalOpen.value = false;
		}
	} catch (error) {
		console.error('Failed to save variables:', error);
	} finally {
		savingVariables.value = false;
	}
}

const renderedHtml = computed(() => {
	if (!document.value) return '';
	
	return renderDocument({
		markdown: document.value.content,
		profile: profile.value,
		overrides: document.value.overrides || {},
		selectable: true,
	});
});
</script>

<style scoped>
.document-editor-page {
	min-height: 100vh;
	background: #0a0a0a;
	color: #e4e4e7;
}

.loading-state,
.error-state {
	display: flex;
	align-items: center;
	justify-content: center;
	min-height: 100vh;
}

.loading-text,
.error-text {
	font-size: 16px;
	color: #71717a;
}

.editor-container {
	display: flex;
	flex-direction: column;
	height: 100vh;
	overflow: hidden;
}

.editor-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 2rem;
	border-bottom: 1px solid #27272a;
	background: #0a0a0a;
}

.header-left {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.header-right {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.back-btn {
	padding: 0.5rem 1rem;
	background: transparent;
	border: 1px solid #27272a;
	border-radius: 6px;
	color: #a1a1aa;
	font-size: 14px;
	cursor: pointer;
	transition: all 0.2s;
	font-family: inherit;
}

.back-btn:hover {
	background: #27272a;
	color: #e4e4e7;
	border-color: #3f3f46;
}

.document-name-input {
	padding: 0.5rem 0.75rem;
	background: transparent;
	border: 1px solid transparent;
	border-radius: 6px;
	color: #e4e4e7;
	font-size: 20px;
	font-weight: 600;
	outline: none;
	transition: all 0.2s;
	font-family: inherit;
	width: 300px;
}

.document-name-input:focus {
	background: #18181b;
	border-color: #6366f1;
}

.profile-select {
	padding: 0.5rem 0.75rem;
	background: #18181b;
	border: 1px solid #27272a;
	border-radius: 6px;
	color: #e4e4e7;
	font-size: 14px;
	font-family: inherit;
	outline: none;
	transition: all 0.2s;
	cursor: pointer;
}

.profile-select:hover {
	border-color: #3f3f46;
}

.profile-select:focus {
	border-color: #6366f1;
	background: #27272a;
}

.save-btn {
	padding: 0.75rem 1.5rem;
	background: #6366f1;
	color: white;
	border: none;
	border-radius: 8px;
	font-size: 14px;
	font-weight: 600;
	cursor: pointer;
	transition: background 0.2s;
	font-family: inherit;
}

.save-btn:hover:not(:disabled) {
	background: #818cf8;
}

.save-btn:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.variables-btn {
	padding: 0.75rem 1.5rem;
	background: #27272a;
	color: #e4e4e7;
	border: 1px solid #3f3f46;
	border-radius: 8px;
	font-size: 14px;
	font-weight: 600;
	cursor: pointer;
	transition: all 0.2s;
	font-family: inherit;
}

.variables-btn:hover:not(:disabled) {
	background: #3f3f46;
	border-color: #52525b;
}

.variables-btn:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.modal-overlay {
	position: fixed;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	background: rgba(0, 0, 0, 0.7);
	display: flex;
	align-items: center;
	justify-content: center;
	z-index: 1000;
	padding: 2rem;
}

.modal-content {
	background: #18181b;
	border-radius: 12px;
	border: 1px solid #27272a;
	max-width: 900px;
	width: 100%;
	max-height: 80vh;
	overflow: auto;
	display: flex;
	flex-direction: column;
}

.modal-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 1.5rem;
	border-bottom: 1px solid #27272a;
}

.modal-title {
	margin: 0;
	font-size: 1.25rem;
	font-weight: 600;
	color: #e4e4e7;
}

.modal-close-btn {
	background: transparent;
	border: 1px solid #27272a;
	border-radius: 6px;
	color: #a1a1aa;
	font-size: 1.5rem;
	width: 32px;
	height: 32px;
	display: flex;
	align-items: center;
	justify-content: center;
	cursor: pointer;
	transition: all 0.2s;
	font-family: inherit;
	line-height: 1;
}

.modal-close-btn:hover {
	background: #27272a;
	color: #e4e4e7;
	border-color: #3f3f46;
}

.editor-content {
	flex: 1;
	overflow: hidden;
	display: flex;
	flex-direction: column;
	min-height: 0;
}

.preview-and-style {
	display: flex;
	flex-direction: row;
	height: 100%;
}

.preview-pane {
	flex: 1 1 auto;
	min-width: 0;
}

.style-pane {
	flex: 0 0 280px;
	max-width: 320px;
	border-left: 1px solid #27272a;
	background: #09090b;
}

.editor-pane {
	height: 100%;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	min-height: 0;
}

.markdown-editor {
	width: 100%;
	flex: 1;
	min-height: 0;
	padding: 1.5rem;
	background: #0a0a0a;
	border: none;
	color: #e4e4e7;
	font-family: 'Courier New', Courier, monospace;
	font-size: 14px;
	line-height: 1.6;
	resize: none;
	outline: none;
	tab-size: 2;
	box-sizing: border-box;
}

.markdown-editor::placeholder {
	color: #71717a;
}

.editor-pane {
	position: relative;
}

.editor-pane.is-dragging .markdown-editor {
	border-color: #6366f1;
}

.upload-overlay {
	position: absolute;
	inset: 0;
	display: flex;
	align-items: center;
	justify-content: center;
	pointer-events: none;
	z-index: 10;
}

.upload-indicator,
.drag-indicator {
	background: #18181b;
	padding: 1rem 2rem;
	border-radius: 8px;
	border: 2px solid #6366f1;
	color: #e4e4e7;
	font-size: 14px;
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}

.drag-indicator {
	background: rgba(99, 102, 241, 0.1);
	border: 3px dashed #6366f1;
	font-size: 16px;
}

</style>
