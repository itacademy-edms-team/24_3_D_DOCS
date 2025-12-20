<template>
	<div :class="['container', { fullWidth }]">
		<label v-if="label" class="label">{{ label }}</label>
		<div class="inputWrapper">
			<input
				:type="inputType"
				:class="['input', { error: error }]"
				:value="modelValue"
				@input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
				v-bind="$attrs"
			/>
			<button
				v-if="showPasswordToggle && type === 'password'"
				type="button"
				class="passwordToggle"
				@click="togglePasswordVisibility"
				tabindex="-1"
				:aria-label="isPasswordVisible ? 'Скрыть пароль' : 'Показать пароль'"
			>
				<svg
					v-if="isPasswordVisible"
					class="eye-icon"
					viewBox="0 0 24 24"
					fill="none"
					stroke="currentColor"
					stroke-width="2"
					stroke-linecap="round"
					stroke-linejoin="round"
				>
					<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
					<circle cx="12" cy="12" r="3" />
				</svg>
				<svg
					v-else
					class="eye-icon"
					viewBox="0 0 24 24"
					fill="none"
					stroke="currentColor"
					stroke-width="2"
					stroke-linecap="round"
					stroke-linejoin="round"
				>
					<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
					<line x1="1" y1="1" x2="23" y2="23" />
				</svg>
			</button>
		</div>
		<span v-if="error" class="errorText">{{ error }}</span>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';

interface Props {
	label?: string;
	error?: string;
	fullWidth?: boolean;
	showPasswordToggle?: boolean;
	type?: string;
	modelValue: string;
}

const props = withDefaults(defineProps<Props>(), {
	fullWidth: false,
	showPasswordToggle: false,
	type: 'text',
});

defineEmits<{
	'update:modelValue': [value: string];
}>();

const isPasswordVisible = ref(false);

const inputType = computed(() => {
	if (props.showPasswordToggle && props.type === 'password') {
		return isPasswordVisible.value ? 'text' : 'password';
	}
	return props.type;
});

function togglePasswordVisibility() {
	isPasswordVisible.value = !isPasswordVisible.value;
}
</script>

<style scoped>
.container {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.fullWidth {
	width: 100%;
}

.label {
	font-size: 14px;
	font-weight: 500;
	color: #a5b4fc;
	margin-left: 4px;
}

.inputWrapper {
	position: relative;
	display: flex;
	align-items: center;
}

.input {
	padding: 12px 16px;
	font-size: 16px;
	border: 2px solid #27272a;
	border-radius: 12px;
	background: #18181b;
	backdrop-filter: blur(10px);
	color: #e4e4e7;
	transition: all 0.3s ease;
	font-family: inherit;
	width: 100%;
}

.inputWrapper .input {
	padding-right: 48px;
}

.input:hover {
	border-color: #3f3f46;
}

.input:focus {
	outline: none;
	border-color: #6366f1;
	background: #1c1c1f;
	box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
}

.input::placeholder {
	color: #52525b;
}

.input.error {
	border-color: #ef4444;
	background: rgba(239, 68, 68, 0.05);
}

.input.error:focus {
	border-color: #ef4444;
	box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
}

.errorText {
	font-size: 12px;
	color: #f87171;
	margin-left: 4px;
	animation: slideDown 0.3s ease;
}

.passwordToggle {
	position: absolute;
	right: 12px;
	top: 50%;
	transform: translateY(-50%);
	background: none;
	border: none;
	cursor: pointer;
	color: #71717a;
	padding: 4px;
	border-radius: 4px;
	transition: all 0.2s ease;
	display: flex;
	align-items: center;
	justify-content: center;
	width: 24px;
	height: 24px;
}

.passwordToggle:hover {
	color: #a1a1aa;
}

.passwordToggle:active {
	transform: translateY(-50%) scale(0.95);
}

.eye-icon {
	width: 20px;
	height: 20px;
}

@keyframes slideDown {
	from {
		opacity: 0;
		transform: translateY(-4px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}
</style>
