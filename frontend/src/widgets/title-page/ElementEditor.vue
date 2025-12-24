<template>
	<div class="element-editor">
		<div
			v-if="!element"
			class="empty-state"
		>
			<p>Выберите элемент для редактирования</p>
		</div>

		<div
			v-else
			class="editor-content"
		>
			<div class="header">
				<h3>{{ elementTypeLabel }}</h3>
				<button
					class="delete-btn"
					@click="handleDelete"
				>
					Удалить
				</button>
			</div>

			<!-- Position -->
			<div class="field">
				<label>Позиция (мм)</label>
				<div class="position-inputs">
					<input
						v-model.number="localX"
						type="number"
						step="0.1"
						placeholder="X"
						@blur="handlePositionBlur('x')"
					/>
					<input
						v-model.number="localY"
						type="number"
						step="0.1"
						placeholder="Y"
						@blur="handlePositionBlur('y')"
					/>
				</div>
			</div>

			<!-- Text/Variable specific -->
			<template v-if="element.type === 'text' || element.type === 'variable'">
				<div
					v-if="element.type === 'text'"
					class="field"
				>
					<label>Текст</label>
					<textarea
						v-model="localContent"
						placeholder="Введите текст"
						@input="handleChange({ content: localContent })"
					/>
				</div>

				<div
					v-if="element.type === 'variable'"
					class="field"
				>
					<label>Ключ переменной</label>
					<input
						v-model="localVariableKey"
						type="text"
						placeholder="например: university"
						@input="handleChange({ variableKey: localVariableKey })"
					/>
				</div>

				<div class="field">
					<label>Размер шрифта (pt)</label>
					<input
						v-model.number="localFontSize"
						type="number"
						min="8"
						max="72"
						@input="handleChange({ fontSize: localFontSize || 14 })"
						@blur="handleNumericBlur('fontSize', 14)"
					/>
				</div>

				<div class="field">
					<label>Шрифт</label>
					<select
						v-model="localFontFamily"
						@change="handleChange({ fontFamily: localFontFamily })"
					>
						<option value="">По умолчанию</option>
						<option
							v-for="font in FONT_FAMILIES"
							:key="font.value"
							:value="font.value"
						>
							{{ font.label }}
						</option>
					</select>
				</div>

				<div class="field">
					<label>Начертание</label>
					<select
						v-model="localFontWeight"
						@change="handleChange({ fontWeight: localFontWeight })"
					>
						<option value="normal">Обычный</option>
						<option value="bold">Жирный</option>
					</select>
				</div>

				<div class="field">
					<label>Стиль</label>
					<select
						v-model="localFontStyle"
						@change="handleChange({ fontStyle: localFontStyle })"
					>
						<option value="normal">Обычный</option>
						<option value="italic">Курсив</option>
					</select>
				</div>

				<div class="field">
					<label>Междустрочный интервал</label>
					<input
						v-model.number="localLineHeight"
						type="number"
						min="0.5"
						max="3"
						step="0.1"
						@input="handleChange({ lineHeight: localLineHeight || 1.2 })"
						@blur="handleNumericBlur('lineHeight', 1.2)"
					/>
					<small>Множитель (например, 1.2 = 120%)</small>
				</div>

				<div class="field">
					<label>Выравнивание</label>
					<select
						v-model="localTextAlign"
						@change="handleChange({ textAlign: localTextAlign })"
					>
						<option value="left">Слева</option>
						<option value="center">По центру</option>
						<option value="right">Справа</option>
					</select>
				</div>
			</template>

			<!-- Line specific -->
			<template v-if="element.type === 'line'">
				<div class="field">
					<label>Длина (мм)</label>
					<input
						v-model.number="localLength"
						type="number"
						min="1"
						@input="handleChange({ length: localLength || 100 })"
						@blur="handleNumericBlur('length', 100)"
					/>
				</div>

				<div class="field">
					<label>Толщина (мм)</label>
					<input
						v-model.number="localThickness"
						type="number"
						min="0.1"
						step="0.1"
						@input="handleChange({ thickness: localThickness || 1 })"
						@blur="handleNumericBlur('thickness', 1)"
					/>
				</div>
			</template>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import type { TitlePageElement } from '@/shared/types/titlePage';
import { FONT_FAMILIES } from '@/entities/profile/constants';

interface Props {
	element: TitlePageElement | null;
}

interface Emits {
	(e: 'update', element: TitlePageElement): void;
	(e: 'delete'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const elementTypeLabel = computed(() => {
	if (!props.element) return '';
	if (props.element.type === 'text') return 'Текст';
	if (props.element.type === 'variable') return 'Переменная';
	if (props.element.type === 'line') return 'Линия';
	return '';
});

const localX = ref(0);
const localY = ref(0);
const localContent = ref('');
const localVariableKey = ref('');
const localFontSize = ref(14);
const localFontFamily = ref('');
const localFontWeight = ref<'normal' | 'bold'>('normal');
const localFontStyle = ref<'normal' | 'italic'>('normal');
const localLineHeight = ref(1.2);
const localTextAlign = ref<'left' | 'center' | 'right'>('left');
const localLength = ref(100);
const localThickness = ref(1);

function syncFromElement() {
	if (!props.element) return;

	localX.value = props.element.x ?? 0;
	localY.value = props.element.y ?? 0;
	localContent.value = props.element.content || '';
	localVariableKey.value = props.element.variableKey || '';
	localFontSize.value = props.element.fontSize ?? 14;
	localFontFamily.value = props.element.fontFamily || '';
	localFontWeight.value = props.element.fontWeight || 'normal';
	localFontStyle.value = props.element.fontStyle || 'normal';
	localLineHeight.value = props.element.lineHeight ?? 1.2;
	localTextAlign.value = props.element.textAlign || 'left';
	localLength.value = props.element.length ?? 100;
	localThickness.value = props.element.thickness ?? 1;
}

watch(
	() => props.element,
	() => {
		syncFromElement();
	},
	{ immediate: true, deep: true }
);

function handleChange(updates: Partial<TitlePageElement>) {
	if (!props.element) return;
	const updated = { ...props.element, ...updates };
	emit('update', updated);
}

function handlePositionBlur(field: 'x' | 'y') {
	if (!props.element) return;
	const value = field === 'x' ? localX.value : localY.value;
	const rounded = parseFloat(value.toFixed(1));
	handleChange({ [field]: rounded });
}

function handleNumericBlur(
	field: 'fontSize' | 'lineHeight' | 'length' | 'thickness',
	defaultValue: number
) {
	if (!props.element) return;
	const value =
		field === 'fontSize'
			? localFontSize.value
			: field === 'lineHeight'
				? localLineHeight.value
				: field === 'length'
					? localLength.value
					: localThickness.value;

	if (isNaN(value) || value === undefined || value === null) {
		handleChange({ [field]: defaultValue });
	} else {
		handleChange({ [field]: value });
	}
}

function handleDelete() {
	emit('delete');
}
</script>

<style scoped>
.element-editor {
	padding: 1rem;
	border: 1px solid #ddd;
	border-radius: 4px;
}

.empty-state {
	color: #666;
}

.editor-content {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 1rem;
}

.header h3 {
	margin: 0;
	font-size: 1rem;
	font-weight: 600;
}

.delete-btn {
	padding: 0.25rem 0.5rem;
	background: #dc3545;
	color: #fff;
	border: none;
	border-radius: 4px;
	cursor: pointer;
	font-size: 0.875rem;
}

.field {
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
}

.field label {
	font-size: 0.875rem;
}

.field input,
.field textarea,
.field select {
	padding: 0.25rem;
	width: 100%;
}

.field textarea {
	min-height: 60px;
}

.position-inputs {
	display: flex;
	gap: 0.5rem;
}

.position-inputs input {
	width: 100px;
}

.field small {
	color: #666;
	font-size: 0.75rem;
}
</style>
