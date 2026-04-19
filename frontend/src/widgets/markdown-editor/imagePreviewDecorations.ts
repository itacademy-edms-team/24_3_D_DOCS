import { RangeSetBuilder } from '@codemirror/state';
import {
	Decoration,
	EditorView,
	ViewPlugin,
	WidgetType,
	type DecorationSet,
	type Extension,
	type ViewUpdate,
} from '@codemirror/view';

class ImageThumbWidget extends WidgetType {
	constructor(readonly url: string) {
		super();
	}

	override toDOM(): HTMLElement {
		const wrap = document.createElement('span');
		wrap.className = 'ddoc-cm-img-preview';
		const img = document.createElement('img');
		img.alt = '';
		img.loading = 'lazy';
		img.src = this.url;
		img.addEventListener(
			'error',
			() => {
				wrap.replaceChildren();
				wrap.classList.add('ddoc-cm-img-preview--broken');
				wrap.textContent = '×';
			},
			{ once: true },
		);
		wrap.appendChild(img);
		return wrap;
	}

	override ignoreEvent(): boolean {
		return true;
	}

	override eq(other: ImageThumbWidget): boolean {
		return other.url === this.url;
	}
}

function isSafeImageUrl(url: string): boolean {
	const u = url.trim().toLowerCase();
	if (!u) return false;
	if (u.startsWith('javascript:') || u.startsWith('vbscript:')) return false;
	if (u.startsWith('data:')) return u.startsWith('data:image/');
	return true;
}

/** Первый URL из destination картинки `![](... "title")`. */
function normalizeMdImageDest(inner: string): string {
	const s = inner.trim();
	const ang = s.match(/^<([^>]+)>/);
	if (ang) return ang[1].trim();
	const quoted = s.match(/^"([^"]*)"/);
	if (quoted) return quoted[1].trim();
	const first = s.split(/\s+/)[0];
	return (first ?? '').replace(/^['"]|['"]$/g, '');
}

function findMarkdownImages(lineFrom: number, lineText: string): Array<{ pos: number; url: string }> {
	const out: Array<{ pos: number; url: string }> = [];
	let i = 0;
	const t = lineText;
	while (i < t.length) {
		const start = t.indexOf('![', i);
		if (start === -1) break;
		const rb = t.indexOf(']', start + 2);
		if (rb === -1 || t[rb + 1] !== '(') {
			i = start + 2;
			continue;
		}
		const p0 = rb + 2;
		let j = p0;
		let depth = 1;
		while (j < t.length && depth > 0) {
			const c = t[j];
			if (c === '(') depth++;
			else if (c === ')') depth--;
			j++;
		}
		if (depth !== 0) {
			i = start + 2;
			continue;
		}
		const inner = t.slice(p0, j - 1);
		const url = normalizeMdImageDest(inner);
		const endAtom = lineFrom + j;
		if (url && isSafeImageUrl(url)) {
			out.push({ pos: endAtom, url });
		}
		i = j;
	}
	return out;
}

const imagePreviewPlugin = ViewPlugin.fromClass(
	class {
		decorations: DecorationSet;

		constructor(view: EditorView) {
			this.decorations = buildDecorations(view);
		}

		update(u: ViewUpdate) {
			if (u.docChanged || u.viewportChanged) {
				this.decorations = buildDecorations(u.view);
			}
		}
	},
	{ decorations: (v) => v.decorations },
);

function buildDecorations(view: EditorView) {
	const builder = new RangeSetBuilder();
	const { from, to } = view.viewport;
	const doc = view.state.doc;
	if (doc.length === 0) {
		return builder.finish();
	}
	const startLine = doc.lineAt(from).number;
	const endLine = doc.lineAt(Math.min(Math.max(0, to - 1), doc.length - 1)).number;
	for (let ln = startLine; ln <= endLine; ln++) {
		const line = doc.line(ln);
		for (const { pos, url } of findMarkdownImages(line.from, line.text)) {
			builder.add(
				pos,
				pos,
				Decoration.widget({
					widget: new ImageThumbWidget(url),
					side: 1,
				}),
			);
		}
	}
	return builder.finish();
}

const imagePreviewTheme = EditorView.baseTheme({
	'.ddoc-cm-img-preview': {
		display: 'inline-block',
		verticalAlign: 'middle',
		marginLeft: '6px',
	},
	'.ddoc-cm-img-preview img': {
		maxHeight: '1.35em',
		maxWidth: '5em',
		verticalAlign: 'middle',
		borderRadius: '3px',
		objectFit: 'contain',
	},
	'.ddoc-cm-img-preview--broken': {
		fontSize: '10px',
		color: 'var(--md-color, #94a3b8)',
		marginLeft: '4px',
	},
});

export const imagePreviewExtension: Extension = [imagePreviewPlugin, imagePreviewTheme];
