import { getAccessToken } from '@/shared/auth/tokenStorage';

/**
 * Ассеты документа отдаются с `/api/upload/document/.../asset/...` с JWT в query или в заголовке.
 * В сохранённом markdown часто протухший `token`; у `<img>` нет Authorization — подставляем текущий access token.
 */
export function withFreshUploadAssetToken(src: string): string {
	if (!src) return src;
	const trimmed = src.trim();
	if (!trimmed.includes('/api/') && !trimmed.startsWith('api/')) {
		return src;
	}

	const currentToken = getAccessToken();
	if (!currentToken) {
		return src;
	}

	try {
		const base = typeof BASE_URI !== 'undefined' ? BASE_URI : window.location.origin;

		let url: URL;
		if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
			url = new URL(trimmed);
		} else {
			url = new URL(trimmed, base);
		}

		url.searchParams.set('token', currentToken);
		return url.toString();
	} catch {
		try {
			const base = typeof BASE_URI !== 'undefined' ? BASE_URI : '';
			let fullUrl = trimmed;
			if (base && !trimmed.startsWith('http')) {
				fullUrl = base.endsWith('/')
					? base + trimmed.replace(/^\//, '')
					: base + (trimmed.startsWith('/') ? trimmed : '/' + trimmed);
			}

			const urlWithoutToken = fullUrl.split('?')[0];
			const existingParams = fullUrl.includes('?') ? fullUrl.split('?')[1] : '';
			const params = new URLSearchParams(existingParams);
			params.set('token', currentToken);

			return `${urlWithoutToken}?${params.toString()}`;
		} catch {
			const baseUrl = trimmed.split('?')[0];
			const existingParams = trimmed.includes('?') ? trimmed.split('?')[1] : '';
			const params = new URLSearchParams(existingParams);
			params.set('token', currentToken);
			return `${baseUrl}?${params.toString()}`;
		}
	}
}
