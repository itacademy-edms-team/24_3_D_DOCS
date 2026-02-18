using System.Linq;
using System.Text.Json;
using RusalProject.Services.Document;
using RusalProject.Services.Storage;

namespace RusalProject.Services.Agent.Tools.ChangeDocTools;

public class DocumentEditTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly IMinioService _minioService;
    private readonly ILogger<DocumentEditTool> _logger;

    public string Name => "document_edit";
    public string Description => "Редактирует документ: вставляет текст после указанной строки, обновляет строку или удаляет строку.";

    public DocumentEditTool(IDocumentService documentService, IMinioService minioService, ILogger<DocumentEditTool> logger)
    {
        _documentService = documentService;
        _minioService = minioService;
        _logger = logger;
    }

    public Dictionary<string, object> GetParametersSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["document_id"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ID документа" },
                ["user_id"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ID пользователя" },
                ["operation"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = new[] { "insert", "update", "delete" }, ["description"] = "Тип операции" },
                ["line_number"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "Номер строки (1-based)" },
                ["text"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Текст для insert/update" }
            },
            ["required"] = new[] { "document_id", "user_id", "operation", "line_number" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
        var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
        var operation = GetStringValue(arguments, "operation");
        var lineNumber = GetIntValue(arguments, "line_number") - 1;

        var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
        if (document == null) return "Ошибка: Документ не найден";

        var lines = (document.Content ?? string.Empty).Split('\n').ToList();

        switch (operation.ToLower())
        {
            case "insert":
                if (!arguments.ContainsKey("text")) return "Ошибка: text обязателен для insert";
                var textToInsert = GetStringValue(arguments, "text");
                var linesToInsert = textToInsert.Split('\n').ToList();
                if (lineNumber < 0 || lineNumber >= lines.Count)
                    lines.AddRange(linesToInsert);
                else
                    for (int i = 0; i < linesToInsert.Count; i++)
                        lines.Insert(lineNumber + 1 + i, linesToInsert[i]);
                break;
            case "update":
                if (!arguments.ContainsKey("text")) return "Ошибка: text обязателен для update";
                if (lineNumber < 0 || lineNumber >= lines.Count) return "Ошибка: Номер строки вне диапазона";
                lines[lineNumber] = GetStringValue(arguments, "text");
                break;
            case "delete":
                if (lineNumber < 0 || lineNumber >= lines.Count) return "Ошибка: Номер строки вне диапазона";
                lines.RemoveAt(lineNumber);
                break;
            default:
                return $"Ошибка: Неизвестная операция {operation}";
        }

        await _documentService.UpdateDocumentContentAsync(documentId, userId, string.Join("\n", lines));
        return $"Операция {operation} выполнена успешно";
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

    private static int GetIntValue(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value)) throw new ArgumentException($"Missing required argument: {key}");
        return value switch
        {
            int i => i, long l => (int)l,
            JsonElement jsonElement => jsonElement.GetInt32(),
            _ => Convert.ToInt32(value)
        };
    }
}
