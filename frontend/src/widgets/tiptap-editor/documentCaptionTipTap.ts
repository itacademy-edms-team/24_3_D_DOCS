import { Node, mergeAttributes } from '@tiptap/core';
import { VueNodeViewRenderer } from '@tiptap/vue-3';
import DocumentCaptionNodeView from './DocumentCaptionNodeView.vue';

function safeDecodeURIComponent(s: string): string {
	try {
		return decodeURIComponent(s);
	} catch {
		return s;
	}
}

export type CaptionKind = 'IMAGE' | 'TABLE' | 'FORMULA';

export const DocumentCaptionTipTap = Node.create({
	name: 'documentCaption',
	group: 'block',
	atom: true,
	selectable: true,
	draggable: true,

	addAttributes() {
		return {
			kind: { default: 'TABLE' },
			text: { default: '' },
		};
	},

	parseHTML() {
		return [
			{
				tag: 'div[data-ddoc-caption]',
				getAttrs: (el) => {
					const n = el as HTMLElement;
					const k = (n.getAttribute('data-ddoc-caption') || 'TABLE') as CaptionKind;
					const raw = n.getAttribute('data-ddoc-text') || '';
					return {
						kind: k,
						text: safeDecodeURIComponent(raw),
					};
				},
			},
		];
	},

	renderHTML({ node, HTMLAttributes }) {
		const kind = node.attrs.kind as string;
		const text = String(node.attrs.text ?? '');
		const enc = encodeURIComponent(text);
		return [
			'div',
			mergeAttributes(HTMLAttributes, {
				'data-ddoc-caption': kind,
				'data-ddoc-text': enc,
			}),
		];
	},

	addNodeView() {
		return VueNodeViewRenderer(DocumentCaptionNodeView);
	},
});
