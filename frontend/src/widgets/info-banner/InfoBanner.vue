<template>
	<div class="info-banner" v-if="!dismissed">
		<div class="info-banner__content">
			<span class="info-banner__icon">ℹ️</span>
			<div class="info-banner__text">
				<p class="info-banner__title">Оформляйте отчёты по ГОСТ автоматически</p>
				<p class="info-banner__description">
					Подключите премиум, чтобы хранить неограниченное число профилей стилей.
				</p>
			</div>
		</div>
		<div class="info-banner__actions">
			<button class="info-banner__link" @click="handleContact">
				Связаться с нами
			</button>
			<button class="info-banner__close" @click="handleDismiss" aria-label="Закрыть">
				×
			</button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useStorage } from '@vueuse/core';

const dismissed = useStorage('info-banner-dismissed', false);

const handleContact = () => {
	// TODO: Открыть форму связи или email
	console.log('Contact clicked');
};

const handleDismiss = () => {
	dismissed.value = true;
};
</script>

<style scoped>
.info-banner {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: var(--spacing-md) var(--spacing-lg);
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	margin-bottom: var(--spacing-lg);
}

.info-banner__content {
	display: flex;
	align-items: flex-start;
	gap: var(--spacing-md);
	flex: 1;
}

.info-banner__icon {
	font-size: 20px;
	flex-shrink: 0;
}

.info-banner__text {
	flex: 1;
}

.info-banner__title {
	font-size: 14px;
	font-weight: 600;
	color: var(--text-primary);
	margin: 0 0 4px 0;
}

.info-banner__description {
	font-size: 13px;
	color: var(--text-secondary);
	margin: 0;
}

.info-banner__actions {
	display: flex;
	align-items: center;
	gap: var(--spacing-md);
}

.info-banner__link {
	background: transparent;
	border: none;
	color: var(--accent);
	font-size: 13px;
	font-weight: 500;
	cursor: pointer;
	text-decoration: underline;
	transition: color 0.2s ease;
}

.info-banner__link:hover {
	color: var(--accent-hover);
}

.info-banner__close {
	width: 24px;
	height: 24px;
	background: transparent;
	border: none;
	color: var(--text-secondary);
	font-size: 20px;
	cursor: pointer;
	display: flex;
	align-items: center;
	justify-content: center;
	border-radius: var(--radius-sm);
	transition: all 0.2s ease;
	line-height: 1;
}

.info-banner__close:hover {
	background: var(--bg-tertiary);
	color: var(--text-primary);
}
</style>
