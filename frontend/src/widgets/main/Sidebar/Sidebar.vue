<template>
	<aside class="sidebar">
		<div class="sidebar-content">
			<div class="logo">
				<div class="logo-icon">📄</div>
				<h1 class="logo-text">DDOCS</h1>
			</div>

			<nav class="nav-section">
				<button
					v-for="tab in tabs"
					:key="tab.id"
					class="nav-item"
					:class="{ active: activeTab === tab.id }"
					@click="$emit('tab-change', tab.id)"
				>
					<span class="nav-icon">{{ tab.icon }}</span>
					{{ tab.label }}
				</button>
			</nav>

			<UserSection @logout="$emit('logout')" @settings="$emit('settings')" />
		</div>
	</aside>
</template>

<script setup lang="ts">
import UserSection from './UserSection.vue';
import type { TabType } from '../useMainPage';

interface Props {
	activeTab: TabType;
}

defineProps<Props>();

defineEmits<{
	'tab-change': [tab: TabType];
	logout: [];
	settings: [];
}>();

const tabs = [
	{ id: 'docs' as TabType, label: 'Документы', icon: '📄' },
	{ id: 'profiles' as TabType, label: 'Шаблоны', icon: '🎨' },
	{ id: 'title-pages' as TabType, label: 'Титульные листы', icon: '📋' },
];
</script>

<style scoped>
.sidebar {
	width: 280px;
	background: #18181b;
	border-right: 1px solid #27272a;
	display: flex;
	flex-direction: column;
	overflow-y: auto;
}

.sidebar-content {
	display: flex;
	flex-direction: column;
	height: 100%;
	padding: 1.5rem;
	gap: 2rem;
}

.logo {
	display: flex;
	align-items: center;
	gap: 12px;
	margin-bottom: 0;
}

.logo-icon {
	font-size: 32px;
	filter: drop-shadow(0 0 20px rgba(99, 102, 241, 0.5));
}

.logo-text {
	font-size: 24px;
	font-weight: 700;
	background: linear-gradient(135deg, #6366f1 0%, #a855f7 100%);
	-webkit-background-clip: text;
	-webkit-text-fill-color: transparent;
	background-clip: text;
	letter-spacing: -0.02em;
	margin: 0;
}

.nav-section {
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
	flex: 1;
}

.nav-item {
	width: 100%;
	padding: 0.75rem 1rem;
	background: transparent;
	border: none;
	border-radius: 6px;
	color: #a1a1aa;
	font-size: 14px;
	text-align: left;
	cursor: pointer;
	display: flex;
	align-items: center;
	gap: 0.75rem;
	transition: all 0.2s;
}

.nav-item:hover {
	background: #27272a;
	color: #e4e4e7;
}

.nav-item.active {
	background: rgba(99, 102, 241, 0.2);
	color: #6366f1;
}

.nav-icon {
	font-size: 18px;
}
</style>
