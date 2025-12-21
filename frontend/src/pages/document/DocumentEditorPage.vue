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
				</div>
				<button class="save-btn" @click="handleSave" :disabled="saving">
					{{ saving ? 'Сохранение...' : 'Сохранить' }}
				</button>
			</div>

			<!-- Main Content -->
			<div class="editor-content">
				<ResizableSplitView :initial-left-width="50">
					<template #left>
						<div class="editor-pane">
							<textarea
								ref="textareaRef"
								class="markdown-editor"
								:value="document.content"
								@input="handleContentChange($event)"
								placeholder="Введите Markdown...

# Заголовок
Обычный параграф текста.

Формулы: $E=mc^2$ или $$\int_0^1 x^2 dx$$

Перетащите изображение или Ctrl+V"
								spellcheck="false"
							/>
						</div>
					</template>
					<template #right>
						<DocumentPreview
							:html="renderedHtml"
							:profile="profile"
							:document-variables="documentVariables"
						/>
					</template>
				</ResizableSplitView>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDocumentEditor } from '@/widgets/document/useDocumentEditor';
import ResizableSplitView from '@/widgets/document/ResizableSplitView.vue';
import DocumentPreview from '@/widgets/document/DocumentPreview.vue';
import { renderDocument } from '@/shared/services/markdown/documentRenderer';

const route = useRoute();
const router = useRouter();
const documentId = route.params.id as string;

const {
	document,
	profile,
	profiles,
	loading,
	saving,
	handleSave: handleSaveInternal,
	handleNameChange: handleNameChangeInternal,
	handleContentChange: handleContentChangeInternal,
	handleProfileChange: handleProfileChangeInternal,
} = useDocumentEditor(documentId);

const textareaRef = ref<HTMLTextAreaElement | null>(null);

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

const documentVariables = computed(() => {
	// Extract variables from frontmatter if needed
	// For now, return empty object
	return {};
});

const renderedHtml = computed(() => {
	if (!document.value) return '';
	
	return renderDocument({
		markdown: document.value.content,
		profile: profile.value,
		overrides: {},
		selectable: false,
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

.editor-content {
	flex: 1;
	overflow: hidden;
	display: flex;
	flex-direction: column;
	min-height: 0;
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

</style>
