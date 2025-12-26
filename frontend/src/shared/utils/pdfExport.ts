import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';

/**
 * Экспортирует готовые DOM элементы страниц в PDF
 * @param pageElements - Массив DOM элементов страниц (.document-page)
 * @param filename - Имя файла для сохранения (без расширения)
 * @param options - Опции экспорта
 */
export async function exportPagesToPDF(
	pageElements: HTMLElement[],
	filename: string = 'document',
	options: {
		format?: [number, number]; // [width, height] в мм
		orientation?: 'portrait' | 'landscape';
		margin?: number; // отступы в мм
	} = {}
): Promise<void> {
	if (pageElements.length === 0) {
		throw new Error('Нет страниц для экспорта');
	}

	const {
		format = [210, 297], // A4 по умолчанию
		orientation = 'portrait',
		margin = 0,
	} = options;

	// Создаем PDF документ
	const pdf = new jsPDF({
		orientation: orientation === 'portrait' ? 'portrait' : 'landscape',
		unit: 'mm',
		format: format,
	});

	// Сохраняем оригинальные стили и трансформации
	const originalStyles: Array<{
		element: HTMLElement;
		transform: string;
		opacity: string;
		visibility: string;
		position: string;
		margin: string;
		boxShadow: string;
	}> = [];

	// Находим родительский контейнер с трансформациями (если есть)
	const parentContainer = pageElements[0]?.parentElement;
	const originalParentTransform = parentContainer?.style.transform || '';
	const originalParentWidth = parentContainer?.style.width || '';

	// Временно убираем трансформации у родительского контейнера
	if (parentContainer) {
		parentContainer.style.transform = 'none';
		parentContainer.style.width = 'auto';
	}

	// Функция для оптимизации изображений внутри страниц
	const optimizeImages = async (container: HTMLElement): Promise<void> => {
		const images = Array.from(container.querySelectorAll('img')) as HTMLImageElement[];
		
		// Ждем загрузки всех изображений
		await Promise.all(
			images.map((img) => {
				if (img.complete) return Promise.resolve();
				return new Promise<void>((resolve) => {
					img.onload = () => resolve();
					img.onerror = () => resolve(); // Продолжаем даже при ошибке
					// Таймаут на случай, если изображение не загрузится
					setTimeout(() => resolve(), 5000);
				});
			})
		);

		images.forEach((imgElement) => {
			// Пропускаем, если изображение не загружено или слишком маленькое
			if (!imgElement.complete || imgElement.naturalWidth === 0 || imgElement.naturalHeight === 0) {
				return;
			}

			// Если изображение слишком большое, создаем оптимизированную версию
			if (imgElement.naturalWidth > 2000 || imgElement.naturalHeight > 2000) {
				const canvas = document.createElement('canvas');
				const ctx = canvas.getContext('2d');
				if (!ctx) return;

				// Ограничиваем максимальный размер до 2000px по большей стороне
				let width = imgElement.naturalWidth;
				let height = imgElement.naturalHeight;
				const maxSize = 2000;
				
				if (width > height) {
					if (width > maxSize) {
						height = (height * maxSize) / width;
						width = maxSize;
					}
				} else {
					if (height > maxSize) {
						width = (width * maxSize) / height;
						height = maxSize;
					}
				}

				canvas.width = width;
				canvas.height = height;
				ctx.drawImage(imgElement, 0, 0, width, height);
				
				// Заменяем src на оптимизированное изображение с высоким качеством
				imgElement.src = canvas.toDataURL('image/jpeg', 0.95);
			}
		});
	};

	// Оптимизируем изображения во всех страницах перед экспортом
	for (const element of pageElements) {
		await optimizeImages(element as HTMLElement);
	}

	// Подготавливаем элементы для экспорта
	pageElements.forEach((element) => {
		const htmlElement = element as HTMLElement;
		
		// Сохраняем текущие стили
		const computedStyle = window.getComputedStyle(htmlElement);
		originalStyles.push({
			element: htmlElement,
			transform: computedStyle.transform,
			opacity: computedStyle.opacity,
			visibility: computedStyle.visibility,
			position: htmlElement.style.position || '',
			margin: htmlElement.style.margin || '',
			boxShadow: htmlElement.style.boxShadow || '',
		});

		// Убираем трансформации и делаем элемент видимым
		htmlElement.style.transform = 'none';
		htmlElement.style.opacity = '1';
		htmlElement.style.visibility = 'visible';
		htmlElement.style.position = 'relative';
		htmlElement.style.margin = '0 auto';
		htmlElement.style.boxShadow = 'none';
	});

	try {
		// Экспортируем каждую страницу
		for (let i = 0; i < pageElements.length; i++) {
			const pageElement = pageElements[i] as HTMLElement;
			
			// Ждем, чтобы стили применились
			await new Promise(resolve => setTimeout(resolve, 50));
			
			// Вычисляем оптимальный scale на основе размера страницы
			// Для A4 (794px ширина) scale 2 даст ~1588px, что обеспечит высокое качество
			const pageWidthPx = pageElement.offsetWidth;
			const optimalScale = pageWidthPx > 1000 ? 1.8 : pageWidthPx > 600 ? 2 : 2.5;
			
			// Конвертируем элемент в canvas
			const canvas = await html2canvas(pageElement, {
				scale: optimalScale, // Оптимальный scale для баланса качества и размера
				useCORS: true,
				allowTaint: false,
				backgroundColor: '#ffffff',
				logging: false,
				width: pageElement.offsetWidth,
				height: pageElement.offsetHeight,
				// Оптимизируем изображения внутри canvas
				imageTimeout: 15000,
			});

			// Оптимизируем изображение: конвертируем в JPEG с компрессией
			// JPEG с качеством 0.95 дает высокое качество с приемлемым размером
			const imgData = canvas.toDataURL('image/jpeg', 0.95);

			// Вычисляем размеры для PDF
			const imgWidth = format[0] - margin * 2;
			const imgHeight = (canvas.height * imgWidth) / canvas.width;

			// Проверяем, не превышает ли высота размер страницы
			const maxHeight = format[1] - margin * 2;
			const finalHeight = imgHeight > maxHeight ? maxHeight : imgHeight;
			const finalWidth = (canvas.width * finalHeight) / canvas.height;

			// Добавляем новую страницу (кроме первой)
			if (i > 0) {
				pdf.addPage(format, orientation);
			}

			// Добавляем изображение на страницу (JPEG вместо PNG)
			pdf.addImage(imgData, 'JPEG', margin, margin, finalWidth, finalHeight);
		}
	} finally {
		// Восстанавливаем оригинальные стили элементов
		originalStyles.forEach(({ element, transform, opacity, visibility, position, margin, boxShadow }) => {
			element.style.transform = transform;
			element.style.opacity = opacity;
			element.style.visibility = visibility;
			element.style.position = position;
			element.style.margin = margin;
			element.style.boxShadow = boxShadow;
		});

		// Восстанавливаем трансформации родительского контейнера
		if (parentContainer) {
			parentContainer.style.transform = originalParentTransform;
			parentContainer.style.width = originalParentWidth;
		}
	}

	// Сохраняем PDF
	pdf.save(`${filename}.pdf`);
}
