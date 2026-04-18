import { Node, mergeAttributes } from '@tiptap/core';
import { VueNodeViewRenderer } from '@tiptap/vue-3';
import InlineMathNodeView from './InlineMathNodeView.vue';
import BlockMathNodeView from './BlockMathNodeView.vue';

function safeDecodeURIComponent(s: string): string {
	try {
		return decodeURIComponent(s);
	} catch {
		return s;
	}
}

export const InlineMathTipTap = Node.create({
	name: 'inlineMath',
	group: 'inline',
	inline: true,
	atom: true,

	addAttributes() {
		return {
			formula: { default: '' },
		};
	},

	parseHTML() {
		return [
			{
				tag: 'span[data-type="inline-math"]',
				getAttrs: (el) => {
					const n = el as HTMLElement;
					const raw = n.getAttribute('data-formula') || '';
					return { formula: safeDecodeURIComponent(raw) };
				},
			},
		];
	},

	renderHTML({ HTMLAttributes, node }) {
		const enc = encodeURIComponent(node.attrs.formula as string);
		return [
			'span',
			mergeAttributes(HTMLAttributes, {
				'data-type': 'inline-math',
				'data-formula': enc,
			}),
		];
	},

	addNodeView() {
		return VueNodeViewRenderer(InlineMathNodeView);
	},

	addKeyboardShortcuts() {
		return {
			'Mod-Shift-m': () =>
				this.editor
					.chain()
					.focus()
					.insertContent({ type: this.name, attrs: { formula: '' } })
					.run(),
		};
	},
});

export const BlockMathTipTap = Node.create({
	name: 'blockMath',
	group: 'block',
	atom: true,

	addAttributes() {
		return {
			formula: { default: '' },
		};
	},

	parseHTML() {
		return [
			{
				tag: 'div[data-type="block-math"]',
				getAttrs: (el) => {
					const n = el as HTMLElement;
					const raw = n.getAttribute('data-formula') || '';
					return { formula: safeDecodeURIComponent(raw) };
				},
			},
		];
	},

	renderHTML({ HTMLAttributes, node }) {
		const enc = encodeURIComponent(node.attrs.formula as string);
		return [
			'div',
			mergeAttributes(HTMLAttributes, {
				'data-type': 'block-math',
				'data-formula': enc,
			}),
		];
	},

	addNodeView() {
		return VueNodeViewRenderer(BlockMathNodeView);
	},

	addKeyboardShortcuts() {
		return {
			'Mod-Shift-M': () =>
				this.editor
					.chain()
					.focus()
					.insertContent({ type: this.name, attrs: { formula: '' } })
					.run(),
		};
	},
});
