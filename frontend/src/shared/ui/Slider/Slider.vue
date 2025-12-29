<template>
	<div class="slider">
		<label v-if="label" class="slider__label">{{ label }}</label>
		<div class="slider__container">
			<input
				:id="inputId"
				type="range"
				:min="min"
				:max="max"
				:step="step"
				:value="modelValue"
				@input="handleInput"
				class="slider__input"
				:class="{ 'slider__input--disabled': disabled }"
				:disabled="disabled"
			/>
			<input
				v-if="showInput"
				type="number"
				:min="min"
				:max="max"
				:step="step"
				:value="modelValue"
				@input="handleNumberInput"
				class="slider__number-input"
				:disabled="disabled"
			/>
			<span v-if="unit" class="slider__unit">{{ unit }}</span>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

interface Props {
	modelValue: number;
	min?: number;
	max?: number;
	step?: number;
	label?: string;
	unit?: string;
	showInput?: boolean;
	disabled?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	min: 0,
	max: 100,
	step: 1,
	showInput: true,
	disabled: false,
});

const emit = defineEmits<{
	'update:modelValue': [value: number];
}>();

const inputId = computed(() => `slider-${Math.random().toString(36).substr(2, 9)}`);

const handleInput = (e: Event) => {
	const target = e.target as HTMLInputElement;
	emit('update:modelValue', parseFloat(target.value));
};

const handleNumberInput = (e: Event) => {
	const target = e.target as HTMLInputElement;
	const value = parseFloat(target.value);
	if (!isNaN(value) && value >= props.min && value <= props.max) {
		emit('update:modelValue', value);
	}
};
</script>

<style scoped>
.slider {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-sm);
}

.slider__label {
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
}

.slider__container {
	display: flex;
	align-items: center;
	gap: var(--spacing-sm);
}

.slider__input {
	flex: 1;
	height: 6px;
	border-radius: 3px;
	background: var(--bg-tertiary);
	outline: none;
	-webkit-appearance: none;
	appearance: none;
	cursor: pointer;
}

.slider__input::-webkit-slider-thumb {
	-webkit-appearance: none;
	appearance: none;
	width: 18px;
	height: 18px;
	border-radius: 50%;
	background: var(--accent);
	cursor: pointer;
	transition: all 0.2s ease;
}

.slider__input::-webkit-slider-thumb:hover {
	background: var(--accent-hover);
	transform: scale(1.1);
}

.slider__input::-moz-range-thumb {
	width: 18px;
	height: 18px;
	border-radius: 50%;
	background: var(--accent);
	cursor: pointer;
	border: none;
	transition: all 0.2s ease;
}

.slider__input::-moz-range-thumb:hover {
	background: var(--accent-hover);
	transform: scale(1.1);
}

.slider__input--disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.slider__input--disabled::-webkit-slider-thumb {
	cursor: not-allowed;
}

.slider__number-input {
	width: 80px;
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 14px;
	text-align: center;
	outline: none;
	transition: border-color 0.2s ease;
}

.slider__number-input:focus {
	border-color: var(--accent);
}

.slider__number-input:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.slider__unit {
	font-size: 14px;
	color: var(--text-secondary);
	min-width: 30px;
}
</style>
