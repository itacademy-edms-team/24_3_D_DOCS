import type { TocItem } from '@/entities/document/types';
import type { TableOfContentsSettings } from '@/entities/profile/types';
import { renderLatex } from './renderers/formulaRenderer';

const MM_TO_PX = 3.7795275591;

function escapeHtml(text: string): string {
	const div = document.createElement('div');
	div.textContent = text;
	return div.innerHTML;
}

/**
 * Render table of contents to HTML string
 */
export function renderTableOfContentsToHtml(
	tocItems: TocItem[],
	settings: TableOfContentsSettings | undefined,
	headingPageMapByIndex: number[],
	pageOffset: number
): string {
	if (!tocItems || tocItems.length === 0) return '';
	const s = settings || {};
	const fontStyle = s.fontStyle || 'normal';
	const fontWeight = s.fontWeight || 'normal';
	const fontSize = s.fontSize ?? 14;
	const indentPerLevel = ((s.indentPerLevel ?? 5) * MM_TO_PX);
	const nestingEnabled = s.nestingEnabled !== false;
	const numberingEnabled = s.numberingEnabled !== false;

	const counters = [0, 0, 0, 0, 0, 0];
	const lines: string[] = [];

	// Flat structure for splitIntoPages: each element is top-level (no toc-wrapper)
	lines.push(
		`<div class="toc-title" style="text-align: center; font-size: ${fontSize + 2}pt; font-weight: bold; margin-bottom: 16pt; font-family: 'Times New Roman', Times, serif;">СОДЕРЖАНИЕ</div>`
	);

	tocItems.forEach((item, idx) => {
		const level = Math.max(1, Math.min(6, item.level || 1));
		const text = item.text || '';
		const pageNum =
			headingPageMapByIndex[idx] !== undefined
				? pageOffset + headingPageMapByIndex[idx]
				: item.pageNumber ?? 0;
		const displayPage = pageNum > 0 ? String(pageNum) : '';

		let numberPrefix = '';
		if (numberingEnabled) {
			for (let i = level; i < 6; i++) counters[i] = 0;
			counters[level - 1]++;
			const parts: number[] = [];
			for (let i = 0; i < level; i++) parts.push(Math.max(1, counters[i]));
			numberPrefix = parts.join('.') + ' ';
		}

		const indent = nestingEnabled ? (level - 1) * indentPerLevel : 0;
		const marginLeft = indent > 0 ? `${indent}px` : '0';
		// Text wraps; dots start right after last line, fill to page number; gap = 3 dot widths
		const lineStyle = `font-size: ${fontSize}pt; font-style: ${fontStyle}; font-weight: ${fontWeight}; margin-left: ${marginLeft}; font-family: 'Times New Roman', Times, serif; display: flex; align-items: flex-end; margin-bottom: 4pt; min-width: 0; overflow: hidden;`;
		const displayText = numberPrefix + text;
		const textWithFormulas = renderLatex(displayText);
		const dotChar = '\u00B7'; // middle dot ·
		const dotsContent = dotChar.repeat(300);
		const letterSpacingEm = 0.15; // spacing between dots
		const gapToPageEm = letterSpacingEm * 3; // 3 dots before page number
		const dotsStyle = `font-size: ${fontSize}pt; font-weight: ${fontWeight}; color: #333; letter-spacing: ${letterSpacingEm}em; overflow: hidden; white-space: nowrap;`;
		// toc-text max-width ~60% so toc-dots always gets space (fixes multi-line squeezing)
		lines.push(
			`<div class="toc-line" data-toc-index="${idx}" style="${lineStyle}">` +
				`<span class="toc-text" style="flex: 0 1 auto; min-width: 0; max-width: 60%; overflow-wrap: break-word; word-break: break-word; background: white;">${textWithFormulas}</span>` +
				`<span class="toc-dots" style="flex: 1 0 2em; min-width: 2em; padding: 0 ${gapToPageEm}em 0 6px; ${dotsStyle}">${escapeHtml(dotsContent)}</span>` +
				`<span class="toc-page" style="flex-shrink: 0; margin-left: ${gapToPageEm}em;">${escapeHtml(displayPage)}</span>` +
				`</div>`
		);
	});
	return lines.join('\n');
}
