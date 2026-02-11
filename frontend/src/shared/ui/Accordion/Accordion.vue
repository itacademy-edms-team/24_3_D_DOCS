<template>
	<div class="accordion">
		<button
			class="accordion__trigger"
			:aria-expanded="isOpen"
			@click="toggle"
			type="button"
		>
			<span class="accordion__title">{{ title }}</span>
			<span class="accordion__chevron" :class="{ 'accordion__chevron--open': isOpen }">▼</span>
		</button>
		<Transition name="accordion">
			<div v-show="isOpen" class="accordion__content">
				<slot />
			</div>
		</Transition>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';

interface Props {
	title: string;
	defaultOpen?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	defaultOpen: false,
});

const isOpen = ref(props.defaultOpen);

function toggle() {
	isOpen.value = !isOpen.value;
}

onMounted(() => {
	isOpen.value = props.defaultOpen;
});
</script>

<style scoped>
.accordion {
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	background: var(--bg-secondary);
	overflow: hidden;
}

.accordion__trigger {
	width: 100%;
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: var(--spacing-md) var(--spacing-lg);
	font-size: 16px;
	font-weight: 600;
	text-align: left;
	background: transparent;
	border: none;
	cursor: pointer;
	color: var(--text-primary);
	font-family: inherit;
	transition: background 0.2s;
}

.accordion__trigger:hover {
	background: var(--bg-tertiary);
}

.accordion__title {
	flex: 1;
}

.accordion__chevron {
	font-size: 12px;
	transition: transform 0.2s;
}

.accordion__chevron--open {
	transform: rotate(180deg);
}

.accordion__content {
	padding: var(--spacing-md) var(--spacing-lg);
	border-top: 1px solid var(--border-color);
}

.accordion-enter-active,
.accordion-leave-active {
	transition: opacity 0.2s ease;
}
.accordion-enter-from,
.accordion-leave-to {
	opacity: 0;
}
.accordion-enter-to,
.accordion-leave-from {
	opacity: 1;
}
</style>
