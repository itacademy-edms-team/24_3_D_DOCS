import { type Ref } from 'vue';
import { useDebounceFn } from '@vueuse/core';
import { Canvas, Text, Line, type FabricObject } from 'fabric';
import { MM_TO_PX, PX_TO_PT, PT_TO_PX } from '@/entities/title-page/constants';
import { validateAndFixElementBounds } from '@/entities/title-page/utils/elementUtils';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';

/**
 * Composable for saving and loading title page data
 */
export function useTitlePageSave(
	canvas: Ref<Canvas | null>,
	titlePageId: string,
	onHistoryInit?: () => void
) {
	/**
	 * Load title page from API
	 */
	const loadTitlePage = async () => {
		if (!canvas.value) return;
		
		try {
			const titlePage = await TitlePageAPI.getById(titlePageId);
			if (titlePage.data?.elements && Array.isArray(titlePage.data.elements)) {
				// Clear canvas
				canvas.value.clear();
				canvas.value.backgroundColor = 'white';

				// Load elements
				for (const elementData of titlePage.data.elements) {
					if (!elementData || !elementData.type) {
						console.warn('Skipping invalid element:', elementData);
						continue;
					}
					
					try {
						if (elementData.type === 'variable') {
							// Load as variable
							const widthPx = elementData.width ? elementData.width * MM_TO_PX : undefined;
							// Convert fontSize from points (storage) to pixels (Fabric.js)
							const fontSizePt = elementData.fontSize || 12;
							const fontSizePx = fontSizePt * PT_TO_PX;
							const text = new Text(elementData.text || '{Переменная}', {
								left: elementData.x * MM_TO_PX,
								top: elementData.y * MM_TO_PX,
								fontSize: fontSizePx,
								fontFamily: elementData.fontFamily || 'Arial',
								fontWeight: elementData.fontWeight || 'normal',
								fontStyle: elementData.fontStyle || 'italic',
								textAlign: elementData.textAlign || 'left',
								lineHeight: elementData.lineHeight || 1.2,
								fill: elementData.fill || elementData.color || '#0066cc',
							});
							(text as any).id = elementData.id || crypto.randomUUID();
							(text as any).isVariable = true;
							if (widthPx) {
								(text as any).customWidth = widthPx; // Сохраняем customWidth ДО добавления на canvas
							}
							if (elementData.maxLines) {
								(text as any).maxLines = elementData.maxLines;
							}
							canvas.value.add(text);
							if (widthPx) {
								text.set({ width: widthPx, scaleX: 1 });
								text.setCoords();
							}
						} else if (elementData.type === 'text') {
							// Load as regular text
							const widthPx = elementData.width ? elementData.width * MM_TO_PX : undefined;
							// Convert fontSize from points (storage) to pixels (Fabric.js)
							const fontSizePt = elementData.fontSize || 12;
							const fontSizePx = fontSizePt * PT_TO_PX;
							const text = new Text(elementData.text || '', {
								left: elementData.x * MM_TO_PX,
								top: elementData.y * MM_TO_PX,
								fontSize: fontSizePx,
								fontFamily: elementData.fontFamily || 'Arial',
								fontWeight: elementData.fontWeight || 'normal',
								fontStyle: elementData.fontStyle || 'normal',
								textAlign: elementData.textAlign || 'left',
								lineHeight: elementData.lineHeight || 1.2,
								fill: elementData.fill || elementData.color || '#000000',
							});
							(text as any).id = elementData.id || crypto.randomUUID();
							(text as any).isVariable = false;
							if (widthPx) {
								(text as any).customWidth = widthPx; // Сохраняем customWidth ДО добавления на canvas
							}
							if (elementData.maxLines) {
								(text as any).maxLines = elementData.maxLines;
							}
							canvas.value.add(text);
							if (widthPx) {
								text.set({ width: widthPx, scaleX: 1 });
								text.setCoords();
							}
						} else if (elementData.type === 'line') {
							const length = elementData.length || 100;
							const line = new Line(
								[
									elementData.x * MM_TO_PX,
									elementData.y * MM_TO_PX,
									(elementData.x + length) * MM_TO_PX,
									elementData.y * MM_TO_PX,
								],
								{
									stroke: elementData.stroke || elementData.color || '#000000',
									strokeWidth: (elementData.thickness || 1) * MM_TO_PX,
									strokeDashArray: elementData.lineStyle === 'dashed' ? [5, 5] : undefined,
									perPixelTargetFind: true,
									padding: 20,
								}
							);
							(line as any).id = elementData.id || crypto.randomUUID();
							canvas.value.add(line);
						} else {
							console.warn('Unknown element type:', elementData.type, elementData);
						}
					} catch (err) {
						console.error('Error loading element:', err, elementData);
					}
				}

				canvas.value.renderAll();
				
				// Validate and fix element bounds after loading
				const fixedCount = validateAndFixElementBounds(canvas.value);
				if (fixedCount > 0) {
					console.log(`Fixed ${fixedCount} element(s) that were outside page boundaries`);
				}
				
				// Initialize history with loaded state
				onHistoryInit?.();
			}
		} catch (error) {
			console.error('Failed to load title page:', error);
		}
	};

	/**
	 * Save title page to API
	 */
	const saveTitlePage = async () => {
		if (!canvas.value) return;

		// Validate and fix element bounds before saving
		const fixedCount = validateAndFixElementBounds(canvas.value);
		if (fixedCount > 0) {
			console.log(`Fixed ${fixedCount} element(s) that were outside page boundaries before saving`);
		}

		try {
			const elements = canvas.value.getObjects().map((obj) => {
				if (obj.type === 'line') {
					const lineObj = obj as Line;
					const x1 = lineObj.x1! / MM_TO_PX;
					const y1 = lineObj.y1! / MM_TO_PX;
					const x2 = lineObj.x2! / MM_TO_PX;
					const y2 = lineObj.y2! / MM_TO_PX;
					const length = Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2));
					return {
						id: (obj as any).id || crypto.randomUUID(),
						type: 'line',
						x: x1,
						y: y1,
						length: length,
						thickness: (lineObj.strokeWidth || 1) / MM_TO_PX,
						stroke: (lineObj.stroke as string) || '#000000',
						color: (lineObj.stroke as string) || '#000000',
						lineStyle: (lineObj as any).strokeDashArray ? 'dashed' : 'solid',
					};
				}

				const baseElement: any = {
					id: (obj as any).id || crypto.randomUUID(),
					type: obj.type,
					x: obj.left! / MM_TO_PX,
					y: obj.top! / MM_TO_PX,
				};

				if (obj.type === 'text' || obj.type === 'i-text' || obj.type === 'textbox') {
					const textObj = obj as Text;
					const isVariable = (obj as any).isVariable;
					// Сохраняем width только если была установлена явно (customWidth)
					const customWidth = (obj as any).customWidth;
					const width = customWidth 
						? customWidth / MM_TO_PX 
						: undefined;
					const maxLines = (obj as any).maxLines; // Save maxLines if set
					// Convert fontSize from pixels (Fabric.js) to points (for storage)
					const fontSizePx = textObj.fontSize || 12;
					const fontSizePt = fontSizePx * PX_TO_PT;
					
					return {
						...baseElement,
						type: isVariable ? 'variable' : 'text',
						text: textObj.text || '',
						fontSize: fontSizePt,
						fontFamily: textObj.fontFamily,
						fontWeight: textObj.fontWeight,
						fontStyle: textObj.fontStyle,
						textAlign: textObj.textAlign,
						lineHeight: textObj.lineHeight,
						fill: (textObj.fill as string) || '#000000',
						color: (textObj.fill as string) || '#000000',
						width: width, // Save width only if explicitly set
						maxLines: maxLines, // Save maxLines
					};
				}

				return baseElement;
			});

			await TitlePageAPI.update(titlePageId, {
				data: { elements },
			});
		} catch (error) {
			console.error('Failed to save title page:', error);
		}
	};

	/**
	 * Debounced save function
	 */
	const debouncedSaveTitlePage = useDebounceFn(() => {
		saveTitlePage();
	}, 500);

	return {
		loadTitlePage,
		saveTitlePage,
		debouncedSaveTitlePage,
	};
}
