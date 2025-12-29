import type { TitlePageWithData } from '@/entities/title-page/api/TitlePageAPI';

interface TitlePageElement {
	id?: string;
	type: 'text' | 'variable' | 'line';
	x: number; // мм от левого края
	y: number; // мм от верхнего края
	width?: number; // мм
	height?: number; // мм
	
	// Для текста и переменных
	text?: string;
	variableType?: string; // Для переменных: Title, Author, Year, Group, etc. (устаревшее, используется text)
	format?: string; // Для переменных: "г. {city}, {year}"
	
	// Стили текста
	fontFamily?: string;
	fontSize?: number; // px (в Fabric.js), конвертируется в pt в рендерере
	fontWeight?: string; // normal, bold
	fontStyle?: string; // normal, italic
	textAlign?: string; // left, center, right
	lineHeight?: number;
	allCaps?: boolean; // Все заглавные
	fill?: string; // Цвет текста
	color?: string; // Цвет текста (альтернатива fill)
	maxLines?: number; // Максимальное количество строк (для line-clamp)
	
	// Для линий
	length?: number; // мм
	thickness?: number; // мм или pt
	lineStyle?: string; // solid, dashed
	stretchToPageWidth?: boolean; // Растянуть по ширине страницы
	stroke?: string; // Цвет линии
}

const A4_WIDTH_MM = 210;
const A4_HEIGHT_MM = 297;
const MM_TO_PX = 3.7795275591;
const PX_TO_MM = 1 / MM_TO_PX;
const PX_TO_PT = 72 / 96; // 0.75
const PT_TO_MM = 25.4 / 72; // Конвертация пунктов в миллиметры: 1pt = 25.4/72mm

/**
 * Измеряет ширину текста с использованием Canvas API
 */
function measureTextWidth(
	text: string,
	fontSize: number,
	fontFamily: string,
	fontWeight: string,
	fontStyle: string
): number {
	const canvas = document.createElement('canvas');
	const ctx = canvas.getContext('2d');
	if (!ctx) return 0;
	
	// fontSize уже в пунктах (pt), используем напрямую
	ctx.font = `${fontStyle} ${fontWeight} ${fontSize}pt ${fontFamily}`;
	const metrics = ctx.measureText(text);
	return metrics.width;
}

/**
 * Рендерит элемент титульника в HTML
 */
function renderElement(element: TitlePageElement, variables: Record<string, string>): string {
	const positionStyle = `position: absolute; left: ${element.x}mm; top: ${element.y}mm;`;
	
	if (element.type === 'line') {
		// Рендеринг линии
		const lineColor = element.stroke || element.color || '#000';
		let lineStyle = `border-bottom: ${element.thickness ?? 1}mm ${element.lineStyle ?? 'solid'} ${lineColor};`;
		
		if (element.stretchToPageWidth) {
			lineStyle += ' width: 100%;';
		} else if (element.length) {
			lineStyle += ` width: ${element.length}mm;`;
		}
		
		return `<div style="${positionStyle} ${lineStyle}"></div>`;
	}
	
	// Рендеринг текста или переменной
	let content = '';
	let isEmptyVariable = false;
	
	if (element.type === 'variable') {
		// Извлекаем ключ переменной из text (убираем фигурные скобки если есть)
		const variableKey = element.text 
			? element.text.replace(/[{}]/g, '').trim() 
			: (element.variableType || '');
		
		// Получаем значение переменной
		const variableValue = variableKey ? (variables[variableKey] || '') : '';
		
		// Если значение пустое, показываем alt-текст
		if (!variableValue) {
			const displayKey = variableKey || 'Переменная';
			content = `${displayKey} не задана`;
			isEmptyVariable = true;
		} else {
			// Если есть формат, применяем его
			if (element.format && variableValue) {
				content = element.format.replace(/\{(\w+)\}/g, (match, key) => {
					return variables[key] || match;
				});
			} else {
				content = variableValue;
			}
		}
	} else {
		// Обычный текст
		content = element.text || '';
	}
	
	// Применяем allCaps если нужно
	if (element.allCaps) {
		content = content.toUpperCase();
	}
	
	// Проверяем, является ли текст многострочным
	const lines = content.split('\n');
	const isMultiline = lines.length > 1;
	
	// Если задано maxLines, используем старый подход с line-clamp
	if (element.maxLines && element.maxLines > 0) {
		const styles: string[] = [positionStyle];
		styles.push(`display: -webkit-box;`);
		styles.push(`-webkit-line-clamp: ${element.maxLines};`);
		styles.push(`-webkit-box-orient: vertical;`);
		styles.push(`overflow: hidden;`);
		
		// Применяем ширину если задана
		if (element.width) {
			styles.push(`width: ${element.width}mm;`);
		} else if (element.textAlign === 'right') {
			// Для правого выравнивания вычисляем ширину до правого края листа
			const widthToRight = A4_WIDTH_MM - element.x;
			styles.push(`width: ${widthToRight}mm;`);
		}
		
		if (element.height) {
			styles.push(`height: ${element.height}mm;`);
		}
		if (element.fontFamily) {
			styles.push(`font-family: ${element.fontFamily};`);
		}
		if (element.fontSize) {
			// fontSize уже в пунктах (pt), используем напрямую
			styles.push(`font-size: ${element.fontSize}pt;`);
		}
		if (element.fontWeight) {
			styles.push(`font-weight: ${element.fontWeight};`);
		}
		if (element.fontStyle) {
			styles.push(`font-style: ${element.fontStyle};`);
		}
		if (element.textAlign) {
			styles.push(`text-align: ${element.textAlign};`);
		}
		if (element.lineHeight) {
			styles.push(`line-height: ${element.lineHeight};`);
		}
		
		// Применяем цвет текста
		if (isEmptyVariable) {
			styles.push('color: #999 !important;');
			if (!element.fontStyle || element.fontStyle === 'normal') {
				styles.push('font-style: italic;');
			}
		} else {
			const textColor = element.fill || element.color || '#000000';
			styles.push(`color: ${textColor} !important;`);
		}
		
		const styleString = styles.join(' ');
		return `<div style="${styleString}">${escapeHtml(content)}</div>`;
	}
	
	// Для многострочного текста рендерим каждую строку отдельно
	if (isMultiline) {
		const fontSize = element.fontSize || 16;
		const lineHeightMultiplier = element.lineHeight || 1.2;
		// fontSize уже в пунктах (pt), используем напрямую
		const fontSizePt = fontSize;
		const lineHeightPt = fontSizePt * lineHeightMultiplier;
		const textAlign = element.textAlign || 'left';
		const fontFamily = element.fontFamily || 'Times New Roman';
		const fontWeight = element.fontWeight || 'normal';
		const fontStyle = element.fontStyle || 'normal';
		
		// Используем element.x как есть — это левый край bounding box в Fabric.js
		const containerLeft = element.x;
		
		// Определяем ширину контейнера
		let containerWidth = element.width;
		if (!containerWidth) {
			// Если ширина не задана, вычисляем на основе максимальной ширины строк
			let maxWidth = 0;
			lines.forEach(line => {
				const lineWidth = measureTextWidth(line, fontSize, fontFamily, fontWeight, fontStyle);
				if (lineWidth > maxWidth) maxWidth = lineWidth;
			});
			containerWidth = maxWidth / MM_TO_PX;
		}
		
		// Рендерим каждую строку с одинаковым left
		const linesHtml = lines.map((line, i) => {
			const lineTop = element.y + (i * lineHeightPt * PT_TO_MM);
			
			const lineStyles: string[] = [];
			lineStyles.push(`position: absolute;`);
			lineStyles.push(`left: ${containerLeft}mm;`);
			lineStyles.push(`top: ${lineTop}mm;`);
			lineStyles.push(`width: ${containerWidth}mm;`);
			
			if (fontFamily) {
				lineStyles.push(`font-family: ${fontFamily};`);
			}
			lineStyles.push(`font-size: ${fontSizePt}pt;`);
			if (fontWeight) {
				lineStyles.push(`font-weight: ${fontWeight};`);
			}
			if (fontStyle) {
				lineStyles.push(`font-style: ${fontStyle};`);
			}
			lineStyles.push(`text-align: ${textAlign};`);
			lineStyles.push(`white-space: nowrap;`);
			lineStyles.push(`overflow: hidden;`);
			
			if (isEmptyVariable) {
				lineStyles.push('color: #999 !important;');
				if (!fontStyle || fontStyle === 'normal') {
					lineStyles.push('font-style: italic;');
				}
			} else {
				const textColor = element.fill || element.color || '#000000';
				lineStyles.push(`color: ${textColor} !important;`);
			}
			
			const lineStyleString = lineStyles.join(' ');
			return `<div style="${lineStyleString}">${escapeHtml(line)}</div>`;
		}).join('\n');
		
		return linesHtml;
	}
	
	// Для однострочного текста
	const styles: string[] = [];
	
	// Используем element.x как есть — это левый край bounding box в Fabric.js
	styles.push(`position: absolute; left: ${element.x}mm; top: ${element.y}mm;`);
	
	// Устанавливаем ширину если задана
	if (element.width) {
		styles.push(`width: ${element.width}mm;`);
		styles.push(`overflow: hidden;`);
	}
	
	// Добавляем text-align для выравнивания внутри контейнера
	if (element.textAlign) {
		styles.push(`text-align: ${element.textAlign};`);
	}
	
	// Для однострочного текста предотвращаем перенос
	styles.push('white-space: nowrap;');
	
	if (element.height) {
		styles.push(`height: ${element.height}mm;`);
	}
	if (element.fontFamily) {
		styles.push(`font-family: ${element.fontFamily};`);
	}
	if (element.fontSize) {
		// fontSize уже в пунктах (pt), используем напрямую
		styles.push(`font-size: ${element.fontSize}pt;`);
	}
	if (element.fontWeight) {
		styles.push(`font-weight: ${element.fontWeight};`);
	}
	if (element.fontStyle) {
		styles.push(`font-style: ${element.fontStyle};`);
	}
	if (element.lineHeight) {
		styles.push(`line-height: ${element.lineHeight};`);
	}
	
	// Применяем цвет текста
	if (isEmptyVariable) {
		styles.push('color: #999 !important;');
		if (!element.fontStyle || element.fontStyle === 'normal') {
			styles.push('font-style: italic;');
		}
	} else {
		const textColor = element.fill || element.color || '#000000';
		styles.push(`color: ${textColor} !important;`);
	}
	
	const styleString = styles.join(' ');
	return `<div style="${styleString}">${escapeHtml(content)}</div>`;
}

/**
 * Экранирует HTML символы
 */
function escapeHtml(text: string): string {
	const div = document.createElement('div');
	div.textContent = text;
	return div.innerHTML;
}

/**
 * Рендерит титульник в HTML
 */
export function renderTitlePageToHtml(
	titlePage: TitlePageWithData,
	variables: Record<string, string> = {}
): string {
	if (!titlePage?.data?.elements) {
		return '';
	}
	
	const html: string[] = [];
	html.push(`<div class="title-page" style="position: relative; width: ${A4_WIDTH_MM}mm; height: ${A4_HEIGHT_MM}mm; font-family: 'Times New Roman', Times, serif; margin: 0; padding: 0;">`);
	
	for (const element of titlePage.data.elements) {
		html.push(renderElement(element as TitlePageElement, variables));
	}
	
	html.push('</div>');
	
	return html.join('\n');
}

/**
 * Извлекает список переменных из титульника
 */
export function extractTitlePageVariables(titlePage: TitlePageWithData): string[] {
	if (!titlePage?.data?.elements) {
		return [];
	}
	
	const variableTypes = new Set<string>();
	
	for (const element of titlePage.data.elements) {
		if (element.type === 'variable') {
			// Извлекаем ключ из text (убираем фигурные скобки если есть)
			if (element.text) {
				const key = element.text.replace(/[{}]/g, '').trim();
				if (key) {
					variableTypes.add(key);
				}
			}
			// Также проверяем устаревшее поле variableType для обратной совместимости
			if ((element as any).variableType) {
				variableTypes.add((element as any).variableType);
			}
		}
	}
	
	return Array.from(variableTypes);
}
