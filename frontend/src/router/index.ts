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
		{
			path: '/document/:id',
			name: 'editor',
			component: () => import('@/pages/editor/ContentEditor.vue'),
			meta: { requiresAuth: true },
		},
		{
			path: '/profile/:id',
			name: 'profile-editor',
			component: () => import('@/pages/styles/StylesProfile.vue'),
			meta: { requiresAuth: true },
		},
		{
			path: '/title-page/:id',
			name: 'title-page-editor',
			component: () => import('@/pages/title-page/TitlePageEditor.vue'),
			meta: { requiresAuth: true },
		},
		{
			path: '/attachments',
			name: 'attachments',
			component: () => import('@/pages/attachments/AttachmentsPage.vue'),
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
		} catch (error) {
			// Токен невалиден - checkAuth уже попытался обновить его через refreshAccessToken
			// Если refresh тоже не удался, данные уже очищены
			// Проверяем isAuth еще раз после попытки обновления
			if (!authStore.isAuth && to.meta.requiresAuth) {
				next('/auth');
				return;
			}
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
