<template>
	<RouterView />
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { RouterView } from 'vue-router';
import { useAuthStore } from '@/entities/auth/store/authStore';

const authStore = useAuthStore();

onMounted(async () => {
	// Инициализируем авторизацию при загрузке приложения
	// Данные пользователя должны быть в localStorage благодаря persist
	if (authStore.isAuth) {
		try {
			await authStore.checkAuth();
		} catch {
			// Если токен невалиден, данные уже очищены в checkAuth/refreshAccessToken
		}
	}
});
</script>
