using System.Text.Json;
using RusalProject.Models.DTOs.Document;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.CRUDdocTools;

/// <summary>
/// Creates a new document with name, optional description and initialContent.
/// </summary>
public class CreateDocumentTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<CreateDocumentTool> _logger;

    public string Name => "create_document";
    public string Description => "Создаёт новый документ. Параметры: name (обязательно), description, initialContent (опционально).";

    public CreateDocumentTool(IDocumentService documentService, ILogger<CreateDocumentTool> logger)
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
                ["name"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Название документа (обязательно)"
                },
                ["description"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Описание документа (опционально)"
                },
                ["initialContent"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Начальный Markdown-контент документа (опционально)"
                }
            },
            ["required"] = new[] { "name" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetValue("user_id", out var _))
            return "Ошибка: user_id обязателен";

        var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
        var name = GetStringValue(arguments, "name");

        if (string.IsNullOrWhiteSpace(name))
            return "Ошибка: name не должен быть пустым";

        var dto = new CreateDocumentDTO
        {
            Name = name.Trim(),
            Description = GetStringValueOptional(arguments, "description"),
            InitialContent = GetStringValueOptional(arguments, "initialContent")
        };

        var document = await _documentService.CreateDocumentAsync(userId, dto);
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            id = document.Id.ToString(),
            name = document.Name,
            created = true,
            message = $"Документ «{document.Name}» создан"
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

    private static string? GetStringValueOptional(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || value == null) return null;
        var s = value switch
        {
            string str => str,
            JsonElement je => je.GetString(),
            _ => value.ToString()
        };
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
