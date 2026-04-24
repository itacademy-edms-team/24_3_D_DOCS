import { Extension } from '@tiptap/core';
import { Plugin, PluginKey } from '@tiptap/pm/state';
import type { Editor } from '@tiptap/core';
import {
	catalogIdsForPayload,
	hotkeyChordMatches,
	isModifierOnlyCode,
	type EditorHotkeyActionId,
	type EditorHotkeyChord,
} from '@/shared/constants/editorHotkeyCatalog';
import type { OpenFormulaBuilderFn } from './formulaBuilderContext';

export interface EditorHotkeysTipTapOptions {
	getBindings: () => Record<EditorHotkeyActionId, EditorHotkeyChord | null>;
	openFormulaBuilder: OpenFormulaBuilderFn;
	requestImageUpload: (mode: 'insert' | 'crop') => void;
	getDocumentId: () => string | undefined;
}

function applyHotkey(editor: Editor, id: EditorHotkeyActionId, opts: EditorHotkeysTipTapOptions): boolean {
	const chain = () => editor.chain().focus();

	switch (id) {
		case 'toggle_bold':
			return chain().toggleBold().run();
		case 'toggle_italic':
			return chain().toggleItalic().run();
		case 'toggle_list':
			return chain().toggleBulletList().run();
		case 'toggle_highlight':
			return chain().toggleHighlight().run();
		case 'toggle_heading':
			return chain().toggleHeading({ level: 2 }).run();
		case 'insert_formula':
			opts.openFormulaBuilder({
				initialLatex: '',
				isBlock: true,
				onApply: (latex) => {
					editor
						.chain()
						.focus()
						.insertContent({ type: 'blockMath', attrs: { formula: latex } })
						.run();
				},
			});
			return true;
		case 'add_caption_table':
			return chain()
				.insertContent({
					type: 'documentCaption',
					attrs: { kind: 'TABLE', text: 'текст подписи' },
				})
				.run();
		case 'add_caption_formula':
			return chain()
				.insertContent({
					type: 'documentCaption',
					attrs: { kind: 'FORMULA', text: 'текст подписи' },
				})
				.run();
		case 'add_caption_image':
			return chain()
				.insertContent({
					type: 'documentCaption',
					attrs: { kind: 'IMAGE', text: 'текст подписи' },
				})
				.run();
		case 'insert_table':
			return chain().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run();
		case 'image_upload_insert':
			if (!opts.getDocumentId()) return false;
			opts.requestImageUpload('insert');
			return true;
		case 'image_upload_crop':
			if (!opts.getDocumentId()) return false;
			opts.requestImageUpload('crop');
			return true;
		default:
			return false;
	}
}

export const EditorHotkeysTipTap = Extension.create<EditorHotkeysTipTapOptions>({
	name: 'editorHotkeys',

	priority: 2000,

	addOptions() {
		return {
			getBindings: () => ({} as Record<EditorHotkeyActionId, EditorHotkeyChord | null>),
			openFormulaBuilder: ((_opts) => {}) as OpenFormulaBuilderFn,
			requestImageUpload: () => {},
			getDocumentId: () => undefined,
		};
	},

	addProseMirrorPlugins() {
		const editor = this.editor;
		const opts = this.options;

		return [
			new Plugin({
				key: new PluginKey('editorHotkeys'),
				props: {
					handleKeyDown(_view, event) {
						if (event.defaultPrevented || !editor.isEditable) {
							return false;
						}
						if (isModifierOnlyCode(event.code)) {
							return false;
						}

						const bindings = opts.getBindings();
						for (const id of catalogIdsForPayload()) {
							const chord = bindings[id];
							if (chord == null) {
								continue;
							}
							if (!hotkeyChordMatches(event, chord)) {
								continue;
							}
							const ok = applyHotkey(editor, id, opts);
							if (ok) {
								event.preventDefault();
								return true;
							}
						}
						return false;
					},
				},
			}),
		];
	},
});
