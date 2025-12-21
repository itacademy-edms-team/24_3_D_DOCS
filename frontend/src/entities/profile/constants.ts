export type EntityType =
	| 'paragraph'
	| 'heading'
	| 'image'
	| 'image-caption'
	| 'ordered-list'
	| 'unordered-list'
	| 'table'
	| 'table-caption'
	| 'formula'
	| 'formula-caption';

export const ALL_ENTITY_TYPES: EntityType[] = [
	'paragraph',
	'heading',
	'image',
	'image-caption',
	'ordered-list',
	'unordered-list',
	'table',
	'table-caption',
	'formula',
	'formula-caption',
];

export const ENTITY_LABELS: Record<EntityType, string> = {
	paragraph: 'Параграф',
	heading: 'Заголовок',
	image: 'Изображение',
	'image-caption': 'Подпись к изображению',
	'ordered-list': 'Нумерованный список',
	'unordered-list': 'Маркированный список',
	table: 'Таблица',
	'table-caption': 'Подпись к таблице',
	formula: 'Формула',
	'formula-caption': 'Подпись к формуле',
};

export const FONT_FAMILIES = [
	{ value: 'Times New Roman', label: 'Times New Roman' },
	{ value: 'Arial', label: 'Arial' },
	{ value: 'Calibri', label: 'Calibri' },
	{ value: 'Georgia', label: 'Georgia' },
	{ value: 'Verdana', label: 'Verdana' },
	{ value: 'Courier New', label: 'Courier New' },
];
