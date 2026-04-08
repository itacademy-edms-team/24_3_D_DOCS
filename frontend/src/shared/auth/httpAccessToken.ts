/**
 * In-memory access token for axios/fetch. Pinia may update before pinia-plugin
 * flushes to localStorage; using this first fixes wrong-user API calls after login/register.
 */
let accessTokenMemory: string | null = null;

export function setHttpAccessToken(token: string | null): void {
	accessTokenMemory = token;
}

export function getHttpAccessToken(): string | null {
	return accessTokenMemory;
}
