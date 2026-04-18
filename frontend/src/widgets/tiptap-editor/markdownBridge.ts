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
		turndownSingleton = td;
	}
	return turndownSingleton;
}

/** HTML из TipTap → markdown для API (канонический вид). */
export function tipTapHtmlToMarkdown(html: string): string {
	const td = getTurndown();
	return canonicalMarkdown(td.turndown(html));
}
