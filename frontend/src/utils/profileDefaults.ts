import type { ProfileData, EntityStyle } from '@/entities/profile/types';

/**
 * Get default profile data according to GOST standards
 * Returns ProfileData with all default values for styles
 */
export function getDefaultProfileData(): ProfileData {
	const defaultFontFamily = 'Times New Roman';
	const defaultFontSize = 14; // pt
	const defaultLineHeight = 1.5; // ГОСТ

	return {
		pageSettings: {
			size: 'A4',
			orientation: 'portrait',
			margins: {
				top: 20,
				right: 20,
				bottom: 20,
				left: 20,
			},
			pageNumbers: {
				enabled: true,
				position: 'bottom',
				align: 'center',
				format: '{n}',
				fontSize: 12,
				fontStyle: 'normal',
				fontFamily: defaultFontFamily,
			},
			globalLineHeight: defaultLineHeight, // ГОСТ
		},
		entityStyles: {
			// Параграф - ГОСТ
			paragraph: {
				fontFamily: defaultFontFamily,
				fontSize: defaultFontSize,
				fontWeight: 'normal',
				fontStyle: 'normal',
				textAlign: 'justify', // По ширине
				textIndent: 1.25, // см, ГОСТ
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true, // Использовать глобальный по умолчанию
				marginTop: 0,
				marginBottom: 0,
			} as EntityStyle,

			// Заголовки H1-H6 - ГОСТ
			'heading-1': {
				fontFamily: defaultFontFamily,
				fontSize: 18, // pt
				fontWeight: 'bold',
				fontStyle: 'normal',
				textAlign: 'center',
				textIndent: 0,
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 12,
				marginBottom: 12,
			} as EntityStyle,

			'heading-2': {
				fontFamily: defaultFontFamily,
				fontSize: 16,
				fontWeight: 'bold',
				fontStyle: 'normal',
				textAlign: 'center',
				textIndent: 0,
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 10,
				marginBottom: 10,
			} as EntityStyle,

			'heading-3': {
				fontFamily: defaultFontFamily,
				fontSize: 15,
				fontWeight: 'bold',
				fontStyle: 'normal',
				textAlign: 'left',
				textIndent: 0,
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 8,
				marginBottom: 8,
			} as EntityStyle,

			'heading-4': {
				fontFamily: defaultFontFamily,
				fontSize: 14,
				fontWeight: 'bold',
				fontStyle: 'normal',
				textAlign: 'left',
				textIndent: 0,
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 6,
				marginBottom: 6,
			} as EntityStyle,

			'heading-5': {
				fontFamily: defaultFontFamily,
				fontSize: 13,
				fontWeight: 'bold',
				fontStyle: 'normal',
				textAlign: 'left',
				textIndent: 0,
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 6,
				marginBottom: 6,
			} as EntityStyle,

			'heading-6': {
				fontFamily: defaultFontFamily,
				fontSize: 12,
				fontWeight: 'bold',
				fontStyle: 'normal',
				textAlign: 'left',
				textIndent: 0,
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 6,
				marginBottom: 6,
			} as EntityStyle,

			// Списки - ГОСТ
			'ordered-list': {
				fontFamily: defaultFontFamily,
				fontSize: defaultFontSize,
				fontWeight: 'normal',
				fontStyle: 'normal',
				textAlign: 'left',
				textIndent: 1.25, // см, красная строка для первого элемента (когда не из параграфа)
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 6,
				marginBottom: 6,
				listAdditionalIndent: 0,
				listUseParagraphTextIndent: true, // Использовать красную строку из параграфа по умолчанию
			} as EntityStyle,

			'unordered-list': {
				fontFamily: defaultFontFamily,
				fontSize: defaultFontSize,
				fontWeight: 'normal',
				fontStyle: 'normal',
				textAlign: 'left',
				textIndent: 1.25, // см, красная строка для первого элемента (когда не из параграфа)
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 6,
				marginBottom: 6,
				listAdditionalIndent: 0,
				listUseParagraphTextIndent: true,
			} as EntityStyle,

			// Таблицы - ГОСТ
			table: {
				fontFamily: defaultFontFamily,
				fontSize: defaultFontSize,
				fontWeight: 'normal',
				fontStyle: 'normal',
				textAlign: 'center',
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 6,
				marginBottom: 6,
				borderWidth: 1,
				borderColor: '#000000',
				borderStyle: 'solid',
			} as EntityStyle,

			// Изображения - ГОСТ
			image: {
				textAlign: 'center',
				maxWidth: 100,
				marginTop: 6,
				marginBottom: 6,
			} as EntityStyle,

			// Формулы - ГОСТ
			formula: {
				textAlign: 'center',
				marginTop: 6,
				marginBottom: 6,
			} as EntityStyle,

			// Подписи - ГОСТ
			'image-caption': {
				fontFamily: defaultFontFamily,
				fontSize: defaultFontSize,
				fontWeight: 'normal',
				fontStyle: 'normal',
				textAlign: 'center',
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 0,
				marginBottom: 12,
				captionFormat: 'Рисунок {n} - {content}',
			} as EntityStyle,

			'table-caption': {
				fontFamily: defaultFontFamily,
				fontSize: defaultFontSize,
				fontWeight: 'normal',
				fontStyle: 'normal',
				textAlign: 'center',
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 0,
				marginBottom: 12,
				captionFormat: 'Таблица {n} - {content}',
			} as EntityStyle,

			'formula-caption': {
				fontFamily: defaultFontFamily,
				fontSize: defaultFontSize,
				fontWeight: 'normal',
				fontStyle: 'normal',
				textAlign: 'center',
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
				marginTop: 0,
				marginBottom: 12,
				captionFormat: 'Формула {n} - {content}',
			} as EntityStyle,

			// Выделенный текст
			highlight: {
				highlightColor: '#000000',
				highlightBackgroundColor: '#ffeb3b',
			} as EntityStyle,

			// Код
			'code-block': {
				fontFamily: 'Courier New',
				fontSize: 12,
				backgroundColor: '#f5f5f5',
				marginTop: 6,
				marginBottom: 6,
				lineHeight: defaultLineHeight,
				lineHeightUseGlobal: true,
			} as EntityStyle,
		},
		headingNumbering: {
			templates: {
				1: { format: '{n} {content}', enabled: false },
				2: { format: '{n} {content}', enabled: false },
				3: { format: '{n} {content}', enabled: false },
				4: { format: '{n} {content}', enabled: false },
				5: { format: '{n} {content}', enabled: false },
				6: { format: '{n} {content}', enabled: false },
			},
		},
		tableOfContents: {
			fontStyle: 'normal',
			fontWeight: 'normal',
			fontSize: 14,
			indentPerLevel: 5,
			nestingEnabled: true,
			numberingEnabled: true,
		},
	};
}
