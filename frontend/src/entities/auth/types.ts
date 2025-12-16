export interface User {
	id: string;
	email: string;
	name: string;
	role: string;
	createdAt: string;
}

export interface LoginRequest {
	email: string;
	password: string;
}

export interface SendVerificationRequest {
	email: string;
	password: string;
	name: string;
}

export interface VerifyEmailRequest {
	email: string;
	code: string;
}

export interface AuthResponse {
	accessToken: string;
	refreshToken: string;
	accessTokenExpiration: string;
	refreshTokenExpiration: string;
	user: User;
}

export interface RefreshTokenRequest {
	refreshToken: string;
}

export interface LogoutRequest {
	refreshToken: string;
}

export interface MeResponse {
	userId: string;
	email: string;
	role: string;
	claims: Array<{ type: string; value: string }>;
}
