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
				/>
			</div>

			<div class="create-document-modal__field">
				<label class="create-document-modal__label">Титульная страница</label>
				<Dropdown
					v-model="form.titlePageId"
					:options="titlePageOptions"
					placeholder="Выберите титульную страницу (опционально)"
				/>
			</div>

			<div class="create-document-modal__field">
				<label class="create-document-modal__label">
					<input
						v-model="importFromFile"
						type="checkbox"
						class="create-document-modal__checkbox"
						:disabled="importFromDdoc"
					/>
					Импортировать из Markdown файла
				</label>
				<input
					v-if="importFromFile"
					type="file"
					accept=".md,.markdown"
					@change="handleFileSelect"
					class="create-document-modal__file-input"
				/>
			</div>

			<div class="create-document-modal__field">
				<label class="create-document-modal__label">
					<input
						v-model="importFromDdoc"
						type="checkbox"
						class="create-document-modal__checkbox"
						:disabled="importFromFile"
					/>
					Импортировать из .ddoc файла
				</label>
				<input
					v-if="importFromDdoc"
					type="file"
					accept=".ddoc"
					@change="handleDdocSelect"
					class="create-document-modal__file-input"
				/>
			</div>
		</div>

		<template #footer>
			<Button variant="secondary" @click="handleClose">Отмена</Button>
			<Button 
				v-if="!importFromDdoc"
				@click="handleCreate" 
				:isLoading="isCreating"
			>
				Создать
			</Button>
			<Button 
				v-else
				disabled
				:isLoading="isImportingDdoc"
			>
				{{ isImportingDdoc ? 'Импорт...' : 'Выберите .ddoc файл' }}
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

const importFromFile = ref(false);
const importFromDdoc = ref(false);
const isCreating = ref(false);
const isImportingDdoc = ref(false);
const profiles = ref<Profile[]>([]);
const titlePages = ref<any[]>([]);

const profileOptions = computed(() =>
	profiles.value.map((p) => ({ value: p.id, label: p.name })),
);

const titlePageOptions = computed(() => [
	{ value: undefined, label: 'Без титульной страницы' },
	...titlePages.value.map((tp) => ({ value: tp.id, label: tp.name })),
]);

const handleClose = () => {
	isOpen.value = false;
	form.value = {
		name: '',
		profileId: undefined,
		titlePageId: undefined,
		initialContent: '',
	};
	importFromFile.value = false;
	importFromDdoc.value = false;
};

const handleFileSelect = async (e: Event) => {
	const target = e.target as HTMLInputElement;
	const file = target.files?.[0];
	if (file) {
		const text = await file.text();
		form.value.initialContent = text;
		if (!form.value.name) {
			form.value.name = file.name.replace(/\.(md|markdown)$/i, '');
		}
	}
};

const handleDdocSelect = async (e: Event) => {
	const target = e.target as HTMLInputElement;
	const file = target.files?.[0];
	if (!file) return;

	// Валидация расширения
	if (!file.name.toLowerCase().endsWith('.ddoc')) {
		alert('Файл должен иметь расширение .ddoc');
		target.value = '';
		return;
	}

	isImportingDdoc.value = true;
	try {
		const documentName = form.value.name.trim() || undefined;
		const document = await DocumentAPI.importDocument(file, documentName);
		emit('created', document.id);
		handleClose();
	} catch (error: any) {
		console.error('Failed to import .ddoc:', error);
		const errorMessage = error?.message || error?.response?.data?.message || 'Ошибка при импорте документа';
		alert(`Ошибка при импорте документа: ${errorMessage}`);
	} finally {
		isImportingDdoc.value = false;
		target.value = '';
	}
};

const handleCreate = async () => {
	if (!form.value.name.trim()) {
		alert('Введите название документа');
		return;
	}

	isCreating.value = true;
	try {
		// Преобразуем пустую строку в undefined для titlePageId
		const titlePageId = form.value.titlePageId === '' || !form.value.titlePageId 
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
		const errorMessage = error?.message || error?.response?.data?.message || 'Ошибка при создании документа';
		alert(`Ошибка при создании документа: ${errorMessage}`);
	} finally {
		isCreating.value = false;
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

.create-document-modal__file-input {
	margin-top: var(--spacing-xs);
}
</style>
