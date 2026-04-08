<template>
	<Modal
		v-model="isOpen"
		title="Создать новый документ"
		size="md"
		@update:modelValue="handleClose"
	>
		<div class="create-document-modal">
			<div class="create-document-modal__field">
				<label class="create-document-modal__label">Название документа</label>
				<input
					v-model="form.name"
					type="text"
					class="create-document-modal__input"
					placeholder="Например: Курсовая работа"
				/>
			</div>

			<div class="create-document-modal__field">
				<label class="create-document-modal__label">Профиль стилей</label>
				<Dropdown
					v-model="form.profileId"
					:options="profileOptions"
					placeholder="Выберите профиль стилей"
					:disabled="importFromPdf"
				/>
			</div>

			<div class="create-document-modal__field">
				<label class="create-document-modal__label">Титульная страница</label>
				<Dropdown
					v-model="form.titlePageId"
					:options="titlePageOptions"
					placeholder="Выберите титульную страницу (опционально)"
					:disabled="importFromPdf"
				/>
			</div>

			<div class="create-document-modal__field">
				<label class="create-document-modal__label">
					<input
						v-model="importFromPdf"
						type="checkbox"
						class="create-document-modal__checkbox"
					/>
					Импортировать из PDF файла
				</label>
			</div>

			<div
				v-if="importFromPdf"
				class="create-document-modal__drop"
				:class="{ 'create-document-modal__drop--active': isDragging }"
				@dragenter.prevent="isDragging = true"
				@dragleave.prevent="onDragLeave"
				@dragover.prevent
				@drop.prevent="onDrop"
			>
				<input
					ref="pdfInputRef"
					type="file"
					accept="application/pdf,.pdf"
					class="create-document-modal__file-input-hidden"
					@change="onPdfFileChange"
				/>
				<p v-if="!pdfFile" class="create-document-modal__drop-hint" @click="pdfInputRef?.click()">
					Перетащите PDF сюда или нажмите, чтобы выбрать
				</p>
				<template v-else>
					<div class="create-document-modal__pdf-name">
						{{ pdfFile.name }} ({{ (pdfFile.size / 1024).toFixed(1) }} КБ)
						<button type="button" class="create-document-modal__clear" @click="clearPdf">×</button>
					</div>
					<p v-if="isPreviewLoading" class="create-document-modal__status">Анализ вложений…</p>
					<div v-else-if="pdfPreviewList.length" class="create-document-modal__embed-list">
						<div class="create-document-modal__embed-title">Вложения в PDF</div>
						<label
							v-for="f in pdfPreviewList"
							:key="f.name"
							class="create-document-modal__embed-row"
						>
							<input v-model="selectedNames[f.name]" type="checkbox" />
							<span class="name">{{ f.name }}</span>
							<span class="meta">{{ f.kind === 'ddoc' ? 'пакет' : 'файл' }} ·
								{{ (f.size / 1024).toFixed(1) }} КБ</span>
						</label>
					</div>
					<p v-else class="create-document-modal__status">Вложения не найдены</p>
				</template>
			</div>
		</div>

		<template #footer>
			<Button variant="secondary" @click="handleClose">Отмена</Button>
			<Button
				v-if="!importFromPdf"
				@click="handleCreate"
				:isLoading="isCreating"
			>
				Создать
			</Button>
			<Button
				v-else
				:disabled="!canImportFromPdf"
				:isLoading="isImportingPdf"
				@click="handleImportPdf"
			>
				{{ isImportingPdf ? 'Импорт...' : 'Импорт' }}
			</Button>
		</template>
	</Modal>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Modal from '@/shared/ui/Modal/Modal.vue';
import Dropdown from '@/shared/ui/Dropdown/Dropdown.vue';
import Button from '@/shared/ui/Button/Button.vue';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';
import type { Profile } from '@/entities/profile/types';

interface Props {
	modelValue: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	created: [documentId: string];
}>();

const isOpen = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

const form = ref({
	name: '',
	profileId: undefined as string | undefined,
	titlePageId: undefined as string | undefined,
	initialContent: '',
});

const importFromPdf = ref(false);
const pdfFile = ref<File | null>(null);
const pdfInputRef = ref<HTMLInputElement | null>(null);
const isDragging = ref(false);
const isPreviewLoading = ref(false);
const pdfPreviewList = ref<{ name: string; size: number; kind: string }[]>([]);
const selectedNames = ref<Record<string, boolean>>({});
const isCreating = ref(false);
const isImportingPdf = ref(false);
const profiles = ref<Profile[]>([]);
const titlePages = ref<any[]>([]);

const profileOptions = computed(() =>
	profiles.value.map((p) => ({ value: p.id, label: p.name })),
);

const titlePageOptions = computed(() => [
	{ value: undefined, label: 'Без титульной страницы' },
	...titlePages.value.map((tp) => ({ value: tp.id, label: tp.name })),
]);

const canImportFromPdf = computed(() => {
	if (!pdfFile.value) return false;
	if (isPreviewLoading.value) return false;
	const ddocPicks = pdfPreviewList.value.filter(
		(f) => f.kind === 'ddoc' && selectedNames.value[f.name],
	);
	if (ddocPicks.length > 0) return true;
	return false;
});

const handleClose = () => {
	isOpen.value = false;
	form.value = {
		name: '',
		profileId: undefined,
		titlePageId: undefined,
		initialContent: '',
	};
	importFromPdf.value = false;
	clearPdfState();
};

const clearPdfState = () => {
	pdfFile.value = null;
	pdfPreviewList.value = [];
	selectedNames.value = {};
	isPreviewLoading.value = false;
	if (pdfInputRef.value) pdfInputRef.value.value = '';
};

const onDragLeave = (e: DragEvent) => {
	if ((e.currentTarget as HTMLElement).contains(e.relatedTarget as Node)) return;
	isDragging.value = false;
};

async function setPdfFile(file: File) {
	if (!file.name.toLowerCase().endsWith('.pdf') && !file.type.includes('pdf')) {
		alert('Нужен файл PDF');
		return;
	}
	pdfFile.value = file;
	if (!form.value.name.trim()) {
		form.value.name = file.name.replace(/\.pdf$/i, '');
	}
	isPreviewLoading.value = true;
	pdfPreviewList.value = [];
	try {
		const { files } = await DocumentAPI.previewPdfImport(file);
		pdfPreviewList.value = files;
		const sel: Record<string, boolean> = {};
		for (const f of files) {
			if (f.kind === 'ddoc' || f.name.toLowerCase().endsWith('.ddoc')) sel[f.name] = true;
		}
		selectedNames.value = sel;
	} catch (e) {
		console.error(e);
		alert('Не удалось прочитать вложения PDF');
	} finally {
		isPreviewLoading.value = false;
	}
}

function onDrop(e: DragEvent) {
	isDragging.value = false;
	const f = e.dataTransfer?.files?.[0];
	if (f) void setPdfFile(f);
}

function onPdfFileChange(e: Event) {
	const t = e.target as HTMLInputElement;
	const f = t.files?.[0];
	if (f) void setPdfFile(f);
}

function clearPdf() {
	clearPdfState();
}

watch(importFromPdf, (v) => {
	if (!v) clearPdfState();
});

const handleCreate = async () => {
	if (!form.value.name.trim()) {
		alert('Введите название документа');
		return;
	}

	isCreating.value = true;
	try {
		const titlePageId =
			form.value.titlePageId === '' || !form.value.titlePageId
				? undefined
				: form.value.titlePageId;

		const document = await DocumentAPI.create({
			name: form.value.name,
			profileId: form.value.profileId,
			titlePageId: titlePageId,
			initialContent: form.value.initialContent,
		});
		emit('created', document.id);
		handleClose();
	} catch (error: any) {
		console.error('Failed to create document:', error);
		const errorMessage =
			error?.message || error?.response?.data?.message || 'Ошибка при создании документа';
		alert(`Ошибка при создании документа: ${errorMessage}`);
	} finally {
		isCreating.value = false;
	}
};

const handleImportPdf = async () => {
	if (!pdfFile.value || !canImportFromPdf.value) return;
	const ddocPicks = pdfPreviewList.value.filter(
		(f) => (f.kind === 'ddoc' || f.name.toLowerCase().endsWith('.ddoc')) && selectedNames.value[f.name],
	);
	if (ddocPicks.length === 0) {
		alert('Выберите вложение с пакетом .ddoc');
		return;
	}
	const name = ddocPicks[0]!.name;
	isImportingPdf.value = true;
	try {
		const documentName = form.value.name.trim() || undefined;
		const doc = await DocumentAPI.importFromPdf(pdfFile.value, documentName, name);
		emit('created', doc.id);
		handleClose();
	} catch (error: any) {
		console.error('Failed to import from PDF:', error);
		const errorMessage =
			error?.message || error?.response?.data?.message || 'Ошибка при импорте';
		alert(`Ошибка: ${errorMessage}`);
	} finally {
		isImportingPdf.value = false;
	}
};

watch(isOpen, async (open) => {
	if (open) {
		try {
			const [profilesData, titlePagesData] = await Promise.all([
				ProfileAPI.getAll().catch(() => []),
				TitlePageAPI.getAll().catch(() => []),
			]);
			profiles.value = profilesData;
			titlePages.value = titlePagesData;
		} catch (error) {
			console.error('Failed to load data:', error);
		}
	}
});
</script>

<style scoped>
.create-document-modal {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-lg);
}

.create-document-modal__field {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-sm);
}

.create-document-modal__label {
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
}

.create-document-modal__input {
	padding: var(--spacing-sm) var(--spacing-md);
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	color: var(--text-primary);
	font-size: 14px;
	outline: none;
	transition: border-color 0.2s ease;
}

.create-document-modal__input:focus {
	border-color: var(--accent);
}

.create-document-modal__checkbox {
	margin-right: var(--spacing-xs);
}

.create-document-modal__drop {
	border: 2px dashed var(--border-color);
	border-radius: var(--radius-md);
	padding: var(--spacing-md);
	min-height: 100px;
	transition: border-color 0.2s ease, background 0.2s ease;
}

.create-document-modal__drop--active {
	border-color: var(--accent);
	background: var(--bg-secondary);
}

.create-document-modal__file-input-hidden {
	display: none;
}

.create-document-modal__drop-hint {
	margin: 0;
	text-align: center;
	cursor: pointer;
	color: var(--text-secondary);
}

.create-document-modal__pdf-name {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	font-weight: 500;
}

.create-document-modal__clear {
	border: none;
	background: none;
	color: var(--text-secondary);
	cursor: pointer;
	font-size: 20px;
	line-height: 1;
	padding: 0 4px;
}

.create-document-modal__status {
	margin: 8px 0 0;
	font-size: 13px;
	color: var(--text-secondary);
}

.create-document-modal__embed-list {
	margin-top: var(--spacing-sm);
}

.create-document-modal__embed-title {
	font-size: 12px;
	text-transform: uppercase;
	letter-spacing: 0.05em;
	color: var(--text-secondary);
	margin-bottom: 8px;
}

.create-document-modal__embed-row {
	display: flex;
	align-items: flex-start;
	gap: 8px;
	padding: 6px 0;
	border-bottom: 1px solid var(--border-color);
	font-size: 14px;
	cursor: pointer;
}

.create-document-modal__embed-row .name {
	font-weight: 500;
}
.create-document-modal__embed-row .meta {
	display: block;
	margin-left: 24px;
	font-size: 12px;
	color: var(--text-secondary);
}
</style>
