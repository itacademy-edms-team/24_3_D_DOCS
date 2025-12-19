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

export interface HttpClientOptions {
	baseURL: string;
	headers?: Record<string, string>;
}

class HttpClient {
	private readonly instance: AxiosInstance;

	constructor(options?: HttpClientOptions) {
		this.instance = axios.create({
			baseURL:
				options?.baseURL ||
				(typeof BASE_URI !== 'undefined'
					? BASE_URI
					: 'http://localhost:5159'),
			headers: options?.headers,
		});

		this.instance.interceptors.request.use((config) => {
			const authData = localStorage.getItem('auth-storage');
			if (authData) {
				try {
					const parsed = JSON.parse(authData);
					const accessToken = parsed?.state?.accessToken || parsed?.accessToken;
					if (accessToken) {
						config.headers.Authorization = `Bearer ${accessToken}`;
					}
				} catch {}
			}
			return config;
		});

		this.instance.interceptors.response.use(
			(response: AxiosResponse) => response,
			(error: AxiosError) => this.errorHandler(error),
		);
	}

	protected async _get<T>(uri: string, config?: AxiosRequestConfig): Promise<T> {
		return this.instance.get<T>(uri, config).then((res) => res.data);
	}

	protected async _post<T, D = any>(uri: string, data?: D, config?: AxiosRequestConfig): Promise<T> {
		return this.instance.post<T>(uri, data, config).then((res) => res.data);
	}

	protected async _put<T, D = any>(uri: string, data?: D, config?: AxiosRequestConfig): Promise<T> {
		return this.instance.put<T>(uri, data, config).then((res) => res.data);
	}

	protected async _delete<T>(uri: string, config?: AxiosRequestConfig): Promise<T> {
		return this.instance.delete<T>(uri, config).then((res) => res.data);
	}

	private errorHandler(error: AxiosError) {
		const status = error.response?.status || error.status;
		const responseData = error.response?.data as any;
		let message = responseData?.message || error.message;

		if (responseData?.errors && typeof responseData.errors === 'object') {
			const errors = Object.values(responseData.errors).flat();
			if (errors.length > 0) {
				message = errors.join(', ');
			}
		}

		switch (status) {
			case 401:
			case 403:
				return Promise.reject(new HttpError(status, message || 'Неверный email или пароль'));
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
