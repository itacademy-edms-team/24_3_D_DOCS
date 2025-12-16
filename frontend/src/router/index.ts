import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/entities/auth/store/authStore';
import AuthPage from '@/pages/auth/AuthPage.vue';
import MainPage from '@/pages/main/MainPage.vue';

const router = createRouter({
	history: createWebHistory(),
	routes: [
		{
			path: '/auth',
			name: 'auth',
			component: AuthPage,
		},
		{
			path: '/',
			redirect: '/dashboard',
		},
		{
			path: '/dashboard',
			name: 'dashboard',
			component: MainPage,
			meta: { requiresAuth: true },
		},
	],
});

router.beforeEach(async (to, from, next) => {
	const authStore = useAuthStore();

	// Если есть токен, проверяем его валидность
	if (authStore.isAuth) {
		try {
			await authStore.checkAuth();
		} catch {
			// Токен невалиден, но не редиректим сразу - может быть это не защищенный маршрут
		}
	}

	if (to.meta.requiresAuth) {
		if (!authStore.isAuth) {
			next('/auth');
			return;
		}
	}

	if (to.path === '/auth' && authStore.isAuth) {
		next('/dashboard');
		return;
	}

	next();
});

export default router;
