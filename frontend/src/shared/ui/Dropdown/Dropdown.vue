<template>
	<div class="dropdown" ref="dropdownRef">
		<button
			class="dropdown__trigger"
			:class="{ 'dropdown__trigger--open': isOpen }"
			@click="toggle"
			:disabled="disabled"
		>
			<slot name="trigger">
				<span>{{ selectedLabel || placeholder }}</span>
				<span class="dropdown__arrow">▼</span>
			</slot>
		</button>
		<Transition name="dropdown">
			<div v-if="isOpen" class="dropdown__menu">
				<slot>
					<button
						v-for="option in options"
						:key="option.value"
						:class="['dropdown__item', { 'dropdown__item--active': modelValue === option.value }]"
						@click="selectOption(option.value)"
					>
						{{ option.label }}
					</button>
				</slot>
			</div>
		</Transition>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { onClickOutside } from '@vueuse/core';

interface Option {
	value: string | number | undefined;
	label: string;
}

interface Props {
	modelValue?: string | number | undefined;
	options?: Option[];
	placeholder?: string;
	disabled?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	placeholder: 'Выберите...',
	disabled: false,
});

const emit = defineEmits<{
	'update:modelValue': [value: string | number | undefined];
}>();

const isOpen = ref(false);
const dropdownRef = ref<HTMLElement>();

const selectedLabel = computed(() => {
	if (!props.options) return null;
	// undefined является валидным значением, поэтому проверяем явно на null
	if (props.modelValue === null || props.modelValue === undefined) {
		const undefinedOption = props.options.find((opt) => opt.value === undefined);
		return undefinedOption?.label || null;
	}
	const option = props.options.find((opt) => opt.value === props.modelValue);
	return option?.label;
});

const toggle = () => {
	if (!props.disabled) {
		isOpen.value = !isOpen.value;
	}
};

const selectOption = (value: string | number | undefined) => {
	emit('update:modelValue', value);
	isOpen.value = false;
};

onClickOutside(dropdownRef, () => {
	isOpen.value = false;
});
</script>

<style scoped>
.dropdown {
	position: relative;
	display: inline-block;
}

.dropdown__trigger {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: var(--spacing-sm);
	padding: var(--spacing-sm) var(--spacing-md);
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	color: var(--text-primary);
	font-size: 14px;
	cursor: pointer;
	transition: all 0.2s ease;
	min-width: 150px;
}

.dropdown__trigger:hover:not(:disabled) {
	border-color: var(--border-hover);
	background: var(--bg-tertiary);
}

.dropdown__trigger--open {
	border-color: var(--accent);
}

.dropdown__trigger:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.dropdown__arrow {
	font-size: 10px;
	color: var(--text-secondary);
	transition: transform 0.2s ease;
}

.dropdown__trigger--open .dropdown__arrow {
	transform: rotate(180deg);
}

.dropdown__menu {
	position: absolute;
	top: calc(100% + 4px);
	left: 0;
	right: 0;
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	box-shadow: var(--shadow-lg);
	z-index: 100;
	max-height: 300px;
	overflow-y: auto;
}

.dropdown__item {
	width: 100%;
	padding: var(--spacing-sm) var(--spacing-md);
	background: transparent;
	border: none;
	color: var(--text-primary);
	font-size: 14px;
	text-align: left;
	cursor: pointer;
	transition: background 0.2s ease;
}

.dropdown__item:hover {
	background: var(--bg-secondary);
}

.dropdown__item--active {
	background: var(--accent-light);
	color: var(--accent);
	font-weight: 500;
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
