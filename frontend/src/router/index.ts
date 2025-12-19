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

router.beforeEach(async (to, _from, next) => {
	const authStore = useAuthStore();

	if (authStore.isAuth) {
		await authStore.checkAuth();
	}

	if (to.meta.requiresAuth && !authStore.isAuth) {
		next('/auth');
		return;
	}

	if (to.path === '/auth' && authStore.isAuth) {
		next('/dashboard');
		return;
	}

	next();
});

export default router;
