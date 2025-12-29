<template>
	<div class="heading-numbering-form">
		<div class="heading-numbering-form__section">
			<h4 class="heading-numbering-form__section-title">Нумерация заголовков</h4>
			<p class="heading-numbering-form__description">
				Настройте шаблоны нумерации для каждого уровня заголовков. Используйте {n} для номера и {content} для текста заголовка.
			</p>
			<div class="heading-numbering-form__levels">
				<div
					v-for="level in 6"
					:key="level"
					class="heading-numbering-form__level"
				>
					<div class="heading-numbering-form__level-header">
						<label class="heading-numbering-form__level-checkbox">
							<input
								type="checkbox"
								v-model="localSettings.templates[level].enabled"
							/>
							<span class="heading-numbering-form__level-label">Заголовок H{{ level }}</span>
						</label>
					</div>
					<div
						v-if="localSettings.templates[level].enabled"
						class="heading-numbering-form__level-content"
					>
						<label class="heading-numbering-form__label">Шаблон</label>
						<input
							v-model="localSettings.templates[level].format"
							type="text"
							class="heading-numbering-form__format-input"
							:placeholder="`{n} {content}`"
						/>
						<small class="heading-numbering-form__hint">
							Пример: "{{ level }}. {content}" → "1. Введение" (если это первый H{{ level }})
						</small>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import type { HeadingNumberingSettings, HeadingTemplate } from '@/entities/profile/types';

interface Props {
	modelValue?: HeadingNumberingSettings;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: HeadingNumberingSettings];
}>();

const defaultTemplates: Record<number, HeadingTemplate> = {};
for (let i = 1; i <= 6; i++) {
	defaultTemplates[i] = {
		format: '{n} {content}',
		enabled: false,
	};
}

const localSettings = ref<HeadingNumberingSettings>({
	templates: props.modelValue?.templates
		? { ...defaultTemplates, ...props.modelValue.templates }
		: { ...defaultTemplates },
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
		localSettings.value = {
			templates: newValue?.templates
				? { ...defaultTemplates, ...newValue.templates }
				: { ...defaultTemplates },
		};
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
</script>

<style scoped>
.heading-numbering-form {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-xl);
}

.heading-numbering-form__section {
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	padding: var(--spacing-lg);
}

.heading-numbering-form__section-title {
	font-size: 16px;
	font-weight: 600;
	color: var(--text-primary);
	margin: 0 0 var(--spacing-md) 0;
	padding-bottom: var(--spacing-sm);
	border-bottom: 1px solid var(--border-color);
}

.heading-numbering-form__description {
	font-size: 13px;
	color: var(--text-secondary);
	margin: 0 0 var(--spacing-lg) 0;
	line-height: 1.5;
}

.heading-numbering-form__levels {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
}

.heading-numbering-form__level {
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	padding: var(--spacing-md);
}

.heading-numbering-form__level-header {
	margin-bottom: var(--spacing-sm);
}

.heading-numbering-form__level-checkbox {
	display: flex;
	align-items: center;
	gap: var(--spacing-xs);
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
	cursor: pointer;
}

.heading-numbering-form__level-label {
	user-select: none;
}

.heading-numbering-form__level-content {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-xs);
	padding-top: var(--spacing-sm);
	border-top: 1px solid var(--border-color);
}

.heading-numbering-form__label {
	font-size: 13px;
	font-weight: 500;
	color: var(--text-secondary);
}

.heading-numbering-form__format-input {
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 14px;
	font-family: monospace;
	width: 100%;
}

.heading-numbering-form__format-input:focus {
	outline: none;
	border-color: var(--primary-color);
}

.heading-numbering-form__hint {
	font-size: 12px;
	color: var(--text-secondary);
	font-style: italic;
}
</style>
