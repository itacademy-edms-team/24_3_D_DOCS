import {
	catalogIdsForPayload,
	type EditorHotkeyActionId,
	type EditorHotkeyChord,
} from './editorHotkeyCatalog';

function isValidChord(v: unknown): v is EditorHotkeyChord {
	return (
		typeof v === 'object' &&
		v !== null &&
		typeof (v as EditorHotkeyChord).code === 'string' &&
		(v as EditorHotkeyChord).code.trim().length > 0
	);
}

/** Нормализация ответа API: только явно заданные аккорды, иначе `null`. */
export function normalizeEditorHotkeyApiBindings(
	api: Record<string, EditorHotkeyChord | null | undefined>,
): Record<EditorHotkeyActionId, EditorHotkeyChord | null> {
	const out = {} as Record<EditorHotkeyActionId, EditorHotkeyChord | null>;
	for (const id of catalogIdsForPayload()) {
		const v = api[id];
		out[id] = isValidChord(v) ? v : null;
	}
	return out;
}
