import {
	catalogIdsForPayload,
	type EditorHotkeyActionId,
	type EditorHotkeyChord,
} from './editorHotkeyCatalog';
import { EDITOR_HOTKEY_DEFAULTS } from './editorHotkeyDefaults';

/**
 * Ответ API (null = не задано) + заполнение пропусков дефолтами.
 */
export function effectiveEditorHotkeyBindings(
	api: Record<string, EditorHotkeyChord | null>,
): Record<EditorHotkeyActionId, EditorHotkeyChord> {
	const out = {} as Record<EditorHotkeyActionId, EditorHotkeyChord>;
	for (const id of catalogIdsForPayload()) {
		const v = api[id];
		if (v && typeof v.code === 'string' && v.code.length > 0) {
			out[id] = v;
		} else {
			out[id] = EDITOR_HOTKEY_DEFAULTS[id];
		}
	}
	return out;
}
