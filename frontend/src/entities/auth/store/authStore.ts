import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import AuthAPI from '../api/AuthAPI';
import type {
	User,
	LoginRequest,
	SendVerificationRequest,
	VerifyEmailRequest,
} from '../types';

export const useAuthStore = defineStore(
	'auth',
	() => {
		const user = ref<User | null>(null);
		const accessToken = ref<string | null>(null);
		const refreshToken = ref<string | null>(null);
		const isLoading = ref(false);
		const error = ref<string | null>(null);

		const isAuth = computed(() => !!accessToken.value);

		function setAuthData(data: {
			user: User;
			accessToken: string;
			refreshToken: string;
		}) {
			user.value = data.user;
			accessToken.value = data.accessToken;
			refreshToken.value = data.refreshToken;
		}

		function clearAuthData() {
			user.value = null;
			accessToken.value = null;
			refreshToken.value = null;
		}

		async function login(data: LoginRequest) {
			try {
				isLoading.value = true;
				error.value = null;
				const response = await AuthAPI.login(data);
				setAuthData({
					user: response.user,
					accessToken: response.accessToken,
					refreshToken: response.refreshToken,
				});
			} catch (err: any) {
				error.value = err?.message || 'Ошибка при входе';
				throw err;
			} finally {
				isLoading.value = false;
			}
		}

		async function sendVerification(data: SendVerificationRequest) {
			try {
				isLoading.value = true;
				error.value = null;
				await AuthAPI.sendVerification(data);
			} catch (err: any) {
				error.value = err?.message || 'Ошибка при отправке кода';
				throw err;
			} finally {
				isLoading.value = false;
			}
		}

		async function register(data: VerifyEmailRequest) {
			try {
				isLoading.value = true;
				error.value = null;
				const response = await AuthAPI.register(data);
				setAuthData({
					user: response.user,
					accessToken: response.accessToken,
					refreshToken: response.refreshToken,
				});
			} catch (err: any) {
				error.value = err?.message || 'Ошибка при регистрации';
				throw err;
			} finally {
				isLoading.value = false;
			}
		}

	async function logout() {
		try {
			if (refreshToken.value) {
				await AuthAPI.logout({ refreshToken: refreshToken.value });
			}
		} catch (err: any) {
			// Игнорируем ошибку 401 - это нормально, если токен уже истек
			// Просто очищаем данные локально
			const statusCode = err?.code || err?.response?.status || err?.status;
			if (statusCode !== 401) {
				console.error('Logout error:', err);
			}
		} finally {
			clearAuthData();
			error.value = null;
		}
	}

	async function checkAuth() {
		try {
			if (!accessToken.value) {
				return;
			}
			// Просто проверяем валидность токена
			// Данные пользователя уже должны быть в localStorage благодаря persist
			await AuthAPI.me();
		} catch (err: any) {
			// Token expired, try refresh
			await refreshAccessToken();
		}
	}

	async function refreshAccessToken() {
		try {
			if (!refreshToken.value) {
				throw new Error('No refresh token');
			}
			const response = await AuthAPI.refresh({
				refreshToken: refreshToken.value,
			});
			setAuthData({
				user: response.user,
				accessToken: response.accessToken,
				refreshToken: response.refreshToken,
			});
		} catch (err: any) {
			// Refresh failed, просто очищаем данные (не вызываем logout, чтобы избежать рекурсии)
			clearAuthData();
			error.value = null;
			throw err;
		}
	}

		function clearError() {
			error.value = null;
		}

		return {
			user,
			accessToken,
			refreshToken,
			isLoading,
			error,
			isAuth,
			login,
			sendVerification,
			register,
			logout,
			checkAuth,
			refreshAccessToken,
			clearError,
		};
	},
	{
		persist: {
			storage: localStorage,
			key: 'auth-storage',
		},
	},
);
