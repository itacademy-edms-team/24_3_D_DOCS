<template>
	<header class="content-header">
		<h1 class="page-title">{{ title }}</h1>
		<button class="new-btn" @click="$emit('new-project')">
			{{ buttonText }}
		</button>
	</header>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { TabType } from '../useMainPage';

interface Props {
	title: string;
	activeTab: TabType;
}

const props = defineProps<Props>();

defineEmits<{
	'new-project': [];
}>();

const buttonText = computed(() => {
	if (props.activeTab === 'profiles') return 'Новый шаблон';
	if (props.activeTab === 'title-pages') return 'Новый титульный лист';
	return 'Новый документ';
});
</script>

<style scoped>
.content-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 2rem;
	border-bottom: 1px solid #27272a;
}

.page-title {
	font-size: 28px;
	font-weight: 700;
	color: #e4e4e7;
	margin: 0;
}

.new-btn {
	padding: 0.75rem 1.5rem;
	background: #6366f1;
	color: white;
	border: none;
	border-radius: 8px;
	font-size: 14px;
	font-weight: 600;
	cursor: pointer;
	transition: background 0.2s;
}

.new-btn:hover {
	background: #818cf8;
}
</style>
