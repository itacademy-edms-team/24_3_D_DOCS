<template>
	<div class="color-picker">
		<label v-if="label" class="color-picker__label">{{ label }}</label>
		<div class="color-picker__container">
			<input
				type="color"
				:value="modelValue || '#000000'"
				@input="handleColorInput"
				class="color-picker__input"
				:disabled="disabled"
			/>
			<input
				type="text"
				:value="modelValue || '#000000'"
				@input="handleTextInput"
				class="color-picker__text-input"
				:disabled="disabled"
				placeholder="#000000"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
interface Props {
	modelValue?: string;
	label?: string;
	disabled?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	disabled: false,
});

const emit = defineEmits<{
	'update:modelValue': [value: string];
}>();

const handleColorInput = (e: Event) => {
	const target = e.target as HTMLInputElement;
	emit('update:modelValue', target.value);
};

const handleTextInput = (e: Event) => {
	const target = e.target as HTMLInputElement;
	const value = target.value.trim();
	// Простая валидация hex цвета
	if (/^#[0-9A-Fa-f]{6}$/.test(value)) {
		emit('update:modelValue', value);
	}
};
</script>

<style scoped>
.color-picker {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-sm);
}

.color-picker__label {
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
}

.color-picker__container {
	display: flex;
	align-items: center;
	gap: var(--spacing-sm);
}

.color-picker__input {
	width: 50px;
	height: 40px;
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	cursor: pointer;
	-webkit-appearance: none;
	appearance: none;
	background: none;
	padding: 0;
}

.color-picker__input::-webkit-color-swatch-wrapper {
	padding: 0;
}

.color-picker__input::-webkit-color-swatch {
	border: none;
	border-radius: var(--radius-sm);
}

.color-picker__input:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.color-picker__text-input {
	flex: 1;
	padding: var(--spacing-xs) var(--spacing-sm);
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	font-size: 14px;
	font-family: monospace;
	outline: none;
	transition: border-color 0.2s ease;
}

.color-picker__text-input:focus {
	border-color: var(--accent);
}

.color-picker__text-input:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}
</style>
