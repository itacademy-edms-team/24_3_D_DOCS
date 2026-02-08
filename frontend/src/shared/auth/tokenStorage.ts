const AUTH_STORAGE_KEY = 'auth-storage';

type AuthStorageState = {
	accessToken?: string | null;
	refreshToken?: string | null;
	user?: unknown;
	[key: string]: unknown;
};

type AuthStorageWrapper = {
	state?: AuthStorageState;
	[key: string]: unknown;
};

function getLocalStorage(): Storage | null {
	if (typeof window === 'undefined') {
		return null;
	}

	try {
		return window.localStorage;
	} catch {
		return null;
	}
}

function readRawStorage(): AuthStorageWrapper | null {
	const storage = getLocalStorage();
	if (!storage) {
		return null;
	}

	const raw = storage.getItem(AUTH_STORAGE_KEY);
	if (!raw) {
		return null;
	}

	try {
		const parsed = JSON.parse(raw);
		return typeof parsed === 'object' && parsed !== null ? (parsed as AuthStorageWrapper) : null;
	} catch {
		storage.removeItem(AUTH_STORAGE_KEY);
		return null;
	}
}

function writeRawStorage(value: AuthStorageWrapper): void {
	const storage = getLocalStorage();
	if (!storage) {
		return;
	}
	storage.setItem(AUTH_STORAGE_KEY, JSON.stringify(value));
}

function extractState(wrapper: AuthStorageWrapper | null): AuthStorageState | null {
	if (!wrapper) {
		return null;
	}

	if (wrapper.state && typeof wrapper.state === 'object') {
		return wrapper.state;
	}

	const { state, ...rest } = wrapper;
	return rest as AuthStorageState;
}

function getAuthState(): AuthStorageState | null {
	return extractState(readRawStorage());
}

export function getAccessToken(): string | null {
	return getAuthState()?.accessToken ?? null;
}
