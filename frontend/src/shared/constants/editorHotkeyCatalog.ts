/**
 * Идентификаторы совпадают с бэкендом (EditorHotkeyActionCatalog).
 * Редактор пока не читает эти бинды — только настройки и API.
 */
export type EditorHotkeyActionId =
	| 'toggle_bold'
	| 'toggle_italic'
	| 'toggle_list'
	| 'toggle_highlight'
	| 'toggle_heading'
	| 'insert_formula'
	| 'add_caption_table'
	| 'add_caption_formula'
	| 'add_caption_image'
	| 'insert_table'
	| 'image_upload_insert'
	| 'image_upload_crop';

export interface EditorHotkeyChord {
	code: string;
	ctrlKey: boolean;
	shiftKey: boolean;
	altKey: boolean;
	metaKey: boolean;
}

export interface EditorHotkeyRow {
	id: EditorHotkeyActionId;
	label: string;
}

export interface EditorHotkeySection {
	/** Заголовок группы (например «Добавить подпись»); без заголовка — общий блок. */
	title?: string;
	rows: EditorHotkeyRow[];
}

export const EDITOR_HOTKEY_SECTIONS: EditorHotkeySection[] = [
	{
		rows: [
			{ id: 'toggle_bold', label: 'Изменить Bold' },
			{ id: 'toggle_italic', label: 'Изменить Italic' },
			{ id: 'toggle_list', label: 'Сделать списком' },
			{ id: 'toggle_highlight', label: 'Выделить текст' },
			{ id: 'toggle_heading', label: 'Сделать заголовком' },
			{ id: 'insert_formula', label: 'Добавить формулу' },
		],
	},
	{
		title: 'Добавить подпись',
		rows: [
			{ id: 'add_caption_table', label: 'К таблице' },
			{ id: 'add_caption_formula', label: 'К формуле' },
			{ id: 'add_caption_image', label: 'К изображению' },
		],
	},
	{
		rows: [{ id: 'insert_table', label: 'Добавить таблицу' }],
	},
	{
		title: 'Загрузить изображение',
		rows: [
			{ id: 'image_upload_insert', label: 'Вставка сразу' },
			{ id: 'image_upload_crop', label: 'Crop и вставка' },
		],
	},
];

const MODIFIER_CODES = new Set([
	'ControlLeft',
	'ControlRight',
	'ShiftLeft',
	'ShiftRight',
	'AltLeft',
	'AltRight',
	'MetaLeft',
	'MetaRight',
]);

export function isModifierOnlyCode(code: string): boolean {
	return MODIFIER_CODES.has(code);
}

/** Все id из каталога (с повтором родителя для групп с тем же id — здесь родитель совпадает с первым ребёнком только у «подписи»). */
export function catalogIdsForPayload(): EditorHotkeyActionId[] {
	// Уникальные id в порядке бэкенда — как в EditorHotkeyActionCatalog.All
	return [
		'toggle_bold',
		'toggle_italic',
		'toggle_list',
		'toggle_highlight',
		'toggle_heading',
		'insert_formula',
		'add_caption_table',
		'add_caption_formula',
		'add_caption_image',
		'insert_table',
		'image_upload_insert',
		'image_upload_crop',
	];
}

export function emptyBindingsPayload(): Record<string, EditorHotkeyChord | null> {
	return Object.fromEntries(catalogIdsForPayload().map((id) => [id, null])) as Record<
		string,
		EditorHotkeyChord | null
	>;
}

export function formatHotkeyChord(c: EditorHotkeyChord): string {
	const parts: string[] = [];
	if (c.metaKey) parts.push('Meta');
	if (c.ctrlKey) parts.push('Ctrl');
	if (c.altKey) parts.push('Alt');
	if (c.shiftKey) parts.push('Shift');
	parts.push(humanizeCode(c.code));
	return parts.join('+');
}

function humanizeCode(code: string): string {
	if (code.startsWith('Key')) return code.slice(3);
	if (code.startsWith('Digit')) return code.slice(5);
	if (code === 'Space') return 'Space';
	return code;
}

export function chordFromEvent(e: KeyboardEvent): EditorHotkeyChord {
	return {
		code: e.code,
		ctrlKey: e.ctrlKey,
		shiftKey: e.shiftKey,
		altKey: e.altKey,
		metaKey: e.metaKey,
	};
}
