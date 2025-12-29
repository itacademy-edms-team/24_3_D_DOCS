<template>
	<div class="page-settings-card">
		<h3 class="page-settings-card__title">📄 Настройки страницы</h3>
		<div class="page-settings-card__content">
			<div class="page-settings-card__row">
				<label class="page-settings-card__label">Размер бумаги</label>
				<Dropdown
					v-model="localSettings.size"
					:options="sizeOptions"
				/>
			</div>

			<div class="page-settings-card__row">
				<label class="page-settings-card__label">Ориентация</label>
				<div class="page-settings-card__radio-group">
					<label class="page-settings-card__radio">
						<input
							type="radio"
							value="portrait"
							v-model="localSettings.orientation"
						/>
						Книжная
					</label>
					<label class="page-settings-card__radio">
						<input
							type="radio"
							value="landscape"
							v-model="localSettings.orientation"
						/>
						Альбомная
					</label>
				</div>
			</div>

			<div class="page-settings-card__row">
				<label class="page-settings-card__label">Поля (мм)</label>
				<div class="page-settings-card__margins">
					<Slider
						v-model="localSettings.margins.top"
						label="Верх"
						:min="0"
						:max="50"
						unit="мм"
					/>
					<Slider
						v-model="localSettings.margins.right"
						label="Правый"
						:min="0"
						:max="50"
						unit="мм"
					/>
					<Slider
						v-model="localSettings.margins.bottom"
						label="Низ"
						:min="0"
						:max="50"
						unit="мм"
					/>
					<Slider
						v-model="localSettings.margins.left"
						label="Левый"
						:min="0"
						:max="50"
						unit="мм"
					/>
				</div>
			</div>

			<div class="page-settings-card__row">
				<label class="page-settings-card__label">Нумерация страниц</label>
				<div class="page-settings-card__page-numbers">
					<label class="page-settings-card__checkbox">
						<input
							type="checkbox"
							v-model="localSettings.pageNumbers.enabled"
						/>
						Включить
					</label>
					<Dropdown
						v-if="localSettings.pageNumbers.enabled"
						v-model="localSettings.pageNumbers.position"
						:options="positionOptions"
					/>
					<Dropdown
						v-if="localSettings.pageNumbers.enabled"
						v-model="localSettings.pageNumbers.align"
						:options="alignOptions"
					/>
					<input
						v-if="localSettings.pageNumbers.enabled"
						v-model="localSettings.pageNumbers.format"
						type="text"
						class="page-settings-card__format-input"
						placeholder="{n}"
					/>
				</div>
			</div>

			<div class="page-settings-card__row">
				<label class="page-settings-card__label">Глобальные настройки</label>
				<div class="page-settings-card__global-settings">
					<div class="page-settings-card__field">
						<label class="page-settings-card__field-label">Межстрочный интервал</label>
						<Slider
							v-model="localSettings.globalLineHeight"
							:min="1"
							:max="3"
							:step="0.1"
						/>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import Dropdown from '@/shared/ui/Dropdown/Dropdown.vue';
import Slider from '@/shared/ui/Slider/Slider.vue';
import type { PageSettings } from '@/entities/profile/types';

interface Props {
	modelValue: PageSettings;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: PageSettings];
}>();

const localSettings = ref({ ...props.modelValue });
const isUpdating = ref(false);

watch(
	() => props.modelValue,
	(newValue) => {
		// Избегаем циклических обновлений
		if (isUpdating.value) return;
		
		// Проверяем, действительно ли изменились данные
		const currentStr = JSON.stringify(localSettings.value);
		const newStr = JSON.stringify(newValue);
		if (currentStr === newStr) return;
		
		isUpdating.value = true;
		localSettings.value = { ...newValue };
		// Используем nextTick чтобы избежать конфликтов
		nextTick(() => {
			isUpdating.value = false;
		});
	},
	{ deep: true },
);

watch(
	localSettings,
	(newValue) => {
		// Избегаем циклических обновлений
		if (isUpdating.value) return;
		
		isUpdating.value = true;
		emit('update:modelValue', { ...newValue });
		// Используем nextTick чтобы избежать конфликтов
		nextTick(() => {
			isUpdating.value = false;
		});
	},
	{ deep: true },
);

const sizeOptions = [
	{ value: 'A4', label: 'A4' },
	{ value: 'A5', label: 'A5' },
	{ value: 'Letter', label: 'Letter' },
];

const positionOptions = [
	{ value: 'top', label: 'Сверху' },
	{ value: 'bottom', label: 'Снизу' },
];

const alignOptions = [
	{ value: 'left', label: 'Слева' },
	{ value: 'center', label: 'Центр' },
	{ value: 'right', label: 'Справа' },
];
</script>

<style scoped>
.page-settings-card {
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-lg);
	padding: var(--spacing-lg);
}

.page-settings-card__title {
	font-size: 18px;
	font-weight: 600;
	color: var(--text-primary);
	margin: 0 0 var(--spacing-lg) 0;
}

.page-settings-card__content {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-lg);
}

.page-settings-card__row {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-sm);
}

.page-settings-card__label {
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
}

.page-settings-card__radio-group {
	display: flex;
	gap: var(--spacing-md);
}

.page-settings-card__radio {
	display: flex;
	align-items: center;
	gap: var(--spacing-xs);
	font-size: 14px;
	color: var(--text-primary);
	cursor: pointer;
}

.page-settings-card__margins {
	display: grid;
	grid-template-columns: repeat(2, 1fr);
	gap: var(--spacing-md);
}

.page-settings-card__page-numbers {
	display: flex;
	flex-wrap: wrap;
	gap: var(--spacing-md);
	align-items: center;
}

.page-settings-card__checkbox {
	display: flex;
	align-items: center;
	gap: var(--spacing-xs);
	font-size: 14px;
	color: var(--text-primary);
	cursor: pointer;
}

.page-settings-card__format-input {
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 14px;
	font-family: monospace;
	width: 120px;
}

.page-settings-card__global-settings {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
}

.page-settings-card__field {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-xs);
}

.page-settings-card__field-label {
	font-size: 13px;
	font-weight: 500;
	color: var(--text-secondary);
}
</style>
