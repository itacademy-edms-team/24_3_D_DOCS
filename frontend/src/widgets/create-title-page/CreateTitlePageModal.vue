<template>
	<Modal
		v-model="isOpen"
		title="Создать титульный лист"
		size="md"
		@update:modelValue="handleClose"
	>
		<div class="create-title-page-modal">
			<div class="create-title-page-modal__field">
				<label class="create-title-page-modal__label">Название титульника</label>
				<input
					v-model="name"
					type="text"
					class="create-title-page-modal__input"
					placeholder="Например: Титульный лист для отчёта"
				/>
			</div>

			<div class="create-title-page-modal__field">
				<label class="create-title-page-modal__label">Импорт из PDF (первая страница)</label>
				<input
					ref="fileInputRef"
					type="file"
					class="create-title-page-modal__file-native"
					accept="application/pdf,.pdf"
					@change="onFileInputChange"
				/>
				<div
					class="create-title-page-modal__dropzone"
					:class="{ 'create-title-page-modal__dropzone--active': isDragging }"
					role="button"
					tabindex="0"
					@click="triggerFilePick"
					@keydown.enter.prevent="triggerFilePick"
					@keydown.space.prevent="triggerFilePick"
					@dragover.prevent="isDragging = true"
					@dragleave.prevent="isDragging = false"
					@drop.prevent="onDrop"
				>
					<template v-if="pdfFile">
						<span class="create-title-page-modal__file-name">{{ pdfFile.name }}</span>
						<button
							type="button"
							class="create-title-page-modal__clear-file"
							@click.stop="clearFile"
						>
							Сбросить
						</button>
					</template>
					<template v-else>
						<span class="create-title-page-modal__drop-hint">
							Перетащите PDF сюда или нажмите, чтобы выбрать файл
						</span>
					</template>
				</div>
			</div>
		</div>

		<template #footer>
			<Button variant="secondary" @click="handleClose">Отмена</Button>
			<Button variant="secondary" @click="handleCreateEmpty" :isLoading="isCreatingEmpty">
				Создать пустой
			</Button>
			<Button
				@click="handleImportPdf"
				:disabled="!pdfFile"
				:isLoading="isImportingPdf"
			>
				Импортировать из PDF
			</Button>
		</template>
	</Modal>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Modal from '@/shared/ui/Modal/Modal.vue';
import Button from '@/shared/ui/Button/Button.vue';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';

interface Props {
	modelValue: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	created: [titlePageId: string];
}>();

const isOpen = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

const name = ref('');
const pdfFile = ref<File | null>(null);
const fileInputRef = ref<HTMLInputElement | null>(null);
const isDragging = ref(false);
const isCreatingEmpty = ref(false);
const isImportingPdf = ref(false);

const resetForm = () => {
	name.value = '';
	pdfFile.value = null;
	isDragging.value = false;
	if (fileInputRef.value) {
		fileInputRef.value.value = '';
	}
};

const handleClose = () => {
	isOpen.value = false;
	resetForm();
};

const triggerFilePick = () => {
	fileInputRef.value?.click();
};

const setPdfFile = (file: File | null) => {
	if (!file) {
		pdfFile.value = null;
		return;
	}
	const lower = file.name.toLowerCase();
	if (!lower.endsWith('.pdf') && file.type && !file.type.includes('pdf')) {
		alert('Выберите файл в формате PDF');
		return;
	}
	pdfFile.value = file;
	if (!name.value.trim() && file.name) {
		name.value = file.name.replace(/\.pdf$/i, '');
	}
};

const onFileInputChange = (e: Event) => {
	const input = e.target as HTMLInputElement;
	const file = input.files?.[0] ?? null;
	setPdfFile(file);
};

const onDrop = (e: DragEvent) => {
	isDragging.value = false;
	const file = e.dataTransfer?.files?.[0] ?? null;
	setPdfFile(file);
};

const clearFile = () => {
	pdfFile.value = null;
	if (fileInputRef.value) {
		fileInputRef.value.value = '';
	}
};

const validateName = (): boolean => {
	if (!name.value.trim()) {
		alert('Введите название титульника');
		return false;
	}
	return true;
};

const handleCreateEmpty = async () => {
	if (!validateName()) return;

	isCreatingEmpty.value = true;
	try {
		const titlePage = await TitlePageAPI.create({ name: name.value.trim() });
		emit('created', titlePage.id);
		handleClose();
	} catch (error: any) {
		console.error('Failed to create title page:', error);
		const errorMessage =
			error?.message || error?.response?.data?.message || 'Ошибка при создании';
		alert(`Ошибка: ${errorMessage}`);
	} finally {
		isCreatingEmpty.value = false;
	}
};

const handleImportPdf = async () => {
	if (!validateName()) return;
	if (!pdfFile.value) {
		alert('Выберите PDF-файл');
		return;
	}

	isImportingPdf.value = true;
	try {
		const titlePage = await TitlePageAPI.importFromPdf(name.value.trim(), pdfFile.value);
		emit('created', titlePage.id);
		handleClose();
	} catch (error: any) {
		console.error('Failed to import title page from PDF:', error);
		const errorMessage =
			error?.message || error?.response?.data?.message || 'Ошибка при импорте PDF';
		alert(`Ошибка: ${errorMessage}`);
	} finally {
		isImportingPdf.value = false;
	}
};

watch(isOpen, (open) => {
	if (open) {
		resetForm();
	}
});
</script>

<style scoped>
.create-title-page-modal {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-lg);
}

.create-title-page-modal__field {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-sm);
}

.create-title-page-modal__label {
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
}

.create-title-page-modal__input {
	padding: var(--spacing-sm) var(--spacing-md);
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	color: var(--text-primary);
	font-size: 14px;
	outline: none;
	transition: border-color 0.2s ease;
}

.create-title-page-modal__input:focus {
	border-color: var(--accent);
}

.create-title-page-modal__file-native {
	position: absolute;
	width: 0;
	height: 0;
	opacity: 0;
	pointer-events: none;
}

.create-title-page-modal__dropzone {
	min-height: 120px;
	padding: var(--spacing-md);
	border: 2px dashed var(--border-color);
	border-radius: var(--radius-md);
	background: var(--bg-secondary);
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: var(--spacing-sm);
	cursor: pointer;
	transition:
		border-color 0.2s ease,
		background 0.2s ease;
}

.create-title-page-modal__dropzone:hover,
.create-title-page-modal__dropzone--active {
	border-color: var(--accent);
	background: var(--bg-tertiary, var(--bg-secondary));
}

.create-title-page-modal__drop-hint {
	font-size: 14px;
	color: var(--text-secondary);
	text-align: center;
}

.create-title-page-modal__file-name {
	font-size: 14px;
	color: var(--text-primary);
	text-align: center;
	word-break: break-all;
}

.create-title-page-modal__clear-file {
	font-size: 13px;
	color: var(--accent);
	background: none;
	border: none;
	cursor: pointer;
	padding: var(--spacing-xs);
}

.create-title-page-modal__clear-file:hover {
	text-decoration: underline;
}
</style>
