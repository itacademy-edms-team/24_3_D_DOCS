using System.Text.Json;
using System.Text.RegularExpressions;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.ChangeDocTools;

public class GrepTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<GrepTool> _logger;

    public string Name => "grep";
    public string Description => "Ищет точные ключевые слова, фразы или regex-паттерны по всему документу и возвращает совпадающие строки с номерами.";

    public GrepTool(IDocumentService documentService, ILogger<GrepTool> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    public Dictionary<string, object> GetParametersSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["content"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Поисковый запрос или regex" }
            },
            ["required"] = new[] { "content" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetValue("document_id", out var _) || !arguments.TryGetValue("user_id", out var _))
            return "Ошибка: document_id и user_id обязательны для grep";

        var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
        var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
        var query = GetStringValue(arguments, "content");

        if (string.IsNullOrWhiteSpace(query)) return "Ошибка: content не должен быть пустым";

        var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
        if (document == null) return "Ошибка: Документ не найден";

        var lines = (document.Content ?? string.Empty).Split('\n');
        Regex? regex = null;
        try
        {
            if (query.IndexOfAny(new[] { '.', '*', '+', '?', '(', ')', '[', ']', '{', '}', '|', '\\' }) >= 0)
                regex = new Regex(query, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GrepTool: regex failed"); }

        var matches = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            bool isMatch = regex != null ? regex.IsMatch(line) : line.Contains(query, StringComparison.OrdinalIgnoreCase);
            if (isMatch) matches.Add($"{i + 1}: {line}");
        }

        if (matches.Count == 0) return "Совпадений не найдено";
        var limited = matches.Take(100).ToList();
        var result = string.Join("\n", limited);
        if (matches.Count > 100) result += $"\n... и ещё {matches.Count - 100} совпадений";
        return result;
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
