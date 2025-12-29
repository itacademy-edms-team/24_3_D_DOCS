<template>
	<div class="zoom-dropdown" ref="dropdownRef">
		<div class="zoom-dropdown__trigger" @click="toggle" @dblclick="startEditing">
			<input
				v-if="isEditing"
				ref="inputRef"
				v-model="inputValue"
				@blur="handleBlur"
				@keydown.enter="handleEnter"
				@keydown.esc="handleEsc"
				@input="handleInput"
				class="zoom-dropdown__input"
				type="text"
				placeholder="%"
			/>
			<span v-else class="zoom-dropdown__label">{{ displayValue }}</span>
			<span class="zoom-dropdown__arrow">▼</span>
		</div>
		<Transition name="dropdown">
			<div v-if="isOpen" class="zoom-dropdown__menu">
				<button
					v-for="option in options"
					:key="option.value"
					:class="['zoom-dropdown__item', { 'zoom-dropdown__item--active': modelValue === option.value }]"
					@click="selectOption(option.value)"
				>
					{{ option.label }}
				</button>
				<div class="zoom-dropdown__divider"></div>
				<button
					class="zoom-dropdown__item zoom-dropdown__item--custom"
					@click="startEditing"
				>
					Ввести значение...
				</button>
			</div>
		</Transition>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from 'vue';
import { onClickOutside } from '@vueuse/core';

interface Option {
	value: number;
	label: string;
}

interface Props {
	modelValue: number;
	options: Option[];
	min?: number;
	max?: number;
}

const props = withDefaults(defineProps<Props>(), {
	min: 10,
	max: 500,
});

const emit = defineEmits<{
	'update:modelValue': [value: number];
}>();

const isOpen = ref(false);
const isEditing = ref(false);
const inputValue = ref('');
const dropdownRef = ref<HTMLElement>();
const inputRef = ref<HTMLInputElement>();

const displayValue = computed(() => {
	return `${props.modelValue}%`;
});

const toggle = () => {
	if (!isEditing.value) {
		isOpen.value = !isOpen.value;
	}
};

const selectOption = (value: number) => {
	emit('update:modelValue', value);
	isOpen.value = false;
};

const startEditing = () => {
	isEditing.value = true;
	inputValue.value = props.modelValue.toString();
	isOpen.value = false;
	nextTick(() => {
		inputRef.value?.focus();
		inputRef.value?.select();
	});
};

const handleInput = (e: Event) => {
	const target = e.target as HTMLInputElement;
	// Фильтруем только цифры
	const filtered = target.value.replace(/[^\d]/g, '');
	inputValue.value = filtered;
};

const handleBlur = () => {
	applyValue();
};

const handleEnter = () => {
	applyValue();
	inputRef.value?.blur();
};

const handleEsc = () => {
	isEditing.value = false;
	inputValue.value = '';
};

const applyValue = () => {
	if (!inputValue.value) {
		isEditing.value = false;
		return;
	}

	const numValue = parseInt(inputValue.value, 10);
	if (isNaN(numValue)) {
		isEditing.value = false;
		return;
	}

	const clampedValue = Math.max(props.min, Math.min(props.max, numValue));
	emit('update:modelValue', clampedValue);
	isEditing.value = false;
	inputValue.value = '';
};

onClickOutside(dropdownRef, () => {
	isOpen.value = false;
});

watch(isEditing, (editing) => {
	if (!editing) {
		inputValue.value = '';
	}
});
</script>

<style scoped>
.zoom-dropdown {
	position: relative;
	display: inline-block;
	min-width: 100px;
}

.zoom-dropdown__trigger {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	padding: 6px 12px;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: 8px;
	color: var(--text-primary);
	font-size: 14px;
	cursor: pointer;
	transition: all 0.2s ease;
	min-width: 100px;
}

.zoom-dropdown__trigger:hover {
	border-color: var(--border-hover);
	background: var(--bg-tertiary);
}

.zoom-dropdown__label {
	flex: 1;
	text-align: left;
}

.zoom-dropdown__input {
	flex: 1;
	background: transparent;
	border: none;
	outline: none;
	color: var(--text-primary);
	font-size: 14px;
	font-family: inherit;
	width: 100%;
	padding: 0;
}

.zoom-dropdown__arrow {
	font-size: 10px;
	color: var(--text-secondary);
	transition: transform 0.2s ease;
	flex-shrink: 0;
}

.zoom-dropdown__menu {
	position: absolute;
	top: calc(100% + 4px);
	left: 0;
	right: 0;
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: 8px;
	box-shadow: var(--shadow-lg);
	z-index: 100;
	max-height: 300px;
	overflow-y: auto;
}

.zoom-dropdown__item {
	width: 100%;
	padding: 8px 12px;
	background: transparent;
	border: none;
	color: var(--text-primary);
	font-size: 14px;
	text-align: left;
	cursor: pointer;
	transition: background 0.2s ease;
}

.zoom-dropdown__item:hover {
	background: var(--bg-secondary);
}

.zoom-dropdown__item--active {
	background: var(--accent-light);
	color: var(--accent);
	font-weight: 500;
}

.zoom-dropdown__item--custom {
	font-style: italic;
	color: var(--text-secondary);
}

.zoom-dropdown__divider {
	height: 1px;
	background: var(--border-color);
	margin: 4px 0;
}

/* Transitions */
.dropdown-enter-active,
.dropdown-leave-active {
	transition: opacity 0.2s ease, transform 0.2s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
	opacity: 0;
	transform: translateY(-8px);
}
</style>
