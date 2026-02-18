using System.Text.Json;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.ChangeDocTools;

public class EditTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<EditTool> _logger;

    public string Name => "edit";
    public string Description => "Заменяет строку с указанным 1-based ID. При многострочном контенте заменяет эту и последующие строки.";

    public EditTool(IDocumentService documentService, ILogger<EditTool> logger)
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
                ["id"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "1-based номер первой строки для замены" },
                ["content"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Новый многострочный Markdown-текст" }
            },
            ["required"] = new[] { "id", "content" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetValue("document_id", out var _) || !arguments.TryGetValue("user_id", out var _))
            return "Ошибка: document_id и user_id обязательны для edit";

        var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
        var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
        var id = GetIntValueFlexible(arguments, "id");
        var content = GetStringValue(arguments, "content");

        if (id <= 0) return "Ошибка: id должен быть >= 1 для edit";

        var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
        if (document == null) return "Ошибка: Документ не найден";

        var lines = (document.Content ?? string.Empty).Split('\n').ToList();
        if (lines.Count == 0) return "Ошибка: Документ пуст";

        var startIndex = id - 1;
        if (startIndex >= lines.Count) return $"Ошибка: id={id} вне диапазона документа";

        var newLines = (content ?? string.Empty).Split('\n').ToList();
        if (newLines.Count == 0) return "Предупреждение: content пустой";

        for (int i = 0; i < newLines.Count; i++)
        {
            var targetIndex = startIndex + i;
            if (targetIndex < lines.Count) lines[targetIndex] = newLines[i];
            else lines.Add(newLines[i]);
        }

        await _documentService.UpdateDocumentContentAsync(documentId, userId, string.Join("\n", lines));
        return $"edit: успешно заменено/добавлено {newLines.Count} строк(и)";
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

    private static int GetIntValueFlexible(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value)) throw new ArgumentException($"Missing required argument: {key}");
        return value switch
        {
            int i => i,
            long l => (int)l,
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Number => jsonElement.GetInt32(),
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String =>
                int.TryParse(jsonElement.GetString(), out var p) ? p : throw new InvalidOperationException($"Cannot convert {key} to int"),
            string s when int.TryParse(s, out var p) => p,
            _ => Convert.ToInt32(value)
        };
    }
}
