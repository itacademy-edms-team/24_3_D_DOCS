<template>
	<button
		:class="['button', variant, { fullWidth, loading: isLoading }]"
		:disabled="disabled || isLoading"
		v-bind="$attrs"
	>
		<span v-if="isLoading" class="spinner" />
		<slot v-else />
	</button>
</template>

<script setup lang="ts">
interface Props {
	variant?: 'primary' | 'secondary';
	isLoading?: boolean;
	fullWidth?: boolean;
	disabled?: boolean;
}

withDefaults(defineProps<Props>(), {
	variant: 'primary',
	isLoading: false,
	fullWidth: false,
	disabled: false,
});
</script>

<style scoped>
.button {
	padding: 12px 24px;
	font-size: 16px;
	font-weight: 600;
	border: none;
	border-radius: 12px;
	cursor: pointer;
	transition: all 0.3s ease;
	position: relative;
	overflow: hidden;
	font-family: inherit;
}

.button::before {
	content: '';
	position: absolute;
	top: 0;
	left: -100%;
	width: 100%;
	height: 100%;
	background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
	transition: left 0.5s ease;
}

.button:hover::before {
	left: 100%;
}

.button:disabled {
	cursor: not-allowed;
	opacity: 0.6;
}

.primary {
	background: #6366f1;
	color: white;
	box-shadow: 0 4px 15px rgba(99, 102, 241, 0.3);
}

.primary:hover:not(:disabled) {
	background: #4f46e5;
	transform: translateY(-2px);
	box-shadow: 0 6px 20px rgba(99, 102, 241, 0.5);
}

.primary:active:not(:disabled) {
	transform: translateY(0);
	background: #4338ca;
}

.secondary {
	background: rgba(99, 102, 241, 0.1);
	color: #a5b4fc;
	border: 2px solid #3f3f46;
	backdrop-filter: blur(10px);
}

.secondary:hover:not(:disabled) {
	background: rgba(99, 102, 241, 0.15);
	border-color: #52525b;
	transform: translateY(-2px);
}

.fullWidth {
	width: 100%;
}

.loading {
	color: transparent;
	pointer-events: none;
}

.spinner {
	position: absolute;
	top: 50%;
	left: 50%;
	transform: translate(-50%, -50%);
	width: 20px;
	height: 20px;
	border: 3px solid rgba(255, 255, 255, 0.3);
	border-top-color: white;
	border-radius: 50%;
	animation: spin 0.6s linear infinite;
}

@keyframes spin {
	to {
		transform: translate(-50%, -50%) rotate(360deg);
	}
}
</style>
