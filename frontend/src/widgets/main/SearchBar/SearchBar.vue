<template>
	<div class="search-section">
		<div class="search-wrapper">
			<span class="search-icon">🔍</span>
			<input
				v-model="searchValue"
				type="text"
				class="search-input"
				:placeholder="placeholder"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

interface Props {
	modelValue: string;
	placeholder?: string;
}

const props = withDefaults(defineProps<Props>(), {
	placeholder: 'Поиск...',
});

const emit = defineEmits<{
	'update:modelValue': [value: string];
}>();

const searchValue = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});
</script>

<style scoped>
.search-section {
	padding: 1rem 2rem;
	border-bottom: 1px solid #27272a;
}

.search-wrapper {
	position: relative;
	max-width: 500px;
}

.search-icon {
	position: absolute;
	left: 1rem;
	top: 50%;
	transform: translateY(-50%);
	font-size: 16px;
	color: #71717a;
}

.search-input {
	width: 100%;
	padding: 0.75rem 1rem 0.75rem 3rem;
	background: #18181b;
	border: 1px solid #27272a;
	border-radius: 8px;
	color: #e4e4e7;
	font-size: 14px;
	outline: none;
	transition: border-color 0.2s;
}

.search-input:focus {
	border-color: #6366f1;
}

.search-input::placeholder {
	color: #71717a;
}
</style>
