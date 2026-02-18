using System.Text.Json;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.CRUDdocTools;

/// <summary>
/// Returns list of documents with id, name, updatedAt, status. No content.
/// </summary>
public class ListDocumentTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<ListDocumentTool> _logger;

    public string Name => "list_documents";
    public string Description => "Возвращает список всех документов пользователя: id, название, дата изменения, статус. Без содержимого.";

    public ListDocumentTool(IDocumentService documentService, ILogger<ListDocumentTool> logger)
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
                ["status"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Фильтр статуса: draft, archived (опционально)"
                },
                ["search"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Поиск по названию (опционально)"
                }
            },
            ["required"] = Array.Empty<string>()
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetValue("user_id", out var _))
            return "Ошибка: user_id обязателен";

        var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
        string? status = GetStringValueSafe(arguments, "status");
        string? search = GetStringValueSafe(arguments, "search");

        var documents = await _documentService.GetDocumentsAsync(userId, status, search);

        var result = documents.Select(d => new
        {
            id = d.Id.ToString(),
            name = d.Name,
            updatedAt = d.UpdatedAt.ToString("O"),
            status = d.Status
        }).ToList();

        return System.Text.Json.JsonSerializer.Serialize(result);
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

    private static string? GetStringValueSafe(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || value == null) return null;
        if (value is string str && !string.IsNullOrWhiteSpace(str)) return str;
        if (value is JsonElement je)
        {
            var s = je.GetString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        return value.ToString();
    }
}
