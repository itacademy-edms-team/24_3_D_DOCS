/**
 * Мост атомарных блоков ↔ markdown.
 *
 * Полный документ из TipTap по-прежнему сериализуется через {@link tipTapHtmlToMarkdown}
 * (таблицы внутри него выводятся **атомарно** через {@link serializeHtmlTableToGfmMarkdown}, без GFM Turndown по целой `<table>`).
 *
 * Для будущих «конструкторов» (отдельная модалка таблицы / формулы / картинки) можно вызывать те же функции
 * и подставлять результат в исходный markdown одним фрагментом (полная замена узла).
 */
export {
	tipTapHtmlToMarkdown,
	markdownToTipTapHtml,
	serializeHtmlTableToGfmMarkdown,
	canonicalMarkdown,
	normalizeMarkdownForCompare,
} from './markdownBridge';
