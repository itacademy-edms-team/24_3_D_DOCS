<template>
	<div class="settings-page">
		<header class="settings-header content-header">
			<button class="settings-back" @click="router.push('/dashboard')">
				<Icon name="arrow_back" size="18" ariaLabel="Назад" />
				<span>Назад</span>
			</button>
			<h1 class="page-title">Настройки</h1>
		</header>

		<main class="settings-body content-body">
			<ul class="settings-list">
				<li>
					<Button variant="secondary" fullWidth disabled class="settings-btn">
						Профиль
					</Button>
				</li>
				<li>
					<Button variant="secondary" fullWidth disabled class="settings-btn">
						Уведомления
					</Button>
				</li>
				<li>
					<Button variant="secondary" fullWidth disabled class="settings-btn">
						Безопасность
					</Button>
				</li>
				<li>
					<Button
						variant="secondary"
						fullWidth
						class="settings-btn"
						@click="router.push('/settings/llm')"
					>
						Настройки LLM
					</Button>
				</li>
			</ul>
		</main>
	</div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import Icon from '@/components/Icon.vue';
import Button from '@/shared/ui/Button/Button.vue';

const router = useRouter();
</script>

<style scoped>
.settings-page {
	min-height: 100vh;
	background: var(--bg-primary);
	color: var(--text-primary);
	display: flex;
	flex-direction: column;
}

.settings-header {
	display: flex;
	align-items: center;
	gap: var(--spacing-md);
	padding: var(--spacing-xl) var(--spacing-xl) var(--spacing-md);
	border-bottom: 1px solid var(--border-color);
}

.settings-back {
	display: flex;
	align-items: center;
	gap: var(--spacing-sm);
	padding: 0.6rem 1rem;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	cursor: pointer;
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
	font-family: inherit;
	transition: background 0.2s, border-color 0.2s;
}

.settings-back:hover {
	background: var(--bg-tertiary);
	border-color: var(--border-hover);
}

.page-title {
	margin: 0;
	font-size: 28px;
	font-weight: 700;
	color: var(--text-primary);
}

.settings-body {
	flex: 1;
	display: flex;
	justify-content: center;
	align-items: flex-start;
	padding: var(--spacing-xl);
}

.settings-list {
	list-style: none;
	padding: 0;
	margin: 0;
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
	width: 100%;
	max-width: 480px;
}

.settings-btn {
	padding: 1rem 1.5rem;
	font-size: 16px;
	font-weight: 600;
	justify-content: flex-start;
}
</style>
