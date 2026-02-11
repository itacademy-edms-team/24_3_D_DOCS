<template>
	<div :class="['container', { fullWidth }]">
		<label v-if="label" class="label">{{ label }}</label>
		<div class="inputWrapper">
			<input
				:type="inputType"
				:class="['input', { error: error }]"
				:value="modelValue"
				:autocomplete="autocomplete ?? undefined"
				@input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
				v-bind="filteredAttrs"
			/>
			<button
				v-if="showPasswordToggle && type === 'password'"
				type="button"
				class="passwordToggle"
				@click="togglePasswordVisibility"
				tabindex="-1"
				:aria-label="isPasswordVisible ? 'Скрыть' : 'Показать'"
			>
				<Icon
					:name="isPasswordVisible ? 'visibility_off' : 'visibility'"
					size="20"
					ariaLabel=""
				/>
			</button>
		</div>
		<span v-if="error" class="errorText">{{ error }}</span>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, useAttrs } from 'vue';
import Icon from '@/components/Icon.vue';

interface Props {
	label?: string;
	error?: string;
	fullWidth?: boolean;
	showPasswordToggle?: boolean;
	type?: string;
	modelValue: string;
	/** Предотвращает сохранение в менеджере паролей (для API-ключей) */
	autocomplete?: string;
}

const props = withDefaults(defineProps<Props>(), {
	fullWidth: false,
	showPasswordToggle: false,
	type: 'text',
	autocomplete: undefined,
});

defineEmits<{
	'update:modelValue': [value: string];
}>();

const attrs = useAttrs();
const filteredAttrs = computed(() => {
	const { autocomplete: _ac, ...rest } = attrs as Record<string, unknown>;
	return rest;
});

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
	border-color: var(--accent);
	background: #1c1c1f;
	box-shadow: 0 0 0 3px rgba(var(--accent-rgb), 0.1);
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
	font-size: 18px;
	color: #a5b4fc;
	padding: 4px;
	border-radius: 4px;
	transition: all 0.2s ease;
	display: flex;
	align-items: center;
	justify-content: center;
	width: 32px;
	height: 32px;
}

.passwordToggle:hover {
	background: rgba(var(--accent-rgb), 0.08);
	color: rgba(var(--accent-rgb), 0.9);
}

.passwordToggle:active {
	transform: translateY(-50%) scale(0.95);
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
