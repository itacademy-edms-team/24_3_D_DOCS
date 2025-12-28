using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RusalProject.Models.Types;

namespace RusalProject.Services.Markdown;

public class MarkdownParserService : IMarkdownParserService
{
    private readonly ILogger<MarkdownParserService> _logger;

    public MarkdownParserService(ILogger<MarkdownParserService> logger)
    {
        _logger = logger;
    }

    public List<ParsedBlock> ParseDocument(string markdown)
    {
        var blocks = new List<ParsedBlock>();
        var lines = markdown.Split('\n');
        var i = 0;
        var iterationCount = 0;
        var maxIterations = lines.Length * 2; // Защита от бесконечного цикла

        while (i < lines.Length && iterationCount < maxIterations)
        {
            iterationCount++;
            var line = lines[i];
            var trimmedLine = line.TrimEnd();

            // Skip empty lines (but track them)
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                i++;
                continue;
            }

            // Check for headings
            if (trimmedLine.StartsWith("#"))
            {
                var level = 0;
                while (level < trimmedLine.Length && trimmedLine[level] == '#')
                    level++;

                if (level <= 6 && (level >= trimmedLine.Length || char.IsWhiteSpace(trimmedLine[level])))
                {
                    var text = trimmedLine.Substring(level).Trim();
                    var rawText = line;
                    var normalizedText = $"HEADING({level}): {NormalizeInlineText(text)}";
                    
                    blocks.Add(CreateBlock(BlockType.Heading, i, i, rawText, normalizedText));
                    i++;
                    continue;
                }
            }

            // Check for horizontal rule
            if (Regex.IsMatch(trimmedLine, @"^(\*{3,}|-{3,}|_{3,}|~{3,})$"))
            {
                blocks.Add(CreateBlock(BlockType.HorizontalRule, i, i, line, "HORIZONTAL_RULE"));
                i++;
                continue;
            }

            // Check for list items
            if (Regex.IsMatch(trimmedLine, @"^(\s*)[-*+]\s+") || Regex.IsMatch(trimmedLine, @"^(\s*)\d+\.\s+"))
            {
                var (listBlock, endLine) = ParseList(lines, i);
                blocks.Add(listBlock);
                var listNextI = Math.Max(endLine + 1, i + 1);
                if (listNextI <= i)
                {
                    _logger.LogWarning("ParseList returned endLine {EndLine} <= current i {I}, forcing increment", endLine, i);
                    i++;
                }
                else
                {
                    i = listNextI;
                }
                continue;
            }

            // Check for blockquote
            if (trimmedLine.StartsWith(">"))
            {
                var (quoteBlock, endLine) = ParseQuote(lines, i);
                blocks.Add(quoteBlock);
                var quoteNextI = Math.Max(endLine + 1, i + 1);
                if (quoteNextI <= i)
                {
                    _logger.LogWarning("ParseQuote returned endLine {EndLine} <= current i {I}, forcing increment", endLine, i);
                    i++;
                }
                else
                {
                    i = quoteNextI;
                }
                continue;
            }

            // Check for code block
            if (trimmedLine.StartsWith("```"))
            {
                var (codeBlock, endLine) = ParseCodeBlock(lines, i);
                blocks.Add(codeBlock);
                var codeNextI = Math.Max(endLine + 1, i + 1);
                if (codeNextI <= i)
                {
                    _logger.LogWarning("ParseCodeBlock returned endLine {EndLine} <= current i {I}, forcing increment", endLine, i);
                    i++;
                }
                else
                {
                    i = codeNextI;
                }
                continue;
            }

            // Check for table
            if (trimmedLine.Contains("|") && trimmedLine.Count(c => c == '|') >= 2)
            {
                var (tableBlocks, endLine) = ParseTable(lines, i);
                blocks.AddRange(tableBlocks);
                var tableNextI = Math.Max(endLine + 1, i + 1);
                if (tableNextI <= i)
                {
                    _logger.LogWarning("ParseTable returned endLine {EndLine} <= current i {I}, forcing increment", endLine, i);
                    i++;
                }
                else
                {
                    i = tableNextI;
                }
                continue;
            }

            // Check for captions
            var captionMatch = Regex.Match(trimmedLine, @"^\[(IMAGE|TABLE|FORMULA)-CAPTION:\s*(.+)\]$");
            if (captionMatch.Success)
            {
                var captionType = captionMatch.Groups[1].Value;
                var captionText = captionMatch.Groups[2].Value.Trim();
                var normalizedText = $"{captionType}_CAPTION: {NormalizeInlineText(captionText)}";
                
                blocks.Add(CreateBlock(BlockType.Caption, i, i, line, normalizedText));
                i++;
                continue;
            }

            // Check for image
            var imageMatch = Regex.Match(trimmedLine, @"!\[([^\]]*)\]\(([^)]+)\)");
            if (imageMatch.Success)
            {
                var altText = imageMatch.Groups[1].Value;
                var url = imageMatch.Groups[2].Value;
                var normalizedText = $"IMAGE: alt=\"{NormalizeInlineText(altText)}\" url=\"{url}\"";
                
                blocks.Add(CreateBlock(BlockType.Image, i, i, line, normalizedText));
                i++;
                continue;
            }

            // Check for formula (block)
            if (trimmedLine.StartsWith("\\["))
            {
                var (formulaBlock, endLine) = ParseFormulaBlock(lines, i);
                blocks.Add(formulaBlock);
                var formulaNextI = Math.Max(endLine + 1, i + 1);
                if (formulaNextI <= i)
                {
                    _logger.LogWarning("ParseFormulaBlock returned endLine {EndLine} <= current i {I}, forcing increment", endLine, i);
                    i++;
                }
                else
                {
                    i = formulaNextI;
                }
                continue;
            }

            // Regular paragraph - collect until empty line or special block
            var (paragraphBlock, paragraphEndLine) = ParseParagraph(lines, i);
            blocks.Add(paragraphBlock);
            // Защита от зацикливания: убеждаемся, что i увеличивается
            var paraNextI = Math.Max(paragraphEndLine + 1, i + 1);
            if (paraNextI <= i)
            {
                _logger.LogWarning("ParseParagraph returned endLine {EndLine} <= current i {I}, forcing increment", paragraphEndLine, i);
                i++;
            }
            else
            {
                i = paraNextI;
            }
        }

        if (iterationCount >= maxIterations)
        {
            _logger.LogError("ParseDocument: достигнут лимит итераций {MaxIterations}, возможно бесконечный цикл. Обработано строк: {I} из {TotalLines}", maxIterations, i, lines.Length);
        }

        return blocks;
    }

    private (ParsedBlock block, int endLine) ParseList(string[] lines, int startLine)
    {
        var rawLines = new List<string>();
        var normalizedLines = new List<string>();
        var i = startLine;
        var baseIndent = 0;

        while (i < lines.Length)
        {
            var line = lines[i];
            var trimmedLine = line.TrimEnd();

            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                break;
            }

            var listMatch = Regex.Match(trimmedLine, @"^(\s*)([-*+]|\d+\.)\s+(.+)$");
            if (listMatch.Success)
            {
                if (rawLines.Count == 0)
                {
                    baseIndent = listMatch.Groups[1].Value.Length;
                }

                var indent = listMatch.Groups[1].Value.Length;
                var level = (indent - baseIndent) / 2 + 1;
                var text = listMatch.Groups[3].Value;
                var itemNormalizedText = $"LIST_ITEM(level={level}): {NormalizeInlineText(text)}";

                rawLines.Add(line);
                normalizedLines.Add(itemNormalizedText);
                i++;
            }
            else if (trimmedLine.StartsWith(new string(' ', baseIndent + 2)))
            {
                // Continuation line
                rawLines.Add(line);
                normalizedLines.Add(NormalizeInlineText(trimmedLine));
                i++;
            }
            else
            {
                break;
            }
        }

        var rawText = string.Join("\n", rawLines);
        var normalizedText = string.Join(" ", normalizedLines);
        var block = CreateBlock(BlockType.ListItem, startLine, i - 1, rawText, normalizedText);
        return (block, i - 1);
    }

    private (ParsedBlock block, int endLine) ParseQuote(string[] lines, int startLine)
    {
        var rawLines = new List<string>();
        var normalizedLines = new List<string>();
        var i = startLine;
        var baseLevel = 0;

        while (i < lines.Length)
        {
            var line = lines[i];
            var trimmedLine = line.TrimEnd();

            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                break;
            }

            if (trimmedLine.StartsWith(">"))
            {
                if (rawLines.Count == 0)
                {
                    baseLevel = trimmedLine.TakeWhile(c => c == '>').Count();
                }

                var level = trimmedLine.TakeWhile(c => c == '>').Count();
                var text = trimmedLine.Substring(level).TrimStart();
                var quoteNormalizedText = $"QUOTE(level={level}): {NormalizeInlineText(text)}";

                rawLines.Add(line);
                normalizedLines.Add(quoteNormalizedText);
                i++;
            }
            else
            {
                break;
            }
        }

        var rawText = string.Join("\n", rawLines);
        var normalizedText = string.Join(" ", normalizedLines);
        var block = CreateBlock(BlockType.Quote, startLine, i - 1, rawText, normalizedText);
        return (block, i - 1);
    }

    private (ParsedBlock block, int endLine) ParseCodeBlock(string[] lines, int startLine)
    {
        var rawLines = new List<string> { lines[startLine] };
        var language = "";
        var codeLines = new List<string>();
        var i = startLine + 1;
        var maxLines = Math.Min(startLine + 10000, lines.Length); // Защита от бесконечного цикла

        var firstLine = lines[startLine].Trim();
        if (firstLine.Length > 3)
        {
            language = firstLine.Substring(3).Trim();
        }

        while (i < maxLines)
        {
            var line = lines[i];
            rawLines.Add(line);

            if (line.Trim().StartsWith("```"))
            {
                break;
            }

            codeLines.Add(line);
            i++;
        }
        var rawText = string.Join("\n", rawLines);
        var codeContent = string.Join("\n", codeLines.Take(10)); // Limit to first 10 lines
        var normalizedText = $"CODE_BLOCK(language={language}): {codeContent}";
        var block = CreateBlock(BlockType.Code, startLine, i, rawText, normalizedText);
        return (block, i);
    }

    private (List<ParsedBlock> blocks, int endLine) ParseTable(string[] lines, int startLine)
    {
        var blocks = new List<ParsedBlock>();
        var rawLines = new List<string>();
        var normalizedParts = new List<string>();
        var i = startLine;

        // Parse header
        if (i < lines.Length)
        {
            var headerLine = lines[i].TrimEnd();
            rawLines.Add(lines[i]);
            var columns = headerLine.Split('|')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
            normalizedParts.Add($"TABLE: {string.Join(" | ", columns)}");
            i++;

            // Skip separator line (|---|---|)
            if (i < lines.Length && lines[i].Trim().Contains("|"))
            {
                rawLines.Add(lines[i]);
                i++;
            }
        }

        // Parse rows
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("|"))
            {
                break;
            }

            rawLines.Add(lines[i]);
            var cells = line.Split('|')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
            normalizedParts.Add($"ROW: {string.Join(" | ", cells.Select(NormalizeInlineText))}");
            i++;
        }

        var rawText = string.Join("\n", rawLines);
        var normalizedText = string.Join(" ", normalizedParts);
        blocks.Add(CreateBlock(BlockType.TableRow, startLine, i - 1, rawText, normalizedText));

        return (blocks, i - 1);
    }

    private (ParsedBlock block, int endLine) ParseFormulaBlock(string[] lines, int startLine)
    {
        var rawLines = new List<string> { lines[startLine] };
        var formulaLines = new List<string>();
        var i = startLine + 1;
        var maxLines = Math.Min(startLine + 10000, lines.Length); // Защита от бесконечного цикла

        while (i < maxLines)
        {
            var line = lines[i];
            rawLines.Add(line);

            if (line.Trim().StartsWith("\\]"))
            {
                break;
            }

            formulaLines.Add(line.Trim());
            i++;
        }
        var rawText = string.Join("\n", rawLines);
        var formula = string.Join(" ", formulaLines);
        var normalizedText = $"FORMULA_BLOCK: {formula}";
        var block = CreateBlock(BlockType.Formula, startLine, i, rawText, normalizedText);
        return (block, i);
    }

    private (ParsedBlock block, int endLine) ParseParagraph(string[] lines, int startLine)
    {
        var rawLines = new List<string>();
        var normalizedParts = new List<string>();
        var i = startLine;

        while (i < lines.Length)
        {
            var line = lines[i];
            var trimmedLine = line.TrimEnd();

            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                break;
            }

            // Check if next line starts a special block
            if (i + 1 < lines.Length)
            {
                var nextLine = lines[i + 1].Trim();
                if (nextLine.StartsWith("#") || 
                    nextLine.StartsWith("```") ||
                    nextLine.StartsWith(">") ||
                    (nextLine.Contains("|") && nextLine.Count(c => c == '|') >= 2) ||
                    Regex.IsMatch(nextLine, @"^[-*+]\s+") ||
                    Regex.IsMatch(nextLine, @"^\d+\.\s+"))
                {
                    rawLines.Add(line);
                    normalizedParts.Add(NormalizeInlineText(trimmedLine));
                    break;
                }
            }
            else
            {
                // Last line of document - add it and break
                rawLines.Add(line);
                normalizedParts.Add(NormalizeInlineText(trimmedLine));
                i++;
                break;
            }

            rawLines.Add(line);
            normalizedParts.Add(NormalizeInlineText(trimmedLine));
            i++;
        }
        var rawText = string.Join("\n", rawLines);
        var normalizedText = string.Join(" ", normalizedParts);
        var block = CreateBlock(BlockType.Paragraph, startLine, i - 1, rawText, normalizedText);
        return (block, i - 1);
    }

    private string NormalizeInlineText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var normalized = text;

        // Remove HTML tags
        normalized = Regex.Replace(normalized, @"<[^>]+>", "");

        // Remove markdown formatting but keep text
        normalized = Regex.Replace(normalized, @"\*\*\*(.+?)\*\*\*", "$1"); // Bold italic
        normalized = Regex.Replace(normalized, @"\*\*(.+?)\*\*", "$1"); // Bold
        normalized = Regex.Replace(normalized, @"\*(.+?)\*", "$1"); // Italic
        normalized = Regex.Replace(normalized, @"__(.+?)__", "$1"); // Bold (underscore)
        normalized = Regex.Replace(normalized, @"_(.+?)_", "$1"); // Italic (underscore)
        normalized = Regex.Replace(normalized, @"~~(.+?)~~", "$1"); // Strikethrough
        normalized = Regex.Replace(normalized, @"==(.+?)==", "$1"); // Highlight
        normalized = Regex.Replace(normalized, @"<u>(.+?)</u>", "$1"); // Underline

        // Handle inline code
        normalized = Regex.Replace(normalized, @"`([^`]+)`", "INLINE_CODE: $1");

        // Handle inline formulas
        normalized = Regex.Replace(normalized, @"\\\((.+?)\\\)", "FORMULA_INLINE: $1");

        // Handle links
        normalized = Regex.Replace(normalized, @"\[([^\]]+)\]\([^\)]+\)", "$1");

        // Handle superscript/subscript (non-standard)
        normalized = Regex.Replace(normalized, @"\^\^([^^]+)\^\^", "$1"); // Superscript
        normalized = Regex.Replace(normalized, @"~([^~]+)~", "$1"); // Subscript (but not ~~)

        // Clean up extra whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    private ParsedBlock CreateBlock(BlockType blockType, int startLine, int endLine, string rawText, string normalizedText)
    {
        var hash = ComputeHash(normalizedText);
        return new ParsedBlock
        {
            BlockType = blockType,
            StartLine = startLine,
            EndLine = endLine,
            RawText = rawText,
            NormalizedText = normalizedText,
            ContentHash = hash
        };
    }

    private string ComputeHash(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
