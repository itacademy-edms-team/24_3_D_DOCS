import katex from 'katex';
import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { getFinalStyle, styleToCSS, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Render LaTeX formulas in markdown text
 * Pure function - no side effects
 */
const getKatexOptions = (displayMode: boolean) => ({
	displayMode,
	throwOnError: false,
	strict: 'ignore' as const,
});

export function renderLatex(text: string): string {
	const displayFormulaCache = new Map<string, string>();
	const inlineFormulaCache = new Map<string, string>();

	// Block formulas: $$...$$ or \[...\]
	text = text.replace(/\$\$([^$]+)\$\$/g, (_, formula) => {
		try {
			const normalized = formula.trim();
			const cached = displayFormulaCache.get(normalized);
			if (cached) {
				return `<div class="formula-block">${cached}</div>`;
			}
			const html = katex.renderToString(normalized, getKatexOptions(true));
			displayFormulaCache.set(normalized, html);
			return `<div class="formula-block">${html}</div>`;
		} catch {
			return `<div class="formula-block formula-error">${formula}</div>`;
		}
	});

	// Block formulas: \[...\]
	text = text.replace(/\\\[([^\]]+)\\\]/g, (_, formula) => {
		try {
			const normalized = formula.trim();
			const cached = displayFormulaCache.get(normalized);
			if (cached) {
				return `<div class="formula-block">${cached}</div>`;
			}
			const html = katex.renderToString(normalized, getKatexOptions(true));
			displayFormulaCache.set(normalized, html);
			return `<div class="formula-block">${html}</div>`;
		} catch {
			return `<div class="formula-block formula-error">${formula}</div>`;
		}
	});

	// Inline formulas: $...$ or \(...\)
	text = text.replace(/\$([^$\n]+)\$/g, (_, formula) => {
		try {
			const normalized = formula.trim();
			const cached = inlineFormulaCache.get(normalized);
			if (cached) {
				return `<span class="formula-inline">${cached}</span>`;
			}
			const html = katex.renderToString(normalized, getKatexOptions(false));
			inlineFormulaCache.set(normalized, html);
			return `<span class="formula-inline">${html}</span>`;
		} catch {
			return `<span class="formula-error">${formula}</span>`;
		}
	});

	// Inline formulas: \(...\)
	text = text.replace(/\\\(([^)]+)\\\)/g, (_, formula) => {
		try {
			const normalized = formula.trim();
			const cached = inlineFormulaCache.get(normalized);
			if (cached) {
				return `<span class="formula-inline">${cached}</span>`;
			}
			const html = katex.renderToString(normalized, getKatexOptions(false));
			inlineFormulaCache.set(normalized, html);
			return `<span class="formula-inline">${html}</span>`;
		} catch {
			return `<span class="formula-error">${formula}</span>`;
		}
	});

	return text;
}

/**
 * Render formulas with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderFormulas(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	// Get all formulas in document order for numbering
	const formulas = Array.from(doc.querySelectorAll('.formula-block')) as Element[];
	
	// First pass: calculate formula numbers for formulas with captions
	const formulaNumbers = new Map<Element, number>();
	let formulaCounter = 0;
	
	formulas.forEach((formulaBlock) => {
		const nextSibling = formulaBlock.nextElementSibling;
		if (nextSibling && nextSibling.tagName === 'P') {
			const pText = nextSibling.textContent || '';
			const match = pText.match(/^\[FORMULA-CAPTION:\s*(.+)\]$/);
			if (match) {
				formulaCounter++;
				formulaNumbers.set(formulaBlock, formulaCounter);
			}
		}
	});
	
	// Second pass: render formulas
	formulas.forEach((formulaBlock) => {
		const content = formulaBlock.textContent || '';
		const elId = generateElementId('formula', content.slice(0, 50), usedIds);
		const style = getFinalStyle('formula' as EntityType, elId, profile, overrides);

		let captionText: string | null = null;
		const formulaNumber = formulaNumbers.get(formulaBlock);
		const nextSibling = formulaBlock.nextElementSibling;
		if (nextSibling && nextSibling.tagName === 'P') {
			const pText = nextSibling.textContent || '';
			const match = pText.match(/^\[FORMULA-CAPTION:\s*(.+)\]$/);
			if (match) {
				captionText = match[1].trim();
				nextSibling.remove();
			}
		}

		const formulaWrapper = doc.createElement('div');
		formulaWrapper.id = elId;
		formulaWrapper.setAttribute('data-type', 'formula');
		formulaWrapper.setAttribute('style', styleToCSS(style));
		if (selectable) formulaWrapper.classList.add('element-selectable');

		const parent = formulaBlock.parentNode;
		if (parent) {
			parent.insertBefore(formulaWrapper, formulaBlock);
			formulaWrapper.appendChild(formulaBlock);

			const formulaFinalStyle: EntityStyle = {
				...style,
				marginTop: undefined,
				marginBottom: undefined,
				marginLeft: undefined,
				marginRight: undefined,
			};
			formulaBlock.setAttribute('style', styleToCSS(formulaFinalStyle));
			if (selectable)
				(formulaBlock as HTMLElement).classList.add('element-selectable');

			if (captionText && formulaNumber) {
				const captionId = generateElementId('formula-caption', captionText, usedIds);
				const captionStyle = getFinalStyle(
					'formula-caption' as EntityType,
					captionId,
					profile,
					overrides
				);

				// Get caption format from profile
				const captionFormat =
					captionStyle.captionFormat ||
					profile?.entityStyles?.['formula-caption']?.captionFormat ||
					'Формула {n} - {content}';
				
				// Apply template
				const formattedCaption = captionFormat
					.replace(/{n}/g, formulaNumber.toString())
					.replace(/{content}/g, captionText);

				const caption = doc.createElement('div');
				caption.id = captionId;
				caption.setAttribute('data-type', 'formula-caption');
				caption.setAttribute('style', styleToCSS(captionStyle));
				caption.textContent = formattedCaption;
				if (selectable) caption.classList.add('element-selectable');

				parent.insertBefore(caption, formulaWrapper.nextSibling);
			}
		}
	});

	// Apply formula style to inline formulas as well
	const inlineFormulas = Array.from(doc.querySelectorAll('.formula-inline')) as Element[];
	inlineFormulas.forEach((inlineFormula) => {
		const content = inlineFormula.textContent || '';
		const inlineId = generateElementId('formula-inline', content.slice(0, 50), usedIds);
		const inlineStyle = getFinalStyle('formula' as EntityType, inlineId, profile, overrides);
		const inlineFinalStyle: EntityStyle = {
			...inlineStyle,
			marginTop: undefined,
			marginBottom: undefined,
			marginLeft: undefined,
			marginRight: undefined,
		};

		inlineFormula.setAttribute('id', inlineId);
		inlineFormula.setAttribute('data-type', 'formula-inline');
		inlineFormula.setAttribute('style', styleToCSS(inlineFinalStyle));
		if (selectable) (inlineFormula as HTMLElement).classList.add('element-selectable');
	});
}
