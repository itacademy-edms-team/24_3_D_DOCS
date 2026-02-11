import axios, {
	AxiosError,
	type AxiosInstance,
	type AxiosRequestConfig,
	type AxiosResponse,
} from 'axios';

import BadRequest from './error/BadRequest';
import HttpError from './error/HttpError';
import NotFoundError from './error/NotFoundError';
import ServerError from './error/ServerError';

interface HttpClientOptions {
	baseURL: string;
	headers?: Record<string, string>;
}

const AUTH_STORAGE_KEY = 'auth-storage';
const LAST_REFRESH_FAILURE_KEY = 'auth-last-refresh-failure';
const REFRESH_COOLDOWN_MS = 5000; // Не повторять refresh, если недавно уже провалился

class HttpClient {
	private readonly instance: AxiosInstance;
	private readonly baseURL: string;
	private isRefreshing = false;
	private failedQueue: Array<{
		resolve: (value?: any) => void;
		reject: (error?: any) => void;
	}> = [];

	constructor(options?: HttpClientOptions) {
		this.baseURL =
			options?.baseURL ||
			(typeof BASE_URI !== 'undefined'
				? BASE_URI
				: 'http://localhost:5159');

		this.instance = axios.create({
			baseURL: this.baseURL,
			headers: options?.headers,
		});

		// Request interceptor: Add Authorization header
		this.instance.interceptors.request.use(
			(config) => {
				const authData = localStorage.getItem(AUTH_STORAGE_KEY);
				if (authData) {
					try {
						const parsed = JSON.parse(authData);
						// Pinia persist сохраняет данные в формате { state: { ... } }
						const accessToken = parsed?.state?.accessToken || parsed?.accessToken;
						if (accessToken) {
							config.headers.Authorization = `Bearer ${accessToken}`;
						}
					} catch {
						// Игнорируем ошибки парсинга
					}
				}
				return config;
			},
			(error) => Promise.reject(error),
		);

		// Response interceptor: Handle errors and token refresh
		this.instance.interceptors.response.use(
			(response: AxiosResponse) => response,
			async (error: AxiosError) => {
				const originalRequest = error.config as any;

				// Если ошибка 401 и это не запрос на refresh или login
				if (
					error.response?.status === 401 &&
					originalRequest &&
					!originalRequest._retry &&
					!originalRequest.url?.includes('/auth/refresh') &&
					!originalRequest.url?.includes('/auth/login') &&
					!originalRequest.url?.includes('/auth/register')
				) {
					// Если недавно refresh уже провалился (например, в браузере Cursor с отдельным storage) —
					// сразу очищаем и редиректим, без повторных попыток
					const lastFailure = sessionStorage.getItem(LAST_REFRESH_FAILURE_KEY);
					if (lastFailure) {
						const elapsed = Date.now() - parseInt(lastFailure, 10);
						if (elapsed < REFRESH_COOLDOWN_MS) {
							this.clearAuthAndRedirect();
							return Promise.reject(error);
						}
						sessionStorage.removeItem(LAST_REFRESH_FAILURE_KEY);
					}

					if (this.isRefreshing) {
						// Если уже идет обновление токена, добавляем запрос в очередь
						return new Promise((resolve, reject) => {
							this.failedQueue.push({ resolve, reject });
						})
							.then(() => {
								return this.instance(originalRequest);
							})
							.catch((err) => {
								return Promise.reject(err);
							});
					}

					originalRequest._retry = true;
					this.isRefreshing = true;

					try {
						// Пытаемся обновить токен
						const authData = localStorage.getItem(AUTH_STORAGE_KEY);
						let parsed: any = null;
						let refreshToken: string | undefined;
						if (authData) {
							try {
								parsed = JSON.parse(authData);
								refreshToken =
									parsed?.state?.refreshToken || parsed?.refreshToken;
							} catch {
								// Игнорируем ошибки парсинга
							}
						}

						// Нет refresh token — сразу очищаем и редиректим
						if (!refreshToken) {
							this.clearAuthAndRedirect();
							return Promise.reject(error);
						}

						// Создаем временный axios instance для refresh, чтобы избежать рекурсии
						const refreshInstance = axios.create({
							baseURL: this.baseURL,
						});

						const refreshResponse = await refreshInstance.post(
							'/api/auth/refresh',
							{ refreshToken },
						);

						const newAccessToken = refreshResponse.data.accessToken;
						const newRefreshToken = refreshResponse.data.refreshToken;
						const user = refreshResponse.data.user;

						// Обновляем токены в localStorage
						if (parsed) {
							const updatedAuthData = {
								...parsed,
								state: {
									...parsed.state,
									accessToken: newAccessToken,
									refreshToken: newRefreshToken,
									user: user || parsed?.state?.user,
								},
							};
							localStorage.setItem(
								AUTH_STORAGE_KEY,
								JSON.stringify(updatedAuthData),
							);
						}

						// Обновляем заголовок для оригинального запроса
						originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;

						// Обрабатываем очередь ожидающих запросов
						this.processQueue(null);

						// Повторяем оригинальный запрос
						return this.instance(originalRequest);
					} catch (refreshError) {
						// Запоминаем время провала, чтобы не спамить refresh при повторных 401
						sessionStorage.setItem(LAST_REFRESH_FAILURE_KEY, String(Date.now()));
						this.processQueue(refreshError);
						this.clearAuthAndRedirect();
						return Promise.reject(refreshError);
					} finally {
						this.isRefreshing = false;
					}
				}

				return this.errorHandler(error);
			},
		);
	}

	private clearAuthAndRedirect() {
		localStorage.removeItem(AUTH_STORAGE_KEY);
		// Синхронизируем Pinia store через событие (избегаем циклических импортов)
		try {
			window.dispatchEvent(new CustomEvent('auth:session-expired'));
		} catch {
			// Игнорируем
		}
		if (
			typeof window !== 'undefined' &&
			window.location.pathname !== '/auth' &&
			!window.location.pathname.startsWith('/auth')
		) {
			// replace вместо href — не оставляет failed state в истории
			window.location.replace('/auth');
		}
	}

	private processQueue(error: any) {
		this.failedQueue.forEach((prom) => {
			if (error) {
				prom.reject(error);
			} else {
				prom.resolve();
			}
		});
		this.failedQueue = [];
	}

	protected async _get<T>(
		uri: string,
		config?: AxiosRequestConfig,
	): Promise<T> {
		return this.instance
			.get<T>(uri, config)
			.then((res: AxiosResponse<T>) => res.data);
	}

	protected async _post<T, D = any>(
		uri: string,
		data?: D,
		config?: AxiosRequestConfig,
	): Promise<T> {
		return this.instance
			.post<T>(uri, data, config)
			.then((res: AxiosResponse<T>) => res.data);
	}

	protected async _put<T, D = any>(
		uri: string,
		data?: D,
		config?: AxiosRequestConfig,
	): Promise<T> {
		return this.instance
			.put<T>(uri, data, config)
			.then((res: AxiosResponse<T>) => res.data);
	}

	protected async _delete<T>(
		uri: string,
		config?: AxiosRequestConfig,
	): Promise<T> {
		return this.instance
			.delete<T>(uri, config)
			.then((res: AxiosResponse<T>) => {
				// При статусе 204 NoContent тело ответа пустое
				if (res.status === 204) {
					// Для void типа возвращаем undefined
					return (undefined as unknown) as T;
				}
				return res.data;
			});
	}

	// Public methods
	async get<T>(uri: string, config?: AxiosRequestConfig): Promise<T> {
		return this._get<T>(uri, config);
	}

	async post<T, D = any>(
		uri: string,
		data?: D,
		config?: AxiosRequestConfig,
	): Promise<T> {
		return this._post<T, D>(uri, data, config);
	}

	async put<T, D = any>(
		uri: string,
		data?: D,
		config?: AxiosRequestConfig,
	): Promise<T> {
		return this._put<T, D>(uri, data, config);
	}

	async delete<T>(uri: string, config?: AxiosRequestConfig): Promise<T> {
		return this._delete<T>(uri, config);
	}

	async getBlob(uri: string, config?: AxiosRequestConfig): Promise<Blob> {
		const response = await this.instance.get(uri, {
			...config,
			responseType: 'blob',
		});
		return response.data;
	}

	private errorHandler(error: AxiosError) {
		const status = error.response?.status || error.status;
		const responseData = error.response?.data as any;

		// Получаем сообщение с бэкенда
		let message = responseData?.message || error.message;

		// Если есть ошибки валидации ASP.NET Core, объединяем их в одно сообщение
		if (responseData?.errors && typeof responseData.errors === 'object') {
			const validationErrors: string[] = [];
			for (const key in responseData.errors) {
				if (Array.isArray(responseData.errors[key])) {
					validationErrors.push(...responseData.errors[key]);
				} else {
					validationErrors.push(String(responseData.errors[key]));
				}
			}
			if (validationErrors.length > 0) {
				message = validationErrors.join(', ');
			}
		}

		switch (status) {
			case 401:
			case 403:
				// Для 401/403 используем HttpError, но с правильным сообщением
				return Promise.reject(
					new HttpError(status, message || 'Неверный email или пароль'),
				);
			case 404:
				return Promise.reject(new NotFoundError(message));
			case 400:
				return Promise.reject(new BadRequest(message));
			case 500:
				return Promise.reject(new ServerError(message));
			default:
				return Promise.reject(new HttpError(status || 500, message));
		}
	}
}

export default HttpClient;
