import HttpClient from '@/shared/api/HttpClient';
import type {
	AuthResponse,
	LoginRequest,
	LogoutRequest,
	MeResponse,
	RefreshTokenRequest,
	SendVerificationRequest,
	VerifyEmailRequest,
} from '../types';

class AuthAPI extends HttpClient {
	constructor() {
		super({
			baseURL: typeof BASE_URI !== 'undefined' ? BASE_URI : 'http://localhost:5159',
		});
	}

	async sendVerification(data: SendVerificationRequest): Promise<{ message: string }> {
		return this._post('/api/auth/send-verification', data);
	}

	async register(data: VerifyEmailRequest): Promise<AuthResponse> {
		return this._post('/api/auth/register', data);
	}

	async login(data: LoginRequest): Promise<AuthResponse> {
		return this._post('/api/auth/login', data);
	}

	async refresh(data: RefreshTokenRequest): Promise<AuthResponse> {
		return this._post('/api/auth/refresh', data);
	}

	async logout(data: LogoutRequest): Promise<{ message: string }> {
		return this._post('/api/auth/logout', data);
	}

	async me(): Promise<MeResponse> {
		return this._get('/api/auth/me');
	}
}

export default new AuthAPI();
