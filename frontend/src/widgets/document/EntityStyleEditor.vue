<template>
	<div class="style-editor" v-if="style && entityType">
		<div class="style-editor-header">
			<span class="style-editor-title">
				{{ entityLabel }}
			</span>
			<div class="style-editor-actions">
				<button
					v-if="showReset"
					class="btn-reset"
					type="button"
					@click="$emit('reset')"
				>
					Сбросить
				</button>
				<button
					class="btn-close"
					type="button"
					@click="$emit('close')"
					title="Закрыть"
				>
					×
				</button>
			</div>
		</div>

		<div class="style-editor-content">
			<!-- Типографика -->
			<div class="style-section">
				<div class="style-section-title">Типографика</div>
				<div class="form-row">
					<div class="form-group">
						<label class="form-label">Размер (pt)</label>
						<input
							type="number"
							class="form-input"
							:min="6"
							:max="72"
							:placeholder="String(baseStyle.fontSize ?? '')"
							:value="localStyle.fontSize ?? ''"
							@input="updateNumericStyle('fontSize', $event)"
						/>
					</div>

					<div class="form-group">
						<label class="form-label">Жирность</label>
						<select
							class="form-input"
							:value="localStyle.fontWeight ?? ''"
							@change="updateSelectStyle('fontWeight', $event)"
						>
							<option value="">По умолч.</option>
							<option value="normal">Обычный</option>
							<option value="bold">Жирный</option>
						</select>
					</div>

					<div class="form-group">
						<label class="form-label">Начертание</label>
						<select
							class="form-input"
							:value="localStyle.fontStyle ?? ''"
							@change="updateSelectStyle('fontStyle', $event)"
						>
							<option value="">По умолч.</option>
							<option value="normal">Обычный</option>
							<option value="italic">Курсив</option>
						</select>
					</div>
				</div>
			</div>

			<!-- Форматирование текста -->
			<div class="style-section">
				<div class="style-section-title">Форматирование</div>
				<div class="form-row">
					<div class="form-group">
						<label class="form-label">Выравнивание</label>
						<select
							class="form-input"
							:value="localStyle.textAlign ?? ''"
							@change="updateSelectStyle('textAlign', $event)"
						>
							<option value="">По умолч.</option>
							<option value="left">По левому</option>
							<option value="center">По центру</option>
							<option value="right">По правому</option>
							<option value="justify">По ширине</option>
						</select>
					</div>

					<div
						v-if="showIndent"
						class="form-group"
					>
						<label class="form-label">Красная строка (см)</label>
						<input
							type="number"
							class="form-input"
							step="0.25"
							min="0"
							max="5"
							:placeholder="String(baseStyle.textIndent ?? '')"
							:value="localStyle.textIndent ?? ''"
							@input="updateNumericStyle('textIndent', $event, true)"
						/>
					</div>

					<div class="form-group">
						<label class="form-label">Межстрочный интервал</label>
						<input
							type="number"
							class="form-input"
							step="0.1"
							min="0.5"
							max="3"
							:placeholder="String(baseStyle.lineHeight ?? '')"
							:value="localStyle.lineHeight ?? ''"
							@input="updateNumericStyle('lineHeight', $event, true)"
						/>
					</div>
				</div>
			</div>

			<!-- Отступы -->
			<div class="style-section">
				<div class="style-section-title">Внешние отступы (pt)</div>
				<div class="form-row">
					<div class="form-group">
						<label class="form-label">Сверху</label>
						<input
							type="number"
							class="form-input"
							min="0"
							max="200"
							:placeholder="String(baseStyle.marginTop ?? '')"
							:value="localStyle.marginTop ?? ''"
							@input="updateNumericStyle('marginTop', $event)"
						/>
					</div>
					<div class="form-group">
						<label class="form-label">Снизу</label>
						<input
							type="number"
							class="form-input"
							min="0"
							max="200"
							:placeholder="String(baseStyle.marginBottom ?? '')"
							:value="localStyle.marginBottom ?? ''"
							@input="updateNumericStyle('marginBottom', $event)"
						/>
					</div>
				</div>
			</div>

			<!-- Цвета -->
			<div class="style-section">
				<div class="style-section-title">Цвета</div>
				<div class="form-row">
					<div class="form-group">
						<label class="form-label">Цвет текста</label>
						<input
							type="color"
							class="form-input color-input"
							:value="localStyle.color ?? '#000000'"
							@input="updateStyle('color', ($event.target as HTMLInputElement).value)"
						/>
					</div>
					<div class="form-group">
						<label class="form-label">Фон</label>
						<input
							type="color"
							class="form-input color-input"
							:value="localStyle.backgroundColor ?? '#ffffff'"
							@input="updateStyle('backgroundColor', ($event.target as HTMLInputElement).value)"
						/>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import type { EntityType } from '@/entities/profile/constants';
import { ENTITY_LABELS } from '@/entities/profile/constants';
import type { EntityStyle } from '@/shared/services/markdown/renderUtils';
import { getBaseStyle } from '@/shared/services/markdown/renderUtils';
import type { Profile } from '@/entities/profile/types';

interface Props {
	entityType: EntityType | null;
	style: EntityStyle | null;
	profile: Profile | null;
	showReset?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	showReset: false,
});

const emit = defineEmits<{
	change: [style: EntityStyle];
	reset: [];
	close: [];
}>();

const entityLabel = computed(() => {
	if (!props.entityType) return 'Стили элемента';
	return ENTITY_LABELS[props.entityType] || 'Стили элемента';
});

const localStyle = reactive<EntityStyle>({});

const baseStyle = computed(() => {
	if (!props.entityType) return {};
	return getBaseStyle(props.entityType, props.profile);
});

const showIndent = computed(() => {
	return props.entityType === 'paragraph' || props.entityType === 'ordered-list' || props.entityType === 'unordered-list';
});

watch(
	() => props.style,
	(newStyle) => {
		Object.keys(localStyle).forEach((key) => {
			delete (localStyle as Record<string, unknown>)[key];
		});
		if (newStyle) {
			Object.assign(localStyle, newStyle);
		}
	},
	{ immediate: true }
);

function updateStyle(key: keyof EntityStyle, value: unknown) {
	(localStyle as Record<string, unknown>)[key] = value;
	emit('change', { ...localStyle });
}

function updateSelectStyle(key: keyof EntityStyle, event: Event) {
	const target = event.target as HTMLSelectElement;
	const value = target.value || undefined;
	updateStyle(key, value);
}

function updateNumericStyle(key: keyof EntityStyle, event: Event, allowFloat = false) {
	const target = event.target as HTMLInputElement;
	const value = target.value;

	if (value === '') {
		delete (localStyle as Record<string, unknown>)[key];
		emit('change', { ...localStyle });
		return;
	}

	const num = allowFloat ? parseFloat(value) : parseInt(value, 10);
	if (!isNaN(num)) {
		updateStyle(key, num);
	}
}
</script>

<style scoped>
.style-editor {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
	padding: 0.75rem 1rem;
	background: #18181b;
	border-left: 1px solid #27272a;
	height: 100%;
	box-sizing: border-box;
}

.style-editor-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 0.5rem;
}

.style-editor-actions {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.style-editor-title {
	font-size: 0.9rem;
	font-weight: 600;
	color: #e4e4e7;
}

.btn-reset {
	padding: 0.25rem 0.5rem;
	font-size: 0.75rem;
	border-radius: 4px;
	border: 1px solid #3f3f46;
	background: transparent;
	color: #a1a1aa;
	cursor: pointer;
}

.btn-reset:hover {
	background: #27272a;
	color: #e4e4e7;
}

.btn-close {
	padding: 0;
	width: 1.5rem;
	height: 1.5rem;
	font-size: 1.25rem;
	line-height: 1;
	border-radius: 4px;
	border: 1px solid #3f3f46;
	background: transparent;
	color: #a1a1aa;
	cursor: pointer;
	display: flex;
	align-items: center;
	justify-content: center;
	transition: all 0.2s;
}

.btn-close:hover {
	background: #27272a;
	color: #e4e4e7;
	border-color: #52525b;
}

.style-editor-content {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
	overflow-y: auto;
}

.style-section {
	border-radius: 6px;
	border: 1px solid #27272a;
	padding: 0.75rem;
	background: #09090b;
}

.style-section-title {
	font-size: 0.8rem;
	font-weight: 500;
	color: #a1a1aa;
	margin-bottom: 0.5rem;
}

.form-row {
	display: flex;
	flex-wrap: wrap;
	gap: 0.5rem;
}

.form-group {
	flex: 1 1 120px;
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
}

.form-label {
	font-size: 0.75rem;
	color: #a1a1aa;
}

.form-input {
	width: 100%;
	padding: 0.25rem 0.5rem;
	font-size: 0.8rem;
	border-radius: 4px;
	border: 1px solid #27272a;
	background: #18181b;
	color: #e4e4e7;
	box-sizing: border-box;
}

.form-input:focus {
	outline: none;
	border-color: #4f46e5;
}

.color-input {
	padding: 0;
	height: 1.75rem;
}
</style>

