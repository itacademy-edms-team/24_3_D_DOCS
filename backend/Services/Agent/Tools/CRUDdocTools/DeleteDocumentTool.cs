using System.Text.Json;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.CRUDdocTools;

/// <summary>
/// Soft-deletes a document by id.
/// </summary>
public class DeleteDocumentTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DeleteDocumentTool> _logger;

    public string Name => "delete_document";
    public string Description => "Удаляет документ по ID (мягкое удаление — можно восстановить из архива).";

    public DeleteDocumentTool(IDocumentService documentService, ILogger<DeleteDocumentTool> logger)
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
                    ["description"] = "ID документа для удаления"
                }
            },
            ["required"] = new[] { "document_id" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetValue("user_id", out var _))
            return "Ошибка: user_id обязателен";

        var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
        var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));

        await _documentService.DeleteDocumentAsync(documentId, userId);
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            documentId = documentId.ToString(),
            deleted = true,
            message = "Документ удалён (можно восстановить из архива)"
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
