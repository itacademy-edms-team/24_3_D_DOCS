<template>
	<div class="title-page-variables-panel">
		<div v-if="isLoading" class="title-page-variables-panel__loading">
			Загрузка...
		</div>
		<div v-else-if="!titlePageId" class="title-page-variables-panel__empty">
			Выберите титульник для редактирования переменных
		</div>
		<div v-else-if="variableKeys.length === 0" class="title-page-variables-panel__empty">
			В выбранном титульнике нет переменных
		</div>
		<div v-else class="title-page-variables-panel__content">
			<h3 class="title-page-variables-panel__title">Переменные титульника</h3>
			<div class="title-page-variables-panel__form">
				<div
					v-for="key in variableKeys"
					:key="key"
					class="title-page-variables-panel__field"
				>
					<label class="title-page-variables-panel__label">
						{{ getVariableLabel(key) }}
					</label>
					<input
						:value="localVariables[key] || ''"
						@input="handleVariableChange(key, ($event.target as HTMLInputElement).value)"
						class="title-page-variables-panel__input"
						type="text"
						:placeholder="`Введите ${getVariableLabel(key).toLowerCase()}`"
					/>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';
import { extractTitlePageVariables } from '@/utils/titlePageRenderer';
import { useDebounceFn } from '@vueuse/core';
import type { TitlePageWithData } from '@/entities/title-page/api/TitlePageAPI';

interface Props {
	titlePageId?: string;
	documentId: string;
	variables?: Record<string, string>;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:variables': [variables: Record<string, string>];
}>();

const isLoading = ref(false);
const titlePage = ref<TitlePageWithData | null>(null);
const localVariables = ref<Record<string, string>>({});

const variableKeys = computed(() => {
	if (!titlePage.value) return [];
	return extractTitlePageVariables(titlePage.value);
});

const getVariableLabel = (key: string): string => {
	const labels: Record<string, string> = {
		Title: 'Название',
		Author: 'Автор',
		Year: 'Год',
		Group: 'Группа',
		City: 'Город',
		Supervisor: 'Руководитель',
		DocumentType: 'Тип документа',
	};
	return labels[key] || key;
};

const handleVariableChange = useDebounceFn((key: string, value: string) => {
	localVariables.value[key] = value;
	emit('update:variables', { ...localVariables.value });
}, 500);

const loadTitlePage = async () => {
	if (!props.titlePageId) {
		titlePage.value = null;
		localVariables.value = {};
		return;
	}

	isLoading.value = true;
	try {
		const loadedTitlePage = await TitlePageAPI.getById(props.titlePageId);
		titlePage.value = loadedTitlePage;
		
		// Инициализируем переменные из props или пустыми значениями
		const keys = extractTitlePageVariables(loadedTitlePage);
		const newVariables: Record<string, string> = {};
		for (const key of keys) {
			newVariables[key] = props.variables?.[key] || '';
		}
		localVariables.value = newVariables;
	} catch (error) {
		console.error('Failed to load title page:', error);
		titlePage.value = null;
	} finally {
		isLoading.value = false;
	}
};

// Синхронизируем localVariables с props.variables
watch(
	() => props.variables,
	(newVariables) => {
		if (newVariables) {
			// Обновляем только существующие ключи, чтобы не потерять пользовательский ввод
			for (const key of variableKeys.value) {
				if (newVariables[key] !== undefined && localVariables.value[key] !== newVariables[key]) {
					localVariables.value[key] = newVariables[key];
				}
			}
		}
	},
	{ deep: true }
);

watch(
	() => props.titlePageId,
	() => {
		loadTitlePage();
	},
	{ immediate: true }
);

onMounted(() => {
	loadTitlePage();
});
</script>

<style scoped>
.title-page-variables-panel {
	height: 100%;
	display: flex;
	flex-direction: column;
	padding: var(--spacing-lg);
	background: var(--bg-primary);
	overflow-y: auto;
}

.title-page-variables-panel__loading,
.title-page-variables-panel__empty {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 100%;
	color: var(--text-secondary);
	font-size: 14px;
}

.title-page-variables-panel__content {
	flex: 1;
}

.title-page-variables-panel__title {
	font-size: 18px;
	font-weight: 600;
	color: var(--text-primary);
	margin: 0 0 var(--spacing-lg) 0;
}

.title-page-variables-panel__form {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
}

.title-page-variables-panel__field {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-xs);
}

.title-page-variables-panel__label {
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
}

.title-page-variables-panel__input {
	padding: var(--spacing-sm) var(--spacing-md);
	font-size: 14px;
	background: var(--bg-tertiary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	transition: all 0.2s ease;
}

.title-page-variables-panel__input:hover {
	background: var(--bg-hover);
	border-color: var(--border-hover);
}

.title-page-variables-panel__input:focus {
	outline: none;
	background: var(--bg-primary);
	border-color: var(--accent-primary);
	box-shadow: 0 0 0 2px rgba(var(--accent-primary-rgb), 0.1);
}
</style>
