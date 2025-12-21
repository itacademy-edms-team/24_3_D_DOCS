<template>
	<div class="style-editor">
		<div class="style-editor-header">
			<span class="style-editor-title">{{ displayTitle }}</span>
			<button
				v-if="showReset"
				class="btn btn-ghost btn-sm"
				@click="$emit('reset')"
			>
				Сбросить
			</button>
		</div>

		<div class="style-editor-content">
			<!-- Typography Section -->
			<template
				v-if="
					['paragraph', 'heading', 'image-caption', 'table-caption', 'formula-caption', 'ordered-list', 'unordered-list', 'table'].includes(
						entityType,
					)
				"
			>
				<div class="style-section">
					<div class="style-section-title">Типографика</div>
					<div class="form-row">
						<div class="form-group">
							<label class="form-label">Размер (pt)</label>
							<input
								type="number"
								class="form-input form-input-sm"
								:value="localStyle.fontSize ?? ''"
								@input="updateNumericStyle('fontSize', $event)"
								min="6"
								max="72"
							/>
						</div>

						<div class="form-group">
							<label class="form-label">Шрифт</label>
							<select
								class="form-select form-select-sm"
								:value="localStyle.fontFamily ?? ''"
								@change="updateStyle('fontFamily', $event)"
							>
								<option value="">По умолч.</option>
								<option
									v-for="font in FONT_FAMILIES"
									:key="font.value"
									:value="font.value"
								>
									{{ font.label }}
								</option>
							</select>
						</div>

						<div class="form-group">
							<label class="form-label">Жирность</label>
							<select
								class="form-select form-select-sm"
								:value="localStyle.fontWeight ?? ''"
								@change="updateStyle('fontWeight', $event)"
							>
								<option value="">По умолч.</option>
								<option value="normal">Обычный</option>
								<option value="bold">Жирный</option>
							</select>
						</div>

						<div class="form-group">
							<label class="form-label">Начертание</label>
							<select
								class="form-select form-select-sm"
								:value="localStyle.fontStyle ?? ''"
								@change="updateStyle('fontStyle', $event)"
							>
								<option value="">По умолч.</option>
								<option value="normal">Обычный</option>
								<option value="italic">Курсив</option>
							</select>
						</div>
					</div>
				</div>
			</template>

			<!-- Text Formatting Section -->
			<template
				v-if="
					['paragraph', 'heading', 'image-caption', 'table-caption', 'formula-caption', 'image', 'formula'].includes(
						entityType,
					)
				"
			>
				<div class="style-section">
					<div class="style-section-title">Форматирование</div>
					<div class="form-row">
						<div class="form-group">
							<label class="form-label">Выравнивание</label>
							<select
								class="form-select form-select-sm"
								:value="localStyle.textAlign ?? ''"
								@change="updateStyle('textAlign', $event)"
							>
								<option value="">По умолч.</option>
								<option value="left">По левому</option>
								<option value="center">По центру</option>
								<option value="right">По правому</option>
								<option value="justify">По ширине</option>
							</select>
						</div>

						<template
							v-if="['paragraph', 'ordered-list', 'unordered-list'].includes(entityType)"
						>
							<div class="form-group">
								<label class="form-label">Красная строка (см)</label>
								<input
									type="number"
									class="form-input form-input-sm"
									:value="localStyle.textIndent ?? ''"
									@input="updateNumericStyle('textIndent', $event, true)"
									step="0.25"
									min="0"
									max="5"
								/>
							</div>
						</template>

						<div class="form-group">
							<label class="form-label">Межстрочный интервал</label>
							<input
								type="number"
								class="form-input form-input-sm"
								:value="localStyle.lineHeight ?? ''"
								@input="updateNumericStyle('lineHeight', $event, true)"
								step="0.1"
								min="0.5"
								max="3"
							/>
						</div>
					</div>
				</div>
			</template>

			<!-- Margins Section -->
			<div class="style-section">
				<div class="style-section-title">Внешние отступы (pt)</div>
				<div class="form-row">
					<div class="form-group">
						<label class="form-label">Сверху</label>
						<input
							type="number"
							class="form-input form-input-sm"
							:value="localStyle.marginTop ?? ''"
							@input="updateNumericStyle('marginTop', $event)"
							min="0"
							max="200"
						/>
					</div>

					<div class="form-group">
						<label class="form-label">Снизу</label>
						<input
							type="number"
							class="form-input form-input-sm"
							:value="localStyle.marginBottom ?? ''"
							@input="updateNumericStyle('marginBottom', $event)"
							min="0"
							max="200"
						/>
					</div>

					<div class="form-group">
						<label class="form-label">Слева</label>
						<input
							type="number"
							class="form-input form-input-sm"
							:value="localStyle.marginLeft ?? ''"
							@input="updateNumericStyle('marginLeft', $event)"
							min="0"
							max="200"
						/>
					</div>

					<div class="form-group">
						<label class="form-label">Справа</label>
						<input
							type="number"
							class="form-input form-input-sm"
							:value="localStyle.marginRight ?? ''"
							@input="updateNumericStyle('marginRight', $event)"
							min="0"
							max="200"
						/>
					</div>
				</div>
			</div>

			<!-- Border Section (for tables) -->
			<template v-if="entityType === 'table'">
				<div class="style-section">
					<div class="style-section-title">Границы</div>
					<div class="form-row">
						<div class="form-group">
							<label class="form-label">Ширина (px)</label>
							<input
								type="number"
								class="form-input form-input-sm"
								:value="localStyle.borderWidth ?? ''"
								@input="updateNumericStyle('borderWidth', $event)"
								min="0"
								max="10"
							/>
						</div>

						<div class="form-group">
							<label class="form-label">Цвет</label>
							<input
								type="color"
								class="form-input form-input-sm color-input"
								:value="localStyle.borderColor || '#000000'"
								@input="updateStyle('borderColor', $event)"
							/>
						</div>

						<div class="form-group">
							<label class="form-label">Стиль</label>
							<select
								class="form-select form-select-sm"
								:value="localStyle.borderStyle ?? 'solid'"
								@change="updateStyle('borderStyle', $event)"
							>
								<option value="none">Нет</option>
								<option value="solid">Сплошная</option>
								<option value="dashed">Пунктир</option>
							</select>
						</div>
					</div>
				</div>
			</template>

			<!-- Max Width Section (for images) -->
			<template v-if="entityType === 'image'">
				<div class="style-section">
					<div class="style-section-title">Размер</div>
					<div class="form-row">
						<div class="form-group">
							<label class="form-label">Макс. ширина (%)</label>
							<input
								type="number"
								class="form-input form-input-sm"
								:value="localStyle.maxWidth ?? ''"
								@input="updateNumericStyle('maxWidth', $event)"
								min="0"
								max="100"
							/>
						</div>
					</div>
				</div>
			</template>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import type { EntityType } from '@/entities/profile/constants';
import { ENTITY_LABELS, FONT_FAMILIES } from '@/entities/profile/constants';

interface Props {
	entityType: EntityType;
	style: Record<string, any>;
	title?: string;
	showReset?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	showReset: false,
});

const emit = defineEmits<{
	change: [style: Record<string, any>];
	reset: [];
}>();

const localStyle = ref<Record<string, any>>({ ...props.style });

watch(
	() => props.style,
	(newStyle) => {
		localStyle.value = { ...newStyle };
	},
	{ deep: true },
);

const displayTitle = props.title || ENTITY_LABELS[props.entityType];

function updateStyle(key: string, event: Event) {
	const target = event.target as HTMLSelectElement | HTMLInputElement;
	const value = target.value || undefined;
	const newStyle = { ...localStyle.value };
	if (value === undefined || value === '') {
		delete newStyle[key];
	} else {
		newStyle[key] = value;
	}
	localStyle.value = newStyle;
	emit('change', newStyle);
}

function updateNumericStyle(key: string, event: Event, allowFloat = false) {
	const target = event.target as HTMLInputElement;
	const value = target.value;
	if (value === '') {
		const newStyle = { ...localStyle.value };
		delete newStyle[key];
		localStyle.value = newStyle;
		emit('change', newStyle);
		return;
	}
	const num = allowFloat ? parseFloat(value) : parseInt(value, 10);
	if (!isNaN(num)) {
		const newStyle = { ...localStyle.value, [key]: num };
		localStyle.value = newStyle;
		emit('change', newStyle);
	}
}
</script>

<style scoped>
.style-editor {
	background: #18181b;
	border: 1px solid #27272a;
	border-radius: 8px;
	padding: 1.5rem;
}

.style-editor-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 1.5rem;
}

.style-editor-title {
	font-size: 18px;
	font-weight: 600;
	color: #e4e4e7;
}

.btn {
	padding: 0.5rem 1rem;
	border-radius: 6px;
	font-size: 13px;
	font-weight: 500;
	cursor: pointer;
	transition: all 0.2s;
	border: none;
	font-family: inherit;
}

.btn-ghost {
	background: transparent;
	color: #a1a1aa;
	border: 1px solid #27272a;
}

.btn-ghost:hover {
	background: #27272a;
	color: #e4e4e7;
}

.btn-sm {
	padding: 0.4rem 0.75rem;
	font-size: 12px;
}

.style-editor-content {
	display: flex;
	flex-direction: column;
	gap: 1.5rem;
}

.style-section {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
}

.style-section-title {
	font-size: 14px;
	font-weight: 600;
	color: #a1a1aa;
	text-transform: uppercase;
	letter-spacing: 0.5px;
}

.form-row {
	display: grid;
	grid-template-columns: repeat(4, 1fr);
	gap: 0.75rem;
}

.form-group {
	display: flex;
	flex-direction: column;
}

.form-label {
	font-size: 12px;
	font-weight: 500;
	color: #71717a;
	margin-bottom: 0.4rem;
}

.form-input,
.form-select {
	width: 100%;
	padding: 0.5rem 0.75rem;
	background: #0a0a0a;
	border: 1px solid #27272a;
	border-radius: 6px;
	color: #e4e4e7;
	font-size: 13px;
	outline: none;
	transition: border-color 0.2s;
	font-family: inherit;
}

.form-input-sm,
.form-select-sm {
	padding: 0.4rem 0.6rem;
	font-size: 12px;
}

.color-input {
	height: 36px;
	cursor: pointer;
}

.form-input:focus,
.form-select:focus {
	border-color: #6366f1;
}

.form-select option {
	background: #18181b;
	color: #e4e4e7;
}

.form-input::placeholder {
	color: #71717a;
}
</style>
