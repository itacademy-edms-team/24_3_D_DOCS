import type { EditorHotkeyActionId, EditorHotkeyChord } from './editorHotkeyCatalog';

function platformMod(): Pick<EditorHotkeyChord, 'ctrlKey' | 'metaKey'> {
	if (typeof navigator === 'undefined') {
		return { ctrlKey: true, metaKey: false };
	}
	const mac = /Mac|iPhone|iPod|iPad/i.test(navigator.platform ?? '');
	return mac ? { ctrlKey: false, metaKey: true } : { ctrlKey: true, metaKey: false };
}

function chord(p: Omit<EditorHotkeyChord, 'ctrlKey' | 'metaKey'>): EditorHotkeyChord {
	return { ...platformMod(), ...p };
}

/**
 * Если в API для действия null — используем эти сочетания (как в типичных редакторах).
 */
export const EDITOR_HOTKEY_DEFAULTS: Record<EditorHotkeyActionId, EditorHotkeyChord> = {
	toggle_bold: chord({ code: 'KeyB', shiftKey: false, altKey: false }),
	toggle_italic: chord({ code: 'KeyI', shiftKey: false, altKey: false }),
	toggle_list: chord({ code: 'KeyL', shiftKey: true, altKey: false }),
	toggle_highlight: chord({ code: 'KeyH', shiftKey: true, altKey: false }),
	toggle_heading: chord({ code: 'Digit2', shiftKey: false, altKey: true }),
	insert_formula: chord({ code: 'KeyF', shiftKey: false, altKey: true }),
	add_caption_table: chord({ code: 'KeyT', shiftKey: true, altKey: true }),
	add_caption_formula: chord({ code: 'KeyG', shiftKey: true, altKey: true }),
	add_caption_image: chord({ code: 'KeyP', shiftKey: true, altKey: true }),
	insert_table: chord({ code: 'KeyT', shiftKey: true, altKey: false }),
	image_upload_insert: chord({ code: 'KeyU', shiftKey: true, altKey: false }),
	image_upload_crop: chord({ code: 'KeyU', shiftKey: false, altKey: true }),
};
