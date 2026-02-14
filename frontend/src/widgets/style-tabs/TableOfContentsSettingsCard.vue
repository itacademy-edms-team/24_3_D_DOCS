<template>
	<div class="toc-settings-card">
		<h3 class="toc-settings-card__title">Содержание</h3>
		<div class="toc-settings-card__content">
			<div class="toc-settings-card__row">
				<label class="toc-settings-card__label">Стиль шрифта</label>
				<Dropdown
					v-model="localSettings.fontStyle"
					:options="fontStyleOptions"
				/>
			</div>
			<div class="toc-settings-card__row">
				<label class="toc-settings-card__label">Начертание</label>
				<Dropdown
					v-model="localSettings.fontWeight"
					:options="fontWeightOptions"
				/>
			</div>
			<div class="toc-settings-card__row">
				<label class="toc-settings-card__label">Кегль (pt)</label>
				<Slider
					v-model="localSettings.fontSize"
					:min="8"
					:max="24"
					unit="pt"
				/>
			</div>
			<div class="toc-settings-card__row">
				<label class="toc-settings-card__label">Отступ вложенности (мм)</label>
				<Slider
					v-model="localSettings.indentPerLevel"
					:min="0"
					:max="20"
					unit="мм"
				/>
			</div>
			<div class="toc-settings-card__row">
				<label class="toc-settings-card__checkbox">
					<input type="checkbox" v-model="localSettings.nestingEnabled" />
					Вложенность
				</label>
			</div>
			<div class="toc-settings-card__row">
				<label class="toc-settings-card__checkbox">
					<input type="checkbox" v-model="localSettings.numberingEnabled" />
					Нумерация пунктов
				</label>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import Dropdown from '@/shared/ui/Dropdown/Dropdown.vue';
import Slider from '@/shared/ui/Slider/Slider.vue';
import type { TableOfContentsSettings } from '@/entities/profile/types';

const defaultSettings: TableOfContentsSettings = {
	fontStyle: 'normal',
	fontWeight: 'normal',
	fontSize: 14,
	indentPerLevel: 5,
	nestingEnabled: true,
	numberingEnabled: true,
};

interface Props {
	modelValue: TableOfContentsSettings;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: TableOfContentsSettings];
}>();

const localSettings = ref<TableOfContentsSettings>({
	...defaultSettings,
	...props.modelValue,
});
const isUpdating = ref(false);

watch(
	() => props.modelValue,
	(newValue) => {
		if (isUpdating.value) return;
		const currentStr = JSON.stringify(localSettings.value);
		const newStr = JSON.stringify(newValue);
		if (currentStr === newStr) return;
		isUpdating.value = true;
		localSettings.value = { ...defaultSettings, ...newValue };
		nextTick(() => {
			isUpdating.value = false;
		});
	},
	{ deep: true },
);

watch(
	localSettings,
	(newValue) => {
		if (isUpdating.value) return;
		isUpdating.value = true;
		emit('update:modelValue', { ...newValue });
		nextTick(() => {
			isUpdating.value = false;
		});
	},
	{ deep: true },
);

const fontStyleOptions = [
	{ value: 'normal', label: 'Обычный' },
	{ value: 'italic', label: 'Курсив' },
];

const fontWeightOptions = [
	{ value: 'normal', label: 'Обычный' },
	{ value: 'bold', label: 'Жирный' },
];
</script>

<style scoped>
.toc-settings-card {
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-lg);
	padding: var(--spacing-lg);
}

.toc-settings-card__title {
	font-size: 18px;
	font-weight: 600;
	margin: 0 0 var(--spacing-md);
	color: var(--text-primary);
}

.toc-settings-card__content {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
}

.toc-settings-card__row {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-xs);
}

.toc-settings-card__label {
	font-size: 13px;
	font-weight: 500;
	color: var(--text-secondary);
}

.toc-settings-card__checkbox {
	display: flex;
	align-items: center;
	gap: var(--spacing-sm);
	font-size: 14px;
	cursor: pointer;
	color: var(--text-primary);
}
</style>
