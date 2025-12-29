<template>
	<button
		class="theme-toggle"
		:class="{ 'theme-toggle--dark': isDark }"
		@click="toggleTheme"
		:aria-label="isDark ? 'Переключить на светлую тему' : 'Переключить на тёмную тему'"
	>
		<Icon :name="isDark ? 'moon' : 'sun'" size="20" class="theme-toggle__icon" />
	</button>
</template>

<script setup lang="ts">
import { useTheme } from '@/app/composables/useTheme';
import Icon from '@/components/Icon.vue';

const { isDark, toggleTheme } = useTheme();
</script>

<style scoped>
.theme-toggle {
	width: 40px;
	height: 40px;
	border-radius: var(--radius-md);
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	cursor: pointer;
	display: flex;
	align-items: center;
	justify-content: center;
	transition: all 0.2s ease;
	font-size: 20px;
}

.theme-toggle:hover {
	background: var(--bg-tertiary);
	border-color: var(--border-hover);
}

.theme-toggle__icon {
	display: block;
	line-height: 1;
}
</style>
