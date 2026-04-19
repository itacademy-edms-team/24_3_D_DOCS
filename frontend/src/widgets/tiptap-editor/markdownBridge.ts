import MarkdownIt from 'markdown-it';
import markdownItMark from 'markdown-it-mark';
import markdownItSub from 'markdown-it-sub';
import markdownItSup from 'markdown-it-sup';
import TurndownService from 'turndown';
import { gfm } from 'turndown-plugin-gfm/lib/turndown-plugin-gfm.es.js';
import { renderLatex as renderLatexInMarkdown } from '@/utils/renderers/formulaRenderer';

/** Отдельный парсер для редактора: без linkify — иначе в сырой текст «просачиваются» лишние разметки ссылок. */
const markdownParserForEditor = new MarkdownIt({
	html: true,
	linkify: false,
	breaks: false,
});
markdownParserForEditor.enable(['table']);
markdownParserForEditor.use(markdownItSup);
markdownParserForEditor.use(markdownItSub);
markdownParserForEditor.use(markdownItMark);

function safeDecodeURIComponent(s: string): string {
	try {
		return decodeURIComponent(s);
	} catch {
		return s;
	}
}

/** Канонизация строки markdown для сравнения и сохранения (один источник истины). */
export function canonicalMarkdown(s: string): string {
	return s
		.replace(/\r\n/g, '\n')
		.replace(/\n{3,}/g, '\n\n')
		.trimEnd();
}

export function normalizeMarkdownForCompare(a: string, b: string): boolean {
	return canonicalMarkdown(a) === canonicalMarkdown(b);
}

function protectFencedCodeBlocks(md: string): { text: string; blocks: string[] } {
	const blocks: string[] = [];
	const text = md.replace(/```[\s\S]*?```/g, (block) => {
		const i = blocks.length;
		blocks.push(block);
		return `\uE000CODE${i}\uE001`;
	});
	return { text, blocks };
}

function restoreFencedCodeBlocks(text: string, blocks: string[]): string {
	let out = text;
	blocks.forEach((block, i) => {
		out = out.replace(`\uE000CODE${i}\uE001`, block);
	});
	return out;
}

/** Подписи и формулы вставляем сырым HTML в markdown (вне fenced-блоков), как и для math. */
function injectCaptionsAndMath(md: string): string {
	const { text, blocks } = protectFencedCodeBlocks(md);
	let t = text;
	t = t.replace(/\[(IMAGE|TABLE|FORMULA)-CAPTION:\s*([^\]]+)\]/g, (_, type: string, cap: string) => {
		const enc = encodeURIComponent(String(cap).trim());
		return `<div data-ddoc-caption="${type}" data-ddoc-text="${enc}"></div>`;
	});
	t = t.replace(/\$\$([\s\S]+?)\$\$/g, (_, formula: string) => {
		const enc = encodeURIComponent(formula.trim());
		return `<div data-type="block-math" data-formula="${enc}"></div>`;
	});
	t = t.replace(/\$([^$\n]+)\$/g, (_, formula: string) => {
		const enc = encodeURIComponent(formula.trim());
		return `<span data-type="inline-math" data-formula="${enc}"></span>`;
	});
	return restoreFencedCodeBlocks(t, blocks);
}

function preprocessLikeDocument(md: string): string {
	let pre = md;
	pre = pre.replace(/\^\^([^^]+)\^\^/g, '<sup>$1</sup>');
	pre = pre.replace(/~~([^~]+)~~/g, '<del>$1</del>');
	return pre;
}

/** Markdown документа → HTML для TipTap (сырой смысл совпадает с тем, что хранится в content). */
export function markdownToTipTapHtml(markdown: string): string {
	if (!markdown.trim()) {
		return '<p></p>';
	}
	let pre = preprocessLikeDocument(markdown);
	pre = injectCaptionsAndMath(pre);
	const withLatex = renderLatexInMarkdown(pre);
	const html = markdownParserForEditor.render(withLatex);
	return html;
}

let turndownSingleton: TurndownService | null = null;

/** На время `tipTapHtmlToMarkdown`: плейсхолдеры `<div data-ddoc-table-embed>` → готовый GFM. */
let tableEmbedPayloads: string[] = [];

/**
 * Атомарный вывод таблицы в GFM: ячейки без блочных `\n\n` от `<p>` (типичная поломка Turndown).
 * Экспорт для конструкторов / тестов; полный документ проходит через {@link tipTapHtmlToMarkdown}.
 */
export function serializeHtmlTableToGfmMarkdown(table: HTMLTableElement): string {
	const td = getTurndown();

	function getDirectRows(): HTMLTableRowElement[] {
		const rows: HTMLTableRowElement[] = [];
		const sections = [
			table.tHead,
			...Array.from(table.tBodies),
			table.tFoot,
		].filter((section): section is HTMLTableSectionElement => Boolean(section));

		for (const section of sections) {
			rows.push(...Array.from(section.rows));
		}

		rows.push(
			...Array.from(table.children).filter(
				(child): child is HTMLTableRowElement => child.tagName.toLowerCase() === 'tr',
			),
		);

		return rows;
	}

	function flattenCellHtml(html: string): string {
		let h = html;
		h = h.replace(/<br\b[^>]*\/?>/gi, ' ');
		h = h.replace(/<p\b[^>]*>/gi, '<span>').replace(/<\/p>/gi, '</span>');
		return h;
	}

	function cellToMd(cell: HTMLTableCellElement): string {
		const raw = flattenCellHtml(cell.innerHTML).trim();
		if (!raw) {
			return '';
		}
		let md = td.turndown(raw).trim();
		md = md.replace(/\r?\n+/g, ' ').replace(/\s+/g, ' ').trim();
		return md.replace(/\|/g, '\\|');
	}

	const rows = getDirectRows();
	const serializedRows: string[][] = [];
	for (const row of rows) {
		const cells = Array.from(row.cells) as HTMLTableCellElement[];
		if (cells.length === 0) {
			continue;
		}
		const parts: string[] = [];
		for (const cell of cells) {
			const span = Math.max(1, cell.colSpan);
			parts.push(cellToMd(cell));
			for (let s = 1; s < span; s++) {
				parts.push('');
			}
		}
		serializedRows.push(parts);
	}

	const width = Math.max(1, ...serializedRows.map((row) => row.length));
	const normalizedRows = serializedRows.length > 0 ? serializedRows : [Array(width).fill('')];
	const lines: string[] = [];
	for (let ri = 0; ri < normalizedRows.length; ri++) {
		const parts = normalizedRows[ri].slice(0, width);
		while (parts.length < width) {
			parts.push('');
		}
		lines.push(`| ${parts.join(' | ')} |`);
		/* GFM требует строку-разделитель после первой строки; без неё markdown-it не строит <table>, и таблица пропадает при round-trip (часто первая строка — <td>, не <th>). */
		if (ri === 0) {
			lines.push(`| ${parts.map(() => '---').join(' | ')} |`);
		}
	}

	return lines.join('\n');
}

function replaceTablesWithEmbedPlaceholders(html: string): string {
	if (typeof DOMParser === 'undefined') {
		return html;
	}

	try {
		const doc = new DOMParser().parseFromString(html, 'text/html');
		const tables = Array.from(doc.body.querySelectorAll('table')).filter(
			(table) => !table.parentElement?.closest('table'),
		);
		for (const table of tables) {
			const md = serializeHtmlTableToGfmMarkdown(table);
			const idx = tableEmbedPayloads.length;
			tableEmbedPayloads.push(md);
			const placeholder = doc.createElement('div');
			placeholder.setAttribute('data-ddoc-table-embed', String(idx));
			table.replaceWith(placeholder);
		}
		return doc.body.innerHTML;
	} catch {
		return html;
	}
}

function getTurndown(): TurndownService {
	if (!turndownSingleton) {
		const td = new TurndownService({
			headingStyle: 'atx',
			codeBlockStyle: 'fenced',
			emDelimiter: '*',
		});
		/* Специфичные узлы — до GFM, чтобы не перехватывались общими правилами. */
		td.addRule('documentCaption', {
			filter(node) {
				return (
					node.nodeName === 'DIV' &&
					Boolean((node as HTMLElement).getAttribute?.('data-ddoc-caption'))
				);
			},
			replacement(_content, node) {
				const el = node as HTMLElement;
				const kind = el.getAttribute('data-ddoc-caption') || 'TABLE';
				const raw = el.getAttribute('data-ddoc-text') || '';
				const text = safeDecodeURIComponent(raw);
				return `\n\n[${kind}-CAPTION: ${text}]\n\n`;
			},
		});
		td.addRule('blockMath', {
			filter(node) {
				return (
					node.nodeName === 'DIV' &&
					(node as HTMLElement).getAttribute('data-type') === 'block-math'
				);
			},
			replacement(_content, node) {
				const el = node as HTMLElement;
				const raw = el.getAttribute('data-formula') || '';
				const formula = safeDecodeURIComponent(raw);
				return `\n\n$$${formula}$$\n\n`;
			},
		});
		td.addRule('inlineMath', {
			filter(node) {
				return (
					node.nodeName === 'SPAN' &&
					(node as HTMLElement).getAttribute('data-type') === 'inline-math'
				);
			},
			replacement(_content, node) {
				const el = node as HTMLElement;
				const raw = el.getAttribute('data-formula') || '';
				const formula = safeDecodeURIComponent(raw);
				return `$${formula}$`;
			},
		});
		td.use(gfm);
		td.addRule('ddocTableEmbed', {
			filter(node) {
				return (
					node.nodeName === 'DIV' &&
					(node as HTMLElement).getAttribute('data-ddoc-table-embed') !== null
				);
			},
			replacement(_content, node) {
				const el = node as HTMLElement;
				const i = Number(el.getAttribute('data-ddoc-table-embed'));
				const block = tableEmbedPayloads[i];
				return block != null && block.length > 0 ? `\n\n${block}\n\n` : '\n\n';
			},
		});
		turndownSingleton = td;
	}
	return turndownSingleton;
}

/**
 * TipTap Table кладёт `<colgroup>` перед `<tbody>`. Turndown GFM считает первую строку заголовком
 * только при `isFirstTbody` — при соседнем colgroup условие ложно, таблица уходит в `keep` как сырой HTML.
 */
function stripColgroupForTurndown(html: string): string {
	return html.replace(/<colgroup\b[^>]*>[\s\S]*?<\/colgroup>/gi, '');
}

/** HTML из TipTap → markdown для API (канонический вид). */
export function tipTapHtmlToMarkdown(html: string): string {
	const td = getTurndown();
	const prepared = stripColgroupForTurndown(html);
	tableEmbedPayloads = [];
	try {
		const withEmbeds = replaceTablesWithEmbedPlaceholders(prepared);
		return canonicalMarkdown(td.turndown(withEmbeds));
	} finally {
		tableEmbedPayloads = [];
	}
}
