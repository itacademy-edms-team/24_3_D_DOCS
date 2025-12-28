using System.Text.Json;
using RusalProject.Models.Types;
using RusalProject.Services.Documents;
using RusalProject.Services.Markdown;

namespace RusalProject.Services.Agent.Tools;

/// <summary>
/// get_header(): Retrieves the document's title or header.
/// Использует первый H1 из Markdown или имя документа как fallback.
/// </summary>
public class GetHeaderTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly IMarkdownParserService _markdownParserService;
    private readonly ILogger<GetHeaderTool> _logger;

    public string Name => "get_header";
    public string Description => "Возвращает заголовок документа: первый H1 в Markdown или имя документа.";

    public GetHeaderTool(
        IDocumentService documentService,
        IMarkdownParserService markdownParserService,
        ILogger<GetHeaderTool> logger)
    {
        _documentService = documentService;
        _markdownParserService = markdownParserService;
        _logger = logger;
    }

    public Dictionary<string, object> GetParametersSchema()
    {
        // document_id и user_id добавляются агентом; от модели ничего не требуется
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>(),
            ["required"] = Array.Empty<string>()
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!arguments.TryGetValue("document_id", out var _) ||
                !arguments.TryGetValue("user_id", out var _))
            {
                return "Ошибка: document_id и user_id обязательны для get_header";
            }

            var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
            var userId = Guid.Parse(GetStringValue(arguments, "user_id"));

            var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
            if (document == null)
            {
                return "Ошибка: Документ не найден";
            }

            var content = document.Content ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(content))
            {
                // Пробуем найти первый заголовок H1
                var blocks = _markdownParserService.ParseDocument(content);
                var firstHeading = blocks
                    .Where(b => b.BlockType == BlockType.Heading)
                    .Where(b =>
                    {
                        // Парсим уровень из NormalizedText: "HEADING(level): текст"
                        if (b.NormalizedText.StartsWith("HEADING("))
                        {
                            var levelStart = "HEADING(".Length;
                            var levelEnd = b.NormalizedText.IndexOf(')', levelStart);
                            if (levelEnd > levelStart)
                            {
                                var levelStr = b.NormalizedText.Substring(levelStart, levelEnd - levelStart);
                                return int.TryParse(levelStr, out var level) && level == 1;
                            }
                        }
                        // Fallback: проверяем RawText - если начинается с "# " (один символ #), это H1
                        return b.RawText.TrimStart().StartsWith("# ");
                    })
                    .OrderBy(b => b.StartLine)
                    .FirstOrDefault();

                if (firstHeading != null)
                {
                    // Извлекаем текст заголовка из RawText (убираем # символы)
                    var rawText = firstHeading.RawText.TrimStart();
                    var textStart = 0;
                    while (textStart < rawText.Length && rawText[textStart] == '#')
                        textStart++;
                    while (textStart < rawText.Length && char.IsWhiteSpace(rawText[textStart]))
                        textStart++;
                    
                    var headerText = rawText.Substring(textStart).Trim();
                    return !string.IsNullOrWhiteSpace(headerText) ? headerText : firstHeading.NormalizedText;
                }
            }

            // Fallback: имя документа из метаданных
            if (!string.IsNullOrWhiteSpace(document.Name))
            {
                return document.Name;
            }

            return "Заголовок не найден";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHeaderTool: ошибка при получении заголовка");
            return $"Ошибка при получении заголовка: {ex.Message}";
        }
    }

    private static string GetStringValue(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value))
        {
            throw new ArgumentException($"Missing required argument: {key}");
        }

        return value switch
        {
            string str => str,
            JsonElement jsonElement => jsonElement.GetString() ?? throw new InvalidOperationException($"Cannot convert {key} to string"),
            _ => value.ToString() ?? throw new InvalidOperationException($"Cannot convert {key} to string")
        };
    }
}

