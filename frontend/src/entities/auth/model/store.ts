import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import AuthAPI from '../api/AuthAPI';
import type { User, LoginRequest, SendVerificationRequest, VerifyEmailRequest } from './types';

interface AuthState {
	user: User | null;
	accessToken: string | null;
	refreshToken: string | null;
	isAuth: boolean;
	isLoading: boolean;
	error: string | null;

	// Actions
	login: (data: LoginRequest) => Promise<void>;
	sendVerification: (data: SendVerificationRequest) => Promise<void>;
	register: (data: VerifyEmailRequest) => Promise<void>;
	logout: () => Promise<void>;
	checkAuth: () => Promise<void>;
	refreshAccessToken: () => Promise<void>;
	clearError: () => void;
}

export const useAuthStore = create<AuthState>()(
	persist(
		(set, get) => ({
			user: null,
			accessToken: null,
			refreshToken: null,
			isAuth: false,
			isLoading: false,
			error: null,

			login: async (data: LoginRequest) => {
				try {
					set({ isLoading: true, error: null });
					const response = await AuthAPI.login(data);

					set({
						user: response.user,
						accessToken: response.accessToken,
						refreshToken: response.refreshToken,
						isAuth: true,
						isLoading: false,
						error: null,
					});
				} catch (error: any) {
					const errorMessage = error?.message || 'Ошибка при входе';
					set({
						error: errorMessage,
						isLoading: false,
					});
					throw error;
				}
			},

		sendVerification: async (data: SendVerificationRequest) => {
			try {
				set({ isLoading: true, error: null });
				await AuthAPI.sendVerification(data);
				set({ isLoading: false });
			} catch (error: any) {
				set({
					error: error.message || 'Ошибка при отправке кода',
					isLoading: false,
				});
				throw error;
			}
		},

		register: async (data: VerifyEmailRequest) => {
			try {
				set({ isLoading: true, error: null });
				const response = await AuthAPI.register(data);

				set({
					user: response.user,
					accessToken: response.accessToken,
					refreshToken: response.refreshToken,
					isAuth: true,
					isLoading: false,
				});
			} catch (error: any) {
				set({
					error: error.message || 'Ошибка при регистрации',
					isLoading: false,
				});
				throw error;
			}
		},

			logout: async () => {
				try {
					const { refreshToken } = get();
					if (refreshToken) {
						await AuthAPI.logout({ refreshToken });
					}
				} catch (error) {
					console.error('Logout error:', error);
				} finally {
					set({
						user: null,
						accessToken: null,
						refreshToken: null,
						isAuth: false,
						error: null,
					});
				}
			},

			checkAuth: async () => {
				try {
					const { accessToken } = get();
					if (!accessToken) {
						set({ isAuth: false });
						return;
					}

					const response = await AuthAPI.me();
					set({ isAuth: true });
				} catch (error: any) {
					// Token expired, try refresh
					await get().refreshAccessToken();
				}
			},

			refreshAccessToken: async () => {
				try {
					const { refreshToken } = get();
					if (!refreshToken) {
						throw new Error('No refresh token');
					}

					const response = await AuthAPI.refresh({ refreshToken });

					set({
						user: response.user,
						accessToken: response.accessToken,
						refreshToken: response.refreshToken,
						isAuth: true,
					});
				} catch (error: any) {
					// Refresh failed, logout
					await get().logout();
					throw error;
				}
			},

			clearError: () => set({ error: null }),
		}),
		{
			name: 'auth-storage',
			partialize: (state) => ({
				accessToken: state.accessToken,
				refreshToken: state.refreshToken,
				user: state.user,
			}),
		},
	),
);

