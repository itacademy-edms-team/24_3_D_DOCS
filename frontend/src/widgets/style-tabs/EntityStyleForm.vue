<template>
	<div class="entity-style-form">
		<div class="entity-style-form__section">
			<h4 class="entity-style-form__section-title">Текст</h4>
			<div class="entity-style-form__fields">
				<div class="entity-style-form__field">
					<label class="entity-style-form__label">Шрифт</label>
					<Dropdown
						v-model="localStyle.fontFamily"
						:options="fontOptions"
						placeholder="По умолчанию"
					/>
				</div>
				<div class="entity-style-form__field">
					<label class="entity-style-form__label">Размер</label>
					<Slider
						v-model="localStyle.fontSize"
						:min="8"
						:max="72"
						unit="pt"
					/>
				</div>
				<div class="entity-style-form__field">
					<label class="entity-style-form__label">Начертание</label>
					<Dropdown
						v-model="localStyle.fontWeight"
						:options="weightOptions"
					/>
				</div>
				<div class="entity-style-form__field">
					<label class="entity-style-form__label">Выравнивание</label>
					<Dropdown
						v-model="localStyle.textAlign"
						:options="alignOptions"
					/>
				</div>
				<div class="entity-style-form__field" v-if="showTextIndent">
					<label class="entity-style-form__label">Отступ первой строки</label>
					<Slider
						v-model="localStyle.textIndent"
						:min="0"
						:max="5"
						:step="0.1"
						unit="см"
					/>
				</div>
				<div class="entity-style-form__field">
					<label class="entity-style-form__label">Межстрочный интервал</label>
					<div class="entity-style-form__line-height-controls">
						<label class="entity-style-form__checkbox">
							<input
								type="checkbox"
								v-model="localStyle.lineHeightUseGlobal"
							/>
							Использовать глобальный
						</label>
						<div v-if="!localStyle.lineHeightUseGlobal" class="entity-style-form__slider-wrapper">
							<Slider
								v-model="localStyle.lineHeight"
								:min="1"
								:max="3"
								:step="0.1"
							/>
						</div>
						<div v-else class="entity-style-form__global-value">
							<span class="entity-style-form__global-value-text">
								Глобальное значение: {{ pageSettings.globalLineHeight || 'не задано' }}
							</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<div class="entity-style-form__section">
			<h4 class="entity-style-form__section-title">Отступы</h4>
			<div class="entity-style-form__fields">
				<div class="entity-style-form__field">
					<label class="entity-style-form__label">Сверху</label>
					<Slider
						v-model="localStyle.marginTop"
						:min="0"
						:max="50"
						unit="pt"
					/>
				</div>
				<div class="entity-style-form__field">
					<label class="entity-style-form__label">Снизу</label>
					<Slider
						v-model="localStyle.marginBottom"
						:min="0"
						:max="50"
						unit="pt"
					/>
				</div>
			</div>
		</div>

		<div class="entity-style-form__section" v-if="isHighlight">
			<h4 class="entity-style-form__section-title">Выделение</h4>
			<div class="entity-style-form__fields">
				<div class="entity-style-form__field">
					<label class="entity-style-form__label">Цвет текста</label>
					<div class="entity-style-form__color-input">
						<input
							v-model="localStyle.highlightColor"
							type="color"
							class="entity-style-form__color-picker"
						/>
						<input
							v-model="localStyle.highlightColor"
							type="text"
							class="entity-style-form__color-text"
							placeholder="#000000"
							pattern="^#[0-9A-Fa-f]{6}$"
						/>
					</div>
				</div>
				<div class="entity-style-form__field">
					<label class="entity-style-form__label">Цвет фона</label>
					<div class="entity-style-form__color-input">
						<input
							v-model="localStyle.highlightBackgroundColor"
							type="color"
							class="entity-style-form__color-picker"
						/>
						<input
							v-model="localStyle.highlightBackgroundColor"
							type="text"
							class="entity-style-form__color-text"
							placeholder="#ffeb3b"
							pattern="^#[0-9A-Fa-f]{6}$"
						/>
					</div>
				</div>
			</div>
		</div>

		<div class="entity-style-form__section" v-if="isCaptionType">
			<h4 class="entity-style-form__section-title">Шаблон подписи</h4>
			<div class="entity-style-form__fields">
				<div class="entity-style-form__field" style="grid-column: 1 / -1;">
					<label class="entity-style-form__label">Формат подписи</label>
					<input
						v-model="localStyle.captionFormat"
						type="text"
						class="entity-style-form__caption-input"
						:placeholder="captionPlaceholder"
					/>
					<small class="entity-style-form__hint">
						Используйте {n} для номера и {content} для текста подписи из markdown
					</small>
				</div>
			</div>
		</div>

		<div class="entity-style-form__section" v-if="isListType">
			<h4 class="entity-style-form__section-title">Отступы списка</h4>
			<div class="entity-style-form__fields">
				<div class="entity-style-form__field" style="grid-column: 1 / -1;">
					<label class="entity-style-form__checkbox">
						<input
							type="checkbox"
							v-model="localStyle.listUseParagraphTextIndent"
						/>
						Использовать красную строку из параграфа
					</label>
					<small class="entity-style-form__hint">
						Красная строка применяется только к первой строке первого элемента списка
					</small>
				</div>
				<div
					v-if="!localStyle.listUseParagraphTextIndent"
					class="entity-style-form__field"
				>
					<label class="entity-style-form__label">Отступ первой строки</label>
					<Slider
						v-model="localStyle.textIndent"
						:min="0"
						:max="5"
						:step="0.1"
						unit="см"
					/>
					<small class="entity-style-form__hint">
						Красная строка только для первого элемента
					</small>
				</div>
				<div
					v-if="!localStyle.listUseParagraphTextIndent"
					class="entity-style-form__field"
					style="grid-column: 1 / -1;"
				>
					<label class="entity-style-form__label">Добавочный отступ для каждого уровня</label>
					<Slider
						v-model="localStyle.listAdditionalIndent"
						:min="-50"
						:max="50"
						:step="1"
						unit="мм"
					/>
					<small class="entity-style-form__hint">
						Отступ применяется как: уровень_вложенности × значение_отступа
					</small>
				</div>
			</div>
		</div>

		<div class="entity-style-form__section" v-if="isHeadingType">
			<h4 class="entity-style-form__section-title">Нумерация заголовков</h4>
			<div class="entity-style-form__fields">
				<div class="entity-style-form__field" style="grid-column: 1 / -1;">
					<label class="entity-style-form__checkbox">
						<input
							type="checkbox"
							v-model="localHeadingNumbering.enabled"
						/>
						Включить нумерацию
					</label>
				</div>
				<div
					v-if="localHeadingNumbering.enabled"
					class="entity-style-form__field"
					style="grid-column: 1 / -1;"
				>
					<label class="entity-style-form__label">Шаблон</label>
					<input
						v-model="localHeadingNumbering.format"
						type="text"
						class="entity-style-form__caption-input"
						placeholder="{n} {content}"
					/>
					<small class="entity-style-form__hint">
						Используйте {n} для номера и {content} для текста заголовка. Пример: "{{ headingLevel }}. {content}" → "1. Введение"
					</small>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, computed, nextTick } from 'vue';
import Dropdown from '@/shared/ui/Dropdown/Dropdown.vue';
import Slider from '@/shared/ui/Slider/Slider.vue';
import type { EntityStyle, PageSettings, HeadingNumberingSettings, HeadingTemplate } from '@/entities/profile/types';

interface Props {
	entityType: string;
	style: EntityStyle;
	pageSettings: PageSettings;
	headingNumbering?: HeadingNumberingSettings;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:style': [style: EntityStyle];
	'update:headingNumbering': [settings: HeadingNumberingSettings];
}>();

const localStyle = ref<EntityStyle>({ ...props.style });
const isUpdating = ref(false);

// Extract heading level from entityType (e.g., 'heading-1' -> 1)
const headingLevel = computed<number | null>(() => {
	if (props.entityType.startsWith('heading-')) {
		const level = parseInt(props.entityType.replace('heading-', ''), 10);
		return isNaN(level) ? null : level;
	}
	return null;
});

const isHeadingType = computed(() => headingLevel.value !== null);

// Local heading numbering state for this level
const localHeadingNumbering = ref<HeadingTemplate>({
	format: '{n} {content}',
	enabled: false,
});

const isUpdatingHeadingNumbering = ref(false);

// Initialize local heading numbering from props
watch(
	() => [props.headingNumbering, headingLevel.value],
	() => {
		if (isUpdatingHeadingNumbering.value) return;
		if (headingLevel.value && props.headingNumbering?.templates) {
			const template = props.headingNumbering.templates[headingLevel.value];
			if (template) {
				const currentStr = JSON.stringify(localHeadingNumbering.value);
				const newStr = JSON.stringify(template);
				if (currentStr !== newStr) {
					localHeadingNumbering.value = { ...template };
				}
			} else {
				localHeadingNumbering.value = { format: '{n} {content}', enabled: false };
			}
		} else if (headingLevel.value) {
			localHeadingNumbering.value = { format: '{n} {content}', enabled: false };
		}
	},
	{ immediate: true, deep: true },
);

// Watch local heading numbering changes and emit updates
watch(
	localHeadingNumbering,
	(newValue) => {
		if (isUpdatingHeadingNumbering.value) return;
		if (!headingLevel.value) return;

		isUpdatingHeadingNumbering.value = true;

		const updatedSettings: HeadingNumberingSettings = {
			templates: {
				...(props.headingNumbering?.templates || {}),
				[headingLevel.value]: { ...newValue },
			},
		};

		emit('update:headingNumbering', updatedSettings);

		nextTick(() => {
			isUpdatingHeadingNumbering.value = false;
		});
	},
	{ deep: true },
);

const showTextIndent = computed(() =>
	['paragraph', 'heading', 'heading-1', 'heading-2', 'heading-3', 'heading-4', 'heading-5', 'heading-6'].includes(props.entityType),
);

const isHighlight = computed(() => props.entityType === 'highlight');

const isCaptionType = computed(() =>
	['image-caption', 'table-caption', 'formula-caption'].includes(props.entityType),
);

const captionPlaceholder = computed(() => {
	switch (props.entityType) {
		case 'image-caption':
			return 'Рисунок {n} - {content}';
		case 'table-caption':
			return 'Таблица {n} - {content}';
		case 'formula-caption':
			return 'Формула {n} - {content}';
		default:
			return '{n} - {content}';
	}
});

const isListType = computed(() =>
	['ordered-list', 'unordered-list'].includes(props.entityType),
);

watch(
	() => props.style,
	(newStyle) => {
		// Избегаем циклических обновлений
		if (isUpdating.value) return;
		
		// Проверяем, действительно ли изменились данные
		const currentStr = JSON.stringify(localStyle.value);
		const newStr = JSON.stringify(newStyle);
		if (currentStr === newStr) return;
		
		isUpdating.value = true;
		localStyle.value = { ...newStyle };
		// Используем nextTick чтобы избежать конфликтов
		nextTick(() => {
			isUpdating.value = false;
		});
	},
	{ deep: true },
);

watch(
	localStyle,
	(newStyle) => {
		// Избегаем циклических обновлений
		if (isUpdating.value) return;
		
		isUpdating.value = true;
		emit('update:style', { ...newStyle });
		// Используем nextTick чтобы избежать конфликтов
		nextTick(() => {
			isUpdating.value = false;
		});
	},
	{ deep: true },
);

const fontOptions = [
	{ value: '', label: 'По умолчанию' },
	{ value: 'Times New Roman', label: 'Times New Roman' },
	{ value: 'Arial', label: 'Arial' },
	{ value: 'Calibri', label: 'Calibri' },
	{ value: 'Georgia', label: 'Georgia' },
	{ value: 'Verdana', label: 'Verdana' },
	{ value: 'Courier New', label: 'Courier New' },
];

const weightOptions = [
	{ value: 'normal', label: 'Обычный' },
	{ value: 'bold', label: 'Жирный' },
];

const alignOptions = [
	{ value: 'left', label: 'Слева' },
	{ value: 'center', label: 'Центр' },
	{ value: 'right', label: 'Справа' },
	{ value: 'justify', label: 'По ширине' },
];
</script>

<style scoped>
.entity-style-form {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-xl);
}

.entity-style-form__section {
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	padding: var(--spacing-lg);
}

.entity-style-form__section-title {
	font-size: 16px;
	font-weight: 600;
	color: var(--text-primary);
	margin: 0 0 var(--spacing-md) 0;
	padding-bottom: var(--spacing-sm);
	border-bottom: 1px solid var(--border-color);
}

.entity-style-form__fields {
	display: grid;
	grid-template-columns: repeat(2, 1fr);
	gap: var(--spacing-md);
}

.entity-style-form__field {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-xs);
}

.entity-style-form__label {
	font-size: 13px;
	font-weight: 500;
	color: var(--text-secondary);
}

.entity-style-form__color-input {
	display: flex;
	gap: var(--spacing-xs);
	align-items: center;
}

.entity-style-form__color-picker {
	width: 40px;
	height: 32px;
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	cursor: pointer;
	padding: 0;
}

.entity-style-form__color-text {
	flex: 1;
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 14px;
	font-family: monospace;
}

.entity-style-form__caption-input {
	width: 100%;
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 14px;
	font-family: monospace;
}

.entity-style-form__caption-input:focus {
	outline: none;
	border-color: var(--primary-color);
}

.entity-style-form__hint {
	font-size: 12px;
	color: var(--text-secondary);
	font-style: italic;
	margin-top: var(--spacing-xs);
}

.entity-style-form__line-height-controls {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-sm);
}

.entity-style-form__checkbox {
	display: flex;
	align-items: center;
	gap: var(--spacing-xs);
	font-size: 13px;
	color: var(--text-primary);
	cursor: pointer;
}

.entity-style-form__slider-wrapper {
	margin-top: var(--spacing-xs);
}

.entity-style-form__global-value {
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	margin-top: var(--spacing-xs);
}

.entity-style-form__global-value-text {
	font-size: 13px;
	color: var(--text-secondary);
	font-style: italic;
}
</style>
