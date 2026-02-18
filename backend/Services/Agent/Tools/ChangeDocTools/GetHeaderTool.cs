using System.Text.Json;
using RusalProject.Models.Types;
using RusalProject.Services.Document;
using RusalProject.Services.Markdown;

namespace RusalProject.Services.Agent.Tools.ChangeDocTools;

public class GetHeaderTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly IMarkdownParserService _markdownParserService;
    private readonly ILogger<GetHeaderTool> _logger;

    public string Name => "get_header";
    public string Description => "Возвращает заголовок документа: первый H1 в Markdown или имя документа.";

    public GetHeaderTool(IDocumentService documentService, IMarkdownParserService markdownParserService, ILogger<GetHeaderTool> logger)
    {
        _documentService = documentService;
        _markdownParserService = markdownParserService;
        _logger = logger;
    }

    public Dictionary<string, object> GetParametersSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>(),
            ["required"] = Array.Empty<string>()
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetValue("document_id", out var _) || !arguments.TryGetValue("user_id", out var _))
            return "Ошибка: document_id и user_id обязательны";

        var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
        var userId = Guid.Parse(GetStringValue(arguments, "user_id"));

        var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
        if (document == null) return "Ошибка: Документ не найден";

        var content = document.Content ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(content))
        {
            var blocks = _markdownParserService.ParseDocument(content);
            var firstHeading = blocks
                .Where(b => b.BlockType == BlockType.Heading)
                .Where(b => b.RawText.TrimStart().StartsWith("# "))
                .OrderBy(b => b.StartLine)
                .FirstOrDefault();

            if (firstHeading != null)
            {
                var rawText = firstHeading.RawText.TrimStart();
                var textStart = 0;
                while (textStart < rawText.Length && rawText[textStart] == '#') textStart++;
                while (textStart < rawText.Length && char.IsWhiteSpace(rawText[textStart])) textStart++;
                var headerText = rawText.Substring(textStart).Trim();
                return !string.IsNullOrWhiteSpace(headerText) ? headerText : firstHeading.NormalizedText;
            }
        }

        return !string.IsNullOrWhiteSpace(document.Name) ? document.Name : "Заголовок не найден";
    }

    private static string GetStringValue(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value)) throw new ArgumentException($"Missing required argument: {key}");
        return value switch
        {
            string str => str,
            JsonElement jsonElement => jsonElement.GetString() ?? throw new InvalidOperationException($"Cannot convert {key} to string"),
            _ => value.ToString() ?? throw new InvalidOperationException($"Cannot convert {key} to string")
        };
    }
}
