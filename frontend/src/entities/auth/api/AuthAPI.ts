import HttpClient from '@api';
import type {
	AuthResponse,
	LoginRequest,
	LogoutRequest,
	MeResponse,
	RefreshTokenRequest,
	SendVerificationRequest,
	VerifyEmailRequest,
} from '../model/types';

class AuthAPI extends HttpClient {
	constructor() {
		super({
			baseURL: typeof BASE_URI !== 'undefined' ? BASE_URI : 'http://localhost:5159',
		});
	}

	async sendVerification(data: SendVerificationRequest): Promise<{ message: string }> {
		return this._post<{ message: string }, SendVerificationRequest>('/api/auth/send-verification', data);
	}

	async register(data: VerifyEmailRequest): Promise<AuthResponse> {
		return this._post<AuthResponse, VerifyEmailRequest>('/api/auth/register', data);
	}

	async login(data: LoginRequest): Promise<AuthResponse> {
		return this._post<AuthResponse, LoginRequest>('/api/auth/login', data);
	}

	async refresh(data: RefreshTokenRequest): Promise<AuthResponse> {
		return this._post<AuthResponse, RefreshTokenRequest>('/api/auth/refresh', data);
	}

	async logout(data: LogoutRequest): Promise<{ message: string }> {
		return this._post<{ message: string }, LogoutRequest>('/api/auth/logout', data);
	}

	async me(): Promise<MeResponse> {
		return this._get<MeResponse>('/api/auth/me');
	}
}

export default new AuthAPI();

