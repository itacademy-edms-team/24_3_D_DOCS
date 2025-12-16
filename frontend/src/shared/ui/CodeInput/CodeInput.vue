<template>
	<div class="container">
		<input
			v-for="(digit, index) in digits"
			:key="index"
			:ref="(el) => setInputRef(el, index)"
			type="text"
			inputmode="numeric"
			maxlength="1"
			:value="digit"
			@input="handleChange(index, ($event.target as HTMLInputElement).value)"
			@keydown="handleKeyDown(index, $event)"
			@focus="focused = index"
			@blur="focused = -1"
			@paste="handlePaste"
			:class="['input', { focused: focused === index, filled: digit }]"
			:aria-label="`Digit ${index + 1}`"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';

interface Props {
	length?: number;
	modelValue: string;
	autoFocus?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	length: 6,
	autoFocus: false,
});

const emit = defineEmits<{
	'update:modelValue': [value: string];
	complete: [value: string];
}>();

const focused = ref(-1);
const inputRefs = ref<(HTMLInputElement | null)[]>([]);

const digits = ref<string[]>([]);

function setInputRef(el: any, index: number) {
	if (el) {
		inputRefs.value[index] = el;
	}
}

function updateDigits(value: string) {
	const newDigits = value.split('').slice(0, props.length);
	while (newDigits.length < props.length) {
		newDigits.push('');
	}
	digits.value = newDigits;
}

watch(
	() => props.modelValue,
	(newValue) => {
		updateDigits(newValue);
	},
	{ immediate: true },
);

onMounted(() => {
	if (props.autoFocus && inputRefs.value[0]) {
		inputRefs.value[0].focus();
	}
});

function handleChange(index: number, inputValue: string) {
	const digit = inputValue.replace(/\D/g, '').slice(-1);

	const newDigits = [...digits.value];
	newDigits[index] = digit;
	const newValue = newDigits.join('');

	emit('update:modelValue', newValue);

	if (digit && index < props.length - 1) {
		inputRefs.value[index + 1]?.focus();
	}

	if (newValue.length === props.length) {
		emit('complete', newValue);
	}
}

function handleKeyDown(index: number, e: KeyboardEvent) {
	if (e.key === 'Backspace') {
		if (!digits.value[index] && index > 0) {
			inputRefs.value[index - 1]?.focus();
		}
	} else if (e.key === 'ArrowLeft' && index > 0) {
		e.preventDefault();
		inputRefs.value[index - 1]?.focus();
	} else if (e.key === 'ArrowRight' && index < props.length - 1) {
		e.preventDefault();
		inputRefs.value[index + 1]?.focus();
	}
}

function handlePaste(e: ClipboardEvent) {
	e.preventDefault();
	const pastedData = e.clipboardData
		?.getData('text')
		.replace(/\D/g, '')
		.slice(0, props.length) || '';
	emit('update:modelValue', pastedData);

	if (pastedData.length === props.length) {
		inputRefs.value[props.length - 1]?.focus();
		emit('complete', pastedData);
	} else if (pastedData.length > 0) {
		inputRefs.value[Math.min(pastedData.length, props.length - 1)]?.focus();
	}
}
</script>

<style scoped>
.container {
	display: flex;
	gap: 12px;
	justify-content: center;
	align-items: center;
}

.input {
	width: 48px;
	height: 56px;
	font-size: 24px;
	font-weight: 600;
	text-align: center;
	border: 2px solid #27272a;
	border-radius: 12px;
	background: #18181b;
	color: #e4e4e7;
	transition: all 0.2s ease;
	caret-color: #6366f1;
	outline: none;
}

.input:hover {
	border-color: #3f3f46;
}

.input.focused {
	border-color: #6366f1;
	background: #1c1c1f;
	box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
	transform: scale(1.05);
}

.input.filled {
	border-color: #6366f1;
	background: #1c1c1f;
}

.input::placeholder {
	color: #52525b;
}

.input.filled {
	animation: pulse 0.3s ease;
}

@keyframes pulse {
	0% {
		transform: scale(1);
	}
	50% {
		transform: scale(1.08);
	}
	100% {
		transform: scale(1);
	}
}
</style>
