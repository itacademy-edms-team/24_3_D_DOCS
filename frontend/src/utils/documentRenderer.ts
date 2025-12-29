import MarkdownIt from 'markdown-it';
import markdownItMark from 'markdown-it-mark';
import markdownItSub from 'markdown-it-sub';
import markdownItSup from 'markdown-it-sup';
import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { renderLatex } from './renderers/formulaRenderer';
import { renderParagraphs } from './renderers/paragraphRenderer';
import { renderHeadings } from './renderers/headingRenderer';
import { renderImages } from './renderers/imageRenderer';
import {
	renderUnorderedLists,
	renderOrderedLists,
	renderTaskLists,
} from './renderers/listRenderer';
import { renderTables } from './renderers/tableRenderer';
import {
	renderFormulas,
	renderLatex as renderLatexText,
} from './renderers/formulaRenderer';
import {
	renderCodeBlocks,
	renderInlineCode,
} from './renderers/codeRenderer';
import { renderBlockquotes } from './renderers/blockquoteRenderer';
import { renderLinks } from './renderers/linkRenderer';
import { renderHorizontalRules } from './renderers/horizontalRuleRenderer';
import { renderHighlight } from './renderers/highlightRenderer';
import { renderSuperscript } from './renderers/superscriptRenderer';
import { renderSubscript } from './renderers/subscriptRenderer';
import { renderStrikethrough } from './renderers/strikethroughRenderer';

interface RenderDocumentOptions {
	markdown: string;
	profile: ProfileData | null;
	overrides?: Record<string, EntityStyle>;
	selectable?: boolean;
}

/**
 * Render markdown document to HTML with applied styles
 * Pure function - no side effects (except DOM manipulation inside, but isolated)
 */
export function renderDocument(options: RenderDocumentOptions): string {
	const {
		markdown,
		profile,
		overrides = {},
		selectable = false,
	} = options;

	if (!markdown.trim()) {
		return '';
	}

	// 1. Preprocess text to handle conflicts and special syntax
	// Handle double caret (^^text^^) for superscript before markdown-it-sup processes single caret (^text^)
	let preprocessedMarkdown = markdown;
	
	// Replace ^^text^^ with <sup>text</sup> before markdown-it-sup processes ^text^
	// Use a regex that doesn't match single caret to avoid conflicts
	preprocessedMarkdown = preprocessedMarkdown.replace(
		/\^\^([^^]+)\^\^/g,
		'<sup>$1</sup>'
	);

	// Replace ~~text~~ with <del>text</del> to avoid conflict with subscript
	// Do this after handling double caret but before markdown parsing
	preprocessedMarkdown = preprocessedMarkdown.replace(
		/~~([^~]+)~~/g,
		'<del>$1</del>'
	);

	// Protect caption syntax from markdown-it parsing
	// Replace [TYPE-CAPTION: текст] with custom HTML tag that markdown-it will preserve
	// Custom tag won't be processed by markdown-it, allowing us to restore it later
	preprocessedMarkdown = preprocessedMarkdown.replace(
		/\[(IMAGE|TABLE|FORMULA)-CAPTION:\s*([^\]]+)\]/g,
		(match, type, text) => {
			// Escape special characters in text to avoid issues with HTML attributes
			const escapedText = text
				.replace(/&/g, '&amp;')
				.replace(/"/g, '&quot;')
				.replace(/'/g, '&#39;');
			return `<x-caption type="${type}" text="${escapedText}"></x-caption>`;
		}
	);

	// 2. Process LaTeX formulas (after preprocessing, before markdown parsing)
	const contentWithFormulas = renderLatexText(preprocessedMarkdown);

	// 3. Markdown -> HTML using markdown-it with plugins
	// Configure markdown-it with GFM and plugins for sup/sub/mark
	const md = new MarkdownIt({
		html: true,
		linkify: true,
		breaks: false, // Don't convert single line breaks to <br>
	});

	// Use GFM table syntax and strikethrough (though we handle it manually)
	md.enable(['table']);

	// Add plugins for superscript, subscript, and highlight
	// Note: markdown-it-sup supports ^text^ (single caret) by default
	// We handle ^^text^^ (double caret) in preprocessing step above
	md.use(markdownItSup); // Supports ^text^ (single caret)
	md.use(markdownItSub); // Supports ~text~ (single tilde, but not ~~text~~ which is strikethrough, handled in preprocessing)
	md.use(markdownItMark); // Supports ==text==

	const rawHtml = md.render(contentWithFormulas);

	// 3. Parse HTML for style application
	const parser = new DOMParser();
	const doc = parser.parseFromString(rawHtml, 'text/html');
	const usedIds = new Set<string>();

	// Restore caption placeholders from <x-caption> tags to text format
	// Find all <x-caption> tags and replace them with [TYPE-CAPTION: текст]
	// This needs to happen before other renderers process the document
	const captionPlaceholders = Array.from(doc.querySelectorAll('x-caption'));
	captionPlaceholders.forEach((placeholder) => {
		const type = placeholder.getAttribute('type');
		const escapedText = placeholder.getAttribute('text') || '';
		
		// Decode HTML entities in the text
		const text = escapedText
			.replace(/&quot;/g, '"')
			.replace(/&#39;/g, "'")
			.replace(/&amp;/g, '&');
		
		// Replace placeholder with text [TYPE-CAPTION: текст]
		const captionText = `[${type}-CAPTION: ${text}]`;
		
		// Replace the placeholder element with a text node
		const textNode = doc.createTextNode(captionText);
		const parent = placeholder.parentNode;
		if (parent) {
			parent.replaceChild(textNode, placeholder);
		}
	});

	// 4-12. Process all element types using renderers (order matters)
	// Process formulas first (formula blocks created by renderLatex)
	renderFormulas(doc, usedIds, profile, overrides, selectable);

	// Process paragraphs (must come after formula processing to skip captions)
	renderParagraphs(doc, usedIds, profile, overrides, selectable);

	// Process headings
	renderHeadings(doc, usedIds, profile, overrides, selectable);

	// Process images
	renderImages(doc, usedIds, profile, overrides, selectable);

	// Process lists (unordered, ordered, task lists)
	renderUnorderedLists(doc, usedIds, profile, overrides, selectable);
	renderOrderedLists(doc, usedIds, profile, overrides, selectable);
	renderTaskLists(doc, usedIds, profile, overrides, selectable);

	// Process tables
	renderTables(doc, usedIds, profile, overrides, selectable);

	// Process code blocks (must come after other block elements)
	renderCodeBlocks(doc, usedIds, profile, overrides, selectable);
	renderInlineCode(doc, usedIds, profile, overrides, selectable);

	// Process blockquotes
	renderBlockquotes(doc, usedIds, profile, overrides, selectable);

	// Process links
	renderLinks(doc, usedIds, profile, overrides, selectable);

	// Process horizontal rules
	renderHorizontalRules(doc, usedIds, profile, overrides, selectable);

	// Process highlighted text (mark elements)
	renderHighlight(doc, usedIds, profile, overrides, selectable);

	// Process superscript text (sup elements)
	renderSuperscript(doc, usedIds, profile, overrides, selectable);

	// Process subscript text (sub elements)
	renderSubscript(doc, usedIds, profile, overrides, selectable);

	// Process strikethrough text (del elements) - from ~~text~~ preprocessing
	renderStrikethrough(doc, usedIds, profile, overrides, selectable);

	return doc.body.innerHTML;
}
