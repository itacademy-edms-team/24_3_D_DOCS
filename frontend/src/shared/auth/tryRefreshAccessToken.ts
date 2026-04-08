/**
 * Обновление access token через refresh (без axios).
 * Нужен для fetch-вызовов (SSE агент, multipart ingest), которые не проходят через HttpClient interceptors.
 */
import { setHttpAccessToken } from './httpAccessToken';

const AUTH_STORAGE_KEY = 'auth-storage';

type TokenRefreshResponse = {
	accessToken?: string;
	refreshToken?: string;
};

function parseAuthStorage(): Record<string, unknown> | null {
	const raw = localStorage.getItem(AUTH_STORAGE_KEY);
	if (!raw) return null;
	try {
		const parsed = JSON.parse(raw) as unknown;
		return typeof parsed === 'object' && parsed !== null
			? (parsed as Record<string, unknown>)
			: null;
	} catch {
		return null;
	}
}

function readRefreshToken(parsed: Record<string, unknown>): string | null {
	const state = parsed.state;
	if (state && typeof state === 'object' && state !== null) {
		const rt = (state as Record<string, unknown>).refreshToken;
		if (typeof rt === 'string' && rt) return rt;
	}
	const rt = parsed.refreshToken;
	return typeof rt === 'string' && rt ? rt : null;
}

/**
 * @returns true если токены в localStorage обновлены
 */
export async function tryRefreshAccessToken(apiBaseUrl: string): Promise<boolean> {
	const parsed = parseAuthStorage();
	if (!parsed) return false;

	const refreshToken = readRefreshToken(parsed);
	if (!refreshToken) return false;

	const base = apiBaseUrl.replace(/\/$/, '');
	const res = await fetch(`${base}/api/auth/refresh`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify({ refreshToken }),
	});

	if (!res.ok) return false;

	let data: TokenRefreshResponse;
	try {
		data = (await res.json()) as TokenRefreshResponse;
	} catch {
		return false;
	}

	const accessToken = data.accessToken;
	const newRefresh = data.refreshToken;
	if (!accessToken || !newRefresh) return false;

	const state =
		parsed.state && typeof parsed.state === 'object' && parsed.state !== null
			? { ...(parsed.state as Record<string, unknown>) }
			: {};

	state.accessToken = accessToken;
	state.refreshToken = newRefresh;

	const updated = { ...parsed, state };
	localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(updated));
	setHttpAccessToken(accessToken);
	return true;
}
