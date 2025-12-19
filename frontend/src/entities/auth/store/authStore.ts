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
			isLoading.value = true;
			error.value = null;
			try {
				const response = await AuthAPI.login(data);
				setAuthData(response);
			} catch (err: any) {
				error.value = err?.message || 'Ошибка при входе';
				throw err;
			} finally {
				isLoading.value = false;
			}
		}

		async function sendVerification(data: SendVerificationRequest) {
			isLoading.value = true;
			error.value = null;
			try {
				await AuthAPI.sendVerification(data);
			} catch (err: any) {
				error.value = err?.message || 'Ошибка при отправке кода';
				throw err;
			} finally {
				isLoading.value = false;
			}
		}

		async function register(data: VerifyEmailRequest) {
			isLoading.value = true;
			error.value = null;
			try {
				const response = await AuthAPI.register(data);
				setAuthData(response);
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
		} catch {}
		finally {
			clearAuthData();
			error.value = null;
		}
	}

	async function checkAuth() {
		if (!accessToken.value) return;
		
		try {
			await AuthAPI.me();
		} catch (err: any) {
			if (err?.code === 401) {
				await refreshAccessToken();
			} else {
				clearAuthData();
			}
		}
	}

	async function refreshAccessToken() {
		if (!refreshToken.value) {
			clearAuthData();
			return;
		}
		
		try {
			const response = await AuthAPI.refresh({ refreshToken: refreshToken.value });
			setAuthData(response);
		} catch {
			clearAuthData();
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
