using System.Text.Json;
using RusalProject.Models.DTOs.Document;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.CRUDdocTools;

/// <summary>
/// Renames a document by id.
/// </summary>
public class RenameDocumentTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<RenameDocumentTool> _logger;

    public string Name => "rename_document";
    public string Description => "Переименовывает документ по ID. Параметры: document_id, name.";

    public RenameDocumentTool(IDocumentService documentService, ILogger<RenameDocumentTool> logger)
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
                ["document_id"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "ID документа для переименования"
                },
                ["name"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Новое название документа"
                }
            },
            ["required"] = new[] { "document_id", "name" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetValue("user_id", out var _))
            return "Ошибка: user_id обязателен";

        var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
        var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
        var newName = GetStringValue(arguments, "name").Trim();

        if (string.IsNullOrWhiteSpace(newName))
            return "Ошибка: name не должен быть пустым";

        var updated = await _documentService.UpdateDocumentAsync(documentId, userId, new UpdateDocumentDTO
        {
            Name = newName
        });

        return JsonSerializer.Serialize(new
        {
            renamed = true,
            name = updated.Name,
            message = $"Документ переименован в «{updated.Name}»"
        });
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
