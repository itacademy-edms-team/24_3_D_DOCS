using System.Text.Json;
using RusalProject.Services.Documents;

namespace RusalProject.Services.Agent.Tools;

/// <summary>
/// delete(id, [id_end]): Deletes the line at ID (1-based). 
/// If id_end is provided, deletes the entire range from ID to id_end inclusive.
/// </summary>
public class DeleteTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DeleteTool> _logger;

    public string Name => "delete";
    public string Description => "Удаляет строку по 1-based ID или диапазон строк [id, id_end] включительно.";

    public DeleteTool(
        IDocumentService documentService,
        ILogger<DeleteTool> logger)
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
                ["id"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "1-based номер первой строки для удаления"
                },
                ["id_end"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "1-based номер последней строки для удаления (опционально)"
                }
            },
            ["required"] = new[] { "id" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!arguments.TryGetValue("document_id", out var _) ||
                !arguments.TryGetValue("user_id", out var _))
            {
                return "Ошибка: document_id и user_id обязательны для delete";
            }

            var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
            var userId = Guid.Parse(GetStringValue(arguments, "user_id"));

            var id = GetIntValueFlexible(arguments, "id");
            int? idEnd = null;
            if (arguments.ContainsKey("id_end"))
            {
                try
                {
                    idEnd = GetIntValueFlexible(arguments, "id_end");
                }
                catch
                {
                    // Плохой id_end — просто игнорируем и удаляем только одну строку
                    idEnd = null;
                }
            }

            if (id <= 0)
            {
                return "Ошибка: id должен быть >= 1 для delete";
            }

            var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
            if (document == null)
            {
                return "Ошибка: Документ не найден";
            }

            var lines = (document.Content ?? string.Empty).Split('\n').ToList();
            var originalLineCount = lines.Count;

            if (lines.Count == 0)
            {
                return "Ошибка: Документ пуст, нечего удалять";
            }

            var startIndex = id - 1;
            if (startIndex >= lines.Count)
            {
                return $"Ошибка: id={id} вне диапазона документа (строк: {lines.Count})";
            }

            var endIndex = startIndex;
            if (idEnd.HasValue)
            {
                if (idEnd.Value <= 0)
                {
                    return "Ошибка: id_end должен быть >= 1 для delete";
                }

                // Нормализуем порядок, если id_end < id
                var minId = Math.Min(id, idEnd.Value);
                var maxId = Math.Max(id, idEnd.Value);
                startIndex = minId - 1;
                endIndex = maxId - 1;
            }

            endIndex = Math.Min(endIndex, lines.Count - 1);
            startIndex = Math.Max(startIndex, 0);

            var deleteCount = endIndex - startIndex + 1;
            lines.RemoveRange(startIndex, deleteCount);

            _logger.LogInformation(
                "DeleteTool: удалено {DeleteCount} строк(и) в диапазоне [{StartId}, {EndId}] (изначально строк: {OriginalLineCount}, теперь: {NewLineCount})",
                deleteCount, startIndex + 1, endIndex + 1, originalLineCount, lines.Count);

            var newContent = string.Join("\n", lines);
            await _documentService.UpdateDocumentContentAsync(documentId, userId, newContent);

            if (deleteCount == 1)
            {
                return $"delete: успешно удалена строка {startIndex + 1}";
            }

            return $"delete: успешно удалены строки {startIndex + 1}-{endIndex + 1}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteTool: ошибка при выполнении delete");
            return $"Ошибка при выполнении delete: {ex.Message}";
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

    private static int GetIntValueFlexible(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value))
        {
            throw new ArgumentException($"Missing required argument: {key}");
        }

        try
        {
            return value switch
            {
                int i => i,
                long l => (int)l,
                JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Number => jsonElement.GetInt32(),
                JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String =>
                    int.TryParse(jsonElement.GetString(), out var parsedFromString)
                        ? parsedFromString
                        : throw new InvalidOperationException($"Cannot convert {key} string value to int"),
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => Convert.ToInt32(value)
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot convert argument '{key}' to int: {ex.Message}", ex);
        }
    }
}

