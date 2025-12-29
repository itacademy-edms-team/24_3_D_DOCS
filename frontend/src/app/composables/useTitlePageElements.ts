import { computed, type Ref } from 'vue';
import { computedEager } from '@vueuse/core';
import { Canvas, Text, Line, type FabricObject } from 'fabric';
import { MM_TO_PX, FONT_FAMILIES, PX_TO_PT, PT_TO_PX } from '@/entities/title-page/constants';

/**
 * Composable for managing title page elements and their properties
 */
export function useTitlePageElements(
	selectedElement: Ref<FabricObject | null>,
	canvas: Ref<Canvas | null>,
	onSave?: () => void
) {
	/**
	 * Check if selected element is text or variable
	 */
	const isTextOrVariable = computedEager(() => {
		if (!selectedElement.value) return false;
		const type = selectedElement.value.type;
		return type === 'text' || type === 'i-text' || type === 'textbox';
	});

	/**
	 * Check if selected element is a variable
	 */
	const isVariable = computedEager(() => {
		return selectedElement.value && (selectedElement.value as any).isVariable === true;
	});

	/**
	 * Check if selected element is a line
	 */
	const isLine = computedEager(() => {
		return selectedElement.value?.type === 'line';
	});

	/**
	 * Element X position in mm
	 */
	const elementX = computed(() => {
		if (!selectedElement.value) return 0;
		return Math.round((selectedElement.value.left! / MM_TO_PX) * 10) / 10;
	});

	/**
	 * Element Y position in mm
	 */
	const elementY = computed(() => {
		if (!selectedElement.value) return 0;
		return Math.round((selectedElement.value.top! / MM_TO_PX) * 10) / 10;
	});

	/**
	 * Element text content
	 */
	const elementText = computed(() => {
		if (!selectedElement.value || !isTextOrVariable.value) return '';
		const textObj = selectedElement.value as Text;
		return textObj.text || '';
	});

	/**
	 * Element font size (in points for display)
	 */
	const elementFontSize = computed(() => {
		if (!selectedElement.value || !isTextOrVariable.value) return 14;
		const textObj = selectedElement.value as Text;
		// Convert from pixels (Fabric.js) to points (for display)
		const fontSizePx = textObj.fontSize || 14;
		return Math.round(fontSizePx * PX_TO_PT * 10) / 10;
	});

	/**
	 * Element font family
	 */
	const elementFontFamily = computed(() => {
		if (!selectedElement.value || !isTextOrVariable.value) return 'Arial';
		const textObj = selectedElement.value as Text;
		return textObj.fontFamily || 'Arial';
	});

	/**
	 * Element font weight
	 */
	const elementFontWeight = computed(() => {
		if (!selectedElement.value || !isTextOrVariable.value) return 'normal';
		const textObj = selectedElement.value as Text;
		return textObj.fontWeight || 'normal';
	});

	/**
	 * Element font style
	 */
	const elementFontStyle = computed(() => {
		if (!selectedElement.value || !isTextOrVariable.value) return 'normal';
		const textObj = selectedElement.value as Text;
		return textObj.fontStyle || 'normal';
	});

	/**
	 * Element fill color
	 */
	const elementFill = computed(() => {
		if (!selectedElement.value || !isTextOrVariable.value) return '#000000';
		const textObj = selectedElement.value as Text;
		return (textObj.fill as string) || '#000000';
	});

	/**
	 * Element line height
	 */
	const elementLineHeight = computed(() => {
		if (!selectedElement.value || !isTextOrVariable.value) return 1.2;
		const textObj = selectedElement.value as Text;
		return textObj.lineHeight || 1.2;
	});

	/**
	 * Element text align
	 */
	const elementTextAlign = computed(() => {
		if (!selectedElement.value || !isTextOrVariable.value) return 'left';
		const textObj = selectedElement.value as Text;
		return textObj.textAlign || 'left';
	});

	/**
	 * Element width in mm
	 */
	const elementWidth = computed(() => {
		if (!selectedElement.value || !isTextOrVariable.value) return undefined;
		const textObj = selectedElement.value as Text;
		if (textObj.width && (textObj.scaleX || 1)) {
			return Math.round((textObj.width * (textObj.scaleX || 1)) / MM_TO_PX * 10) / 10;
		}
		return undefined;
	});

	/**
	 * Element max lines
	 */
	const elementMaxLines = computed(() => {
		if (!selectedElement.value || !isTextOrVariable.value) return undefined;
		return (selectedElement.value as any).maxLines;
	});

	/**
	 * Line length in mm
	 */
	const elementLength = computed(() => {
		if (!selectedElement.value || !isLine.value) return 100;
		const lineObj = selectedElement.value as Line;
		const x1 = lineObj.x1! / MM_TO_PX;
		const y1 = lineObj.y1! / MM_TO_PX;
		const x2 = lineObj.x2! / MM_TO_PX;
		const y2 = lineObj.y2! / MM_TO_PX;
		return Math.round(Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2)) * 10) / 10;
	});

	/**
	 * Line thickness in mm
	 */
	const elementThickness = computed(() => {
		if (!selectedElement.value || !isLine.value) return 1;
		const lineObj = selectedElement.value as Line;
		return Math.round((lineObj.strokeWidth! / MM_TO_PX) * 10) / 10;
	});

	/**
	 * Line style
	 */
	const elementLineStyle = computed(() => {
		if (!selectedElement.value || !isLine.value) return 'solid';
		const lineObj = selectedElement.value as Line;
		return (lineObj as any).strokeDashArray ? 'dashed' : 'solid';
	});

	/**
	 * Line stroke color
	 */
	const elementStroke = computed(() => {
		if (!selectedElement.value || !isLine.value) return '#000000';
		const lineObj = selectedElement.value as Line;
		return (lineObj.stroke as string) || '#000000';
	});

	/**
	 * Update element position
	 */
	const updateElementPosition = (axis: 'x' | 'y', event: Event) => {
		if (!selectedElement.value || !canvas.value) return;
		const input = event.target as HTMLInputElement;
		const value = parseFloat(input.value);
		if (isNaN(value)) return;
		
		const newPos = value * MM_TO_PX;
		if (axis === 'x') {
			selectedElement.value.set('left', newPos);
		} else {
			selectedElement.value.set('top', newPos);
		}
		canvas.value.renderAll();
		onSave?.();
	};

	/**
	 * Update element text
	 */
	const updateElementText = (event: Event) => {
		if (!selectedElement.value || !canvas.value || !isTextOrVariable.value) return;
		const input = event.target as HTMLInputElement;
		const textObj = selectedElement.value as Text;
		// Сохраняем customWidth перед изменением текста
		const customWidth = (textObj as any).customWidth;
		textObj.set('text', input.value);
		// Восстанавливаем ширину, если она была установлена явно
		if (customWidth) {
			textObj.set({ width: customWidth, scaleX: 1 });
			(textObj as any).customWidth = customWidth;
			textObj.setCoords();
		}
		canvas.value.renderAll();
		onSave?.();
	};

	/**
	 * Update element property
	 */
	const updateElementProperty = (property: string, event: Event) => {
		if (!selectedElement.value || !canvas.value) return;
		const input = event.target as HTMLInputElement;
		let value: any = input.value;
		
		if (property === 'fontSize' || property === 'lineHeight') {
			value = parseFloat(value);
			if (isNaN(value)) return;
			
			// Convert fontSize from points (user input) to pixels (Fabric.js)
			if (property === 'fontSize') {
				value = value * PT_TO_PX;
			}
		}
		
		// Preserve customWidth when changing textAlign (similar to updateElementText)
		if (property === 'textAlign' && isTextOrVariable.value) {
			const textObj = selectedElement.value as Text;
			const customWidth = (textObj as any).customWidth;
			textObj.set(property as any, value);
			// Restore width if it was explicitly set
			if (customWidth) {
				textObj.set({ width: customWidth, scaleX: 1 });
				(textObj as any).customWidth = customWidth;
				textObj.setCoords();
			}
		} else {
			selectedElement.value.set(property as any, value);
		}
		
		canvas.value.renderAll();
		onSave?.();
	};

	/**
	 * Update element width
	 */
	const updateElementWidth = (event: Event) => {
		if (!selectedElement.value || !canvas.value || !isTextOrVariable.value) return;
		const input = event.target as HTMLInputElement;
		const textObj = selectedElement.value as Text;
		
		if (!input.value || input.value.trim() === '') {
			// Reset width if empty - удаляем customWidth
			textObj.set({ width: undefined, scaleX: 1 });
			delete (textObj as any).customWidth;
			textObj.setCoords(); // Обновляем bounding box
			canvas.value.renderAll();
			onSave?.();
			return;
		}
		
		const widthMm = parseFloat(input.value);
		if (isNaN(widthMm) || widthMm <= 0) return;
		
		const widthPx = widthMm * MM_TO_PX;
		textObj.set({ width: widthPx, scaleX: 1 });
		(textObj as any).customWidth = widthPx; // Сохраняем явно установленную ширину
		textObj.setCoords(); // Обновляем bounding box
		canvas.value.renderAll();
		onSave?.();
	};

	/**
	 * Update element max lines
	 */
	const updateElementMaxLines = (event: Event) => {
		if (!selectedElement.value || !canvas.value || !isTextOrVariable.value) return;
		const input = event.target as HTMLInputElement;
		
		if (!input.value || input.value.trim() === '') {
			// Remove maxLines if empty
			delete (selectedElement.value as any).maxLines;
			canvas.value.renderAll();
			onSave?.();
			return;
		}
		
		const maxLines = parseInt(input.value, 10);
		if (isNaN(maxLines) || maxLines < 1) return;
		
		(selectedElement.value as any).maxLines = maxLines;
		canvas.value.renderAll();
		onSave?.();
	};

	/**
	 * Update line length
	 */
	const updateLineLength = (event: Event) => {
		if (!selectedElement.value || !canvas.value || !isLine.value) return;
		const input = event.target as HTMLInputElement;
		const length = parseFloat(input.value);
		if (isNaN(length)) return;
		
		const lineObj = selectedElement.value as Line;
		const x1 = lineObj.x1!;
		const y1 = lineObj.y1!;
		const angle = Math.atan2((lineObj.y2! - y1), (lineObj.x2! - x1));
		const newX2 = x1 + length * MM_TO_PX * Math.cos(angle);
		const newY2 = y1 + length * MM_TO_PX * Math.sin(angle);
		
		lineObj.set({ x2: newX2, y2: newY2 });
		canvas.value.renderAll();
		onSave?.();
	};

	/**
	 * Update line thickness
	 */
	const updateLineThickness = (event: Event) => {
		if (!selectedElement.value || !canvas.value || !isLine.value) return;
		const input = event.target as HTMLInputElement;
		const thickness = parseFloat(input.value);
		if (isNaN(thickness)) return;
		
		const lineObj = selectedElement.value as Line;
		
		// Save line coordinates before changing thickness to prevent position shift
		const x1 = lineObj.x1!;
		const y1 = lineObj.y1!;
		const x2 = lineObj.x2!;
		const y2 = lineObj.y2!;
		
		// Update stroke width
		lineObj.set('strokeWidth', thickness * MM_TO_PX);
		
		// Restore line coordinates to prevent shift
		lineObj.set({ x1, y1, x2, y2 });
		
		// Update coords to recalculate bounding box
		lineObj.setCoords();
		
		canvas.value.renderAll();
		onSave?.();
	};

	/**
	 * Update line style
	 */
	const updateLineStyle = (event: Event) => {
		if (!selectedElement.value || !canvas.value || !isLine.value) return;
		const select = event.target as HTMLSelectElement;
		const lineObj = selectedElement.value as Line;
		
		if (select.value === 'dashed') {
			lineObj.set('strokeDashArray', [5, 5]);
		} else {
			lineObj.set('strokeDashArray', undefined);
		}
		canvas.value.renderAll();
		onSave?.();
	};

	return {
		// Computed properties
		isTextOrVariable,
		isVariable,
		isLine,
		elementX,
		elementY,
		elementText,
		elementFontSize,
		elementFontFamily,
		elementFontWeight,
		elementFontStyle,
		elementFill,
		elementLineHeight,
		elementTextAlign,
		elementWidth,
		elementMaxLines,
		elementLength,
		elementThickness,
		elementLineStyle,
		elementStroke,
		// Update functions
		updateElementPosition,
		updateElementText,
		updateElementProperty,
		updateElementWidth,
		updateElementMaxLines,
		updateLineLength,
		updateLineThickness,
		updateLineStyle,
		// Constants
		fontFamilies: FONT_FAMILIES,
	};
}
