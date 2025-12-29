<template>
	<div class="tabs">
		<div class="tabs__header" role="tablist">
			<button
				v-for="tab in tabs"
				:key="tab.value"
				:class="['tabs__tab', { 'tabs__tab--active': modelValue === tab.value }]"
				@click="handleTabClick(tab.value)"
				:aria-selected="modelValue === tab.value"
				role="tab"
			>
				{{ tab.label }}
			</button>
		</div>
		<div class="tabs__content">
			<slot :activeTab="modelValue" />
		</div>
	</div>
</template>

<script setup lang="ts">
interface Tab {
	value: string;
	label: string;
}

interface Props {
	modelValue: string;
	tabs: Tab[];
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: string];
}>();

const handleTabClick = (value: string) => {
	emit('update:modelValue', value);
};
</script>

<style scoped>
.tabs {
	display: flex;
	flex-direction: column;
}

.tabs__header {
	display: flex;
	gap: var(--spacing-xs);
	border-bottom: 2px solid var(--border-color);
	margin-bottom: var(--spacing-lg);
}

.tabs__tab {
	padding: var(--spacing-sm) var(--spacing-md);
	background: transparent;
	border: none;
	border-bottom: 2px solid transparent;
	color: var(--text-secondary);
	font-size: 14px;
	font-weight: 500;
	cursor: pointer;
	transition: all 0.2s ease;
	margin-bottom: -2px;
}

.tabs__tab:hover {
	color: var(--text-primary);
}

.tabs__tab--active {
	color: var(--accent);
	border-bottom-color: var(--accent);
}

.tabs__content {
	flex: 1;
}
</style>
