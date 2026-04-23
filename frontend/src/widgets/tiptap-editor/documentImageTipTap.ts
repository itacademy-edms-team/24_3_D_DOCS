import Image from '@tiptap/extension-image';
import { mergeAttributes } from '@tiptap/core';
import { Plugin, PluginKey } from '@tiptap/pm/state';
import type { EditorView } from '@tiptap/pm/view';
import { tryRefreshAccessToken, getAuthApiBaseUrl } from '@/shared/auth/tryRefreshAccessToken';
import { withFreshUploadAssetToken } from '@/shared/utils/withFreshUploadAssetToken';

const ASSET_SRC_ATTR = 'data-ddoc-asset-src';

function isApiAssetUrl(src: string): boolean {
	const s = src.trim();
	return s.includes('/api/') || s.startsWith('api/');
}

function patchTipTapDocumentImages(dom: HTMLElement): void {
	dom.querySelectorAll(`img[${ASSET_SRC_ATTR}]`).forEach((el) => {
		const img = el as HTMLImageElement;
		const raw = img.getAttribute(ASSET_SRC_ATTR) || '';
		if (!raw || !isApiAssetUrl(raw)) return;

		const next = withFreshUploadAssetToken(raw);
		if (img.getAttribute('src') !== next) {
			img.setAttribute('src', next);
		}

		if (img.dataset.ddocAssetErrBound === '1') return;
		img.dataset.ddocAssetErrBound = '1';
		img.addEventListener(
			'error',
			() => {
				void (async () => {
					await tryRefreshAccessToken(getAuthApiBaseUrl());
					const r = img.getAttribute(ASSET_SRC_ATTR) || '';
					if (r) img.setAttribute('src', withFreshUploadAssetToken(r));
				})();
			},
			{ passive: true },
		);
	});
}

/**
 * Как `@tiptap/extension-image`, но для URL ассетов API подставляет актуальный JWT в `src`
 * (как axios, только для `<img>`) и при ошибке один раз пробует refresh.
 */
export const DocumentImageTipTap = Image.extend({
	addProseMirrorPlugins() {
		return [
			new Plugin({
				key: new PluginKey('documentImageAuthSync'),
				view(view: EditorView) {
					const root = view.dom as HTMLElement;
					patchTipTapDocumentImages(root);
					return {
						update() {
							patchTipTapDocumentImages(root);
						},
					};
				},
			}),
		];
	},

	renderHTML({ HTMLAttributes }) {
		const merged = mergeAttributes(this.options.HTMLAttributes, HTMLAttributes) as Record<
			string,
			string | number | boolean | undefined | null
		>;
		const raw = typeof merged.src === 'string' ? merged.src : '';
		if (raw && isApiAssetUrl(raw)) {
			merged[ASSET_SRC_ATTR] = raw;
			merged.src = withFreshUploadAssetToken(raw);
		}
		return ['img', merged];
	},
});
