import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type AxiosResponse
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
      baseURL: options?.baseURL || (typeof BASE_URI !== 'undefined' ? BASE_URI : 'http://localhost:5159'),
      headers: options?.headers,
    });

    // Request interceptor: Add Authorization header
    this.instance.interceptors.request.use(
      (config) => {
        const authData = localStorage.getItem('auth-storage');
        if (authData) {
          const { state } = JSON.parse(authData);
          if (state?.accessToken) {
            config.headers.Authorization = `Bearer ${state.accessToken}`;
          }
        }
        return config;
      },
      (error) => Promise.reject(error)
    );
    
    // Response interceptor: Handle errors
    this.instance.interceptors.response.use(
      (response: AxiosResponse) => response,
      (error: AxiosError) => this.errorHandler(error)
    );
  }

  protected async _get<T>(uri: string, config?: AxiosRequestConfig): Promise<T> {
    return this.instance.get<T>(uri, config).then((res: AxiosResponse<T>) => res.data);
  }

  protected async _post<T, D = any>(uri: string, data?: D, config?: AxiosRequestConfig): Promise<T> {
    return this.instance.post<T>(uri, data, config).then((res: AxiosResponse<T>) => res.data);
  }

  protected async _put<T, D = any>(uri: string, data?: D, config?: AxiosRequestConfig): Promise<T> {
    return this.instance.put<T>(uri, data, config).then((res: AxiosResponse<T>) => res.data);
  }

  protected async _delete<T,>(uri: string, config?: AxiosRequestConfig): Promise<T> {
    return this.instance.delete<T>(uri, config).then((res: AxiosResponse<T>) => res.data);
  }

  // Public methods
  async get<T>(uri: string, config?: AxiosRequestConfig): Promise<T> {
    return this._get<T>(uri, config);
  }

  async post<T, D = any>(uri: string, data?: D, config?: AxiosRequestConfig): Promise<T> {
    return this._post<T, D>(uri, data, config);
  }

  async put<T, D = any>(uri: string, data?: D, config?: AxiosRequestConfig): Promise<T> {
    return this._put<T, D>(uri, data, config);
  }

  async delete<T>(uri: string, config?: AxiosRequestConfig): Promise<T> {
    return this._delete<T>(uri, config);
  }

  async getBlob(uri: string, config?: AxiosRequestConfig): Promise<Blob> {
    const response = await this.instance.get(uri, { 
      ...config, 
      responseType: 'blob' 
    });
    return response.data;
  }

  private errorHandler(error: AxiosError) {
    const status = error.response?.status || error.status;
    // Получаем сообщение с бэкенда
    const backendMessage = (error.response?.data as any)?.message;
    const message = backendMessage || error.message;

    switch (status) {
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
