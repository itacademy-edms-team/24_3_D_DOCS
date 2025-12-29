<template>
	<div class="title-page-properties" v-if="selectedElement">
		<h3 class="title-page-properties__title">Свойства элемента</h3>
		<div class="title-page-properties__content">
			<!-- Text/Variable specific -->
			<template v-if="isTextOrVariable">
				<div class="title-page-properties__group" v-if="!isVariable">
					<label class="title-page-properties__label">Текст</label>
					<textarea
						class="title-page-properties__textarea"
						:value="elementText"
						@input="updateElementText($event)"
						placeholder="Введите текст"
					/>
				</div>

				<div class="title-page-properties__group" v-if="isVariable">
					<label class="title-page-properties__label">Ключ переменной</label>
					<input
						type="text"
						class="title-page-properties__input"
						:value="elementText"
						@input="updateElementText($event)"
						placeholder="например: university"
					/>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Размер шрифта (pt)</label>
					<input
						type="number"
						class="title-page-properties__input"
						:value="elementFontSize"
						@input="updateElementProperty('fontSize', $event)"
						min="8"
						max="72"
					/>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Шрифт</label>
					<select
						class="title-page-properties__select"
						:value="elementFontFamily"
						@change="updateElementProperty('fontFamily', $event)"
					>
						<option v-for="font in fontFamilies" :key="font.value" :value="font.value">
							{{ font.label }}
						</option>
					</select>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Начертание</label>
					<select
						class="title-page-properties__select"
						:value="elementFontWeight"
						@change="updateElementProperty('fontWeight', $event)"
					>
						<option value="normal">Обычный</option>
						<option value="bold">Жирный</option>
					</select>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Стиль</label>
					<select
						class="title-page-properties__select"
						:value="elementFontStyle"
						@change="updateElementProperty('fontStyle', $event)"
					>
						<option value="normal">Обычный</option>
						<option value="italic">Курсив</option>
					</select>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Цвет текста</label>
					<input
						type="color"
						class="title-page-properties__color"
						:value="elementFill"
						@input="updateElementProperty('fill', $event)"
					/>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Междустрочный интервал</label>
					<input
						type="number"
						class="title-page-properties__input"
						:value="elementLineHeight"
						@input="updateElementProperty('lineHeight', $event)"
						min="0.5"
						max="3"
						step="0.1"
					/>
					<small class="title-page-properties__hint">Множитель (например, 1.2 = 120%)</small>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Выравнивание</label>
					<select
						class="title-page-properties__select"
						:value="elementTextAlign"
						@change="updateElementProperty('textAlign', $event)"
					>
						<option value="left">Слева</option>
						<option value="center">По центру</option>
						<option value="right">Справа</option>
					</select>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Ширина (мм)</label>
					<input
						type="number"
						class="title-page-properties__input"
						:value="elementWidth"
						@input="updateElementWidth($event)"
						step="0.1"
						min="0"
						placeholder="Авто"
					/>
					<small class="title-page-properties__hint">Оставьте пустым для автоматической ширины</small>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Макс. строк</label>
					<input
						type="number"
						class="title-page-properties__input"
						:value="elementMaxLines"
						@input="updateElementMaxLines($event)"
						min="1"
						placeholder="Не ограничено"
					/>
					<small class="title-page-properties__hint">Ограничить количество строк текста</small>
				</div>
			</template>

			<!-- Line specific -->
			<template v-if="isLine">
				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Длина (мм)</label>
					<input
						type="number"
						class="title-page-properties__input"
						:value="elementLength"
						@input="updateLineLength($event)"
						min="1"
					/>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Толщина (мм)</label>
					<input
						type="number"
						class="title-page-properties__input"
						:value="elementThickness"
						@input="updateLineThickness($event)"
						min="0.1"
						step="0.1"
					/>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Стиль линии</label>
					<select
						class="title-page-properties__select"
						:value="elementLineStyle"
						@change="updateLineStyle($event)"
					>
						<option value="solid">Сплошная</option>
						<option value="dashed">Пунктирная</option>
					</select>
				</div>

				<div class="title-page-properties__group">
					<label class="title-page-properties__label">Цвет линии</label>
					<input
						type="color"
						class="title-page-properties__color"
						:value="elementStroke"
						@input="updateElementProperty('stroke', $event)"
					/>
				</div>
			</template>

			<!-- Delete button -->
			<div class="title-page-properties__group">
				<button
					class="title-page-properties__delete-button"
					@click="$emit('delete')"
				>
					Удалить элемент
				</button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, type Ref } from 'vue';
import { Canvas, type FabricObject } from 'fabric';
import { useTitlePageElements } from '@/app/composables/useTitlePageElements';

interface Props {
	selectedElement: FabricObject | null;
	canvas: Canvas | null;
	onSave?: () => void;
}

const props = defineProps<Props>();

defineEmits<{
	delete: [];
}>();

const selectedElementRef = ref<FabricObject | null>(props.selectedElement);
const canvasRef = ref<Canvas | null>(props.canvas);

// Sync props with refs
watch(() => props.selectedElement, (val) => {
	selectedElementRef.value = val;
}, { immediate: true });

watch(() => props.canvas, (val) => {
	canvasRef.value = val;
}, { immediate: true });

const {
	isTextOrVariable,
	isVariable,
	isLine,
	elementText,
	elementFontSize,
	elementFontFamily,
	elementFontWeight,
	elementFontStyle,
	elementFill,
	elementLineHeight,
	elementTextAlign,
	elementWidth,
	elementMaxLines,
	elementLength,
	elementThickness,
	elementLineStyle,
	elementStroke,
	updateElementText,
	updateElementProperty,
	updateElementWidth,
	updateElementMaxLines,
	updateLineLength,
	updateLineThickness,
	updateLineStyle,
	fontFamilies,
} = useTitlePageElements(
	selectedElementRef,
	canvasRef,
	props.onSave
);
</script>

<style scoped>
.title-page-properties {
	width: 100%;
	height: 100%;
	padding: var(--spacing-lg);
	background: var(--bg-secondary);
	overflow-y: auto;
	display: flex;
	flex-direction: column;
}

.title-page-properties__title {
	font-size: 16px;
	font-weight: 600;
	color: var(--text-primary);
	margin: 0 0 var(--spacing-md) 0;
}

.title-page-properties__content {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
}

.title-page-properties__group {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-xs);
}

.title-page-properties__label {
	font-size: 13px;
	color: var(--text-primary);
	font-weight: 500;
}


.title-page-properties__input {
	width: 100%;
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 14px;
}

.title-page-properties__input:focus {
	outline: none;
	border-color: var(--accent);
}

.title-page-properties__textarea {
	width: 100%;
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 14px;
	min-height: 60px;
	resize: vertical;
	font-family: inherit;
}

.title-page-properties__textarea:focus {
	outline: none;
	border-color: var(--accent);
}

.title-page-properties__select {
	width: 100%;
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 14px;
	cursor: pointer;
}

.title-page-properties__select:focus {
	outline: none;
	border-color: var(--accent);
}

.title-page-properties__color {
	width: 100%;
	height: 40px;
	padding: 2px;
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	cursor: pointer;
}

.title-page-properties__hint {
	font-size: 11px;
	color: var(--text-secondary);
	margin-top: 2px;
}

.title-page-properties__delete-button {
	padding: var(--spacing-sm) var(--spacing-md);
	background: #dc3545;
	color: white;
	border: none;
	border-radius: var(--radius-sm);
	font-size: 14px;
	cursor: pointer;
	transition: background 0.2s ease;
}

.title-page-properties__delete-button:hover {
	background: #c82333;
}
</style>
