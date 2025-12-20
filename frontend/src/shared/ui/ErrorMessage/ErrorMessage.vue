<template>
	<div v-if="message" class="error-message">{{ message }}</div>
</template>

<script setup lang="ts">
interface Props {
	message?: string | null;
}

withDefaults(defineProps<Props>(), {
	message: null,
});
</script>

<style scoped>
.error-message {
	padding: 12px;
	background: rgba(239, 68, 68, 0.1);
	border: 1px solid rgba(239, 68, 68, 0.2);
	border-radius: 8px;
	color: #f87171;
	font-size: 13px;
	text-align: center;
	animation: shake 0.3s ease;
}

@keyframes shake {
	0%,
	100% {
		transform: translateX(0);
	}
	25% {
		transform: translateX(-4px);
	}
	75% {
		transform: translateX(4px);
	}
}
</style>
