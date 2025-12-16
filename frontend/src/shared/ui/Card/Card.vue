<template>
	<div :class="['card', className]">
		<slot />
	</div>
</template>

<script setup lang="ts">
interface Props {
	className?: string;
}

withDefaults(defineProps<Props>(), {
	className: '',
});
</script>

<style scoped>
.card {
	background: rgba(24, 24, 27, 0.8);
	backdrop-filter: blur(20px);
	-webkit-backdrop-filter: blur(20px);
	border-radius: 20px;
	border: 1px solid rgba(63, 63, 70, 0.5);
	padding: 32px;
	box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4),
		inset 0 1px 0 rgba(255, 255, 255, 0.05);
	transition: transform 0.3s ease, box-shadow 0.3s ease;
	animation: fadeIn 0.5s ease;
}

.card:hover {
	box-shadow: 0 12px 40px rgba(0, 0, 0, 0.5),
		inset 0 1px 0 rgba(255, 255, 255, 0.05);
}

@keyframes fadeIn {
	from {
		opacity: 0;
		transform: translateY(20px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}
</style>
