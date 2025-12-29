import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { getFinalStyle, styleToCSS, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render tables with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderTables(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	// Get all tables in document order for numbering
	const tables = Array.from(doc.querySelectorAll('table')) as Element[];
	
	// First pass: calculate table numbers for tables with captions
	const tableNumbers = new Map<Element, number>();
	let tableCounter = 0;
	
	tables.forEach((table) => {
		const nextSibling = table.nextElementSibling;
		if (nextSibling && nextSibling.tagName === 'P') {
			const pText = nextSibling.textContent || '';
			const match = pText.match(/^\[TABLE-CAPTION:\s*(.+)\]$/);
			if (match) {
				tableCounter++;
				tableNumbers.set(table, tableCounter);
			}
		}
	});
	
	// Second pass: render tables
	tables.forEach((table) => {
		const content = table.textContent || '';
		const elId = generateElementId('table', content.slice(0, 50), usedIds);
		const style = getFinalStyle('table' as EntityType, elId, profile, overrides);

		const borderStyle = style.borderStyle || 'solid';
		const borderWidth = style.borderWidth || 1;
		const borderColor = style.borderColor || '#333';
		const cellStyle = `border: ${borderWidth}px ${borderStyle} ${borderColor}; padding: 8px;`;

		table.querySelectorAll('th, td').forEach((cell) => {
			cell.setAttribute('style', cellStyle);
		});

		let captionText: string | null = null;
		const tableNumber = tableNumbers.get(table);
		const nextSibling = table.nextElementSibling;
		if (nextSibling && nextSibling.tagName === 'P') {
			const pText = nextSibling.textContent || '';
			const match = pText.match(/^\[TABLE-CAPTION:\s*(.+)\]$/);
			if (match) {
				captionText = match[1].trim();
				nextSibling.remove();
			}
		}

		const tableWrapper = doc.createElement('div');
		tableWrapper.id = elId;
		tableWrapper.setAttribute('data-type', 'table');
		tableWrapper.setAttribute('style', styleToCSS(style));
		if (selectable) tableWrapper.classList.add('element-selectable');

		const parent = table.parentNode;
		if (parent) {
			parent.insertBefore(tableWrapper, table);
			tableWrapper.appendChild(table);

			const tableFinalStyle: EntityStyle = {
				...style,
				marginTop: undefined,
				marginBottom: undefined,
				marginLeft: undefined,
				marginRight: undefined,
			};
			table.setAttribute(
				'style',
				styleToCSS(tableFinalStyle) +
					'; border-collapse: collapse; width: 100%;'
			);

			if (captionText && tableNumber) {
				const captionId = generateElementId('table-caption', captionText, usedIds);
				const captionStyle = getFinalStyle(
					'table-caption' as EntityType,
					captionId,
					profile,
					overrides
				);

				// Get caption format from profile
				const captionFormat =
					captionStyle.captionFormat ||
					profile?.entityStyles?.['table-caption']?.captionFormat ||
					'Таблица {n} - {content}';
				
				// Apply template
				const formattedCaption = captionFormat
					.replace(/{n}/g, tableNumber.toString())
					.replace(/{content}/g, captionText);

				const caption = doc.createElement('div');
				caption.id = captionId;
				caption.setAttribute('data-type', 'table-caption');
				caption.setAttribute('style', styleToCSS(captionStyle));
				caption.textContent = formattedCaption;
				if (selectable) caption.classList.add('element-selectable');

				parent.insertBefore(caption, tableWrapper.nextSibling);
			}
		}
	});
}
