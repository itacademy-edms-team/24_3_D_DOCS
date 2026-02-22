using System.Text.Json;
using System.Text.RegularExpressions;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

public class ProposeDocumentChangesTool : IDocumentAgentTool
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<ProposeDocumentChangesTool> _logger;

    public string Name => "propose_document_changes";
    public string Description => "Предлагает изменения документа, записывая их прямо в документ с маркерами. Пользователь увидит изменения и сможет принять или отклонить их.";

    public ProposeDocumentChangesTool(IDocumentService documentService, ILogger<ProposeDocumentChangesTool> logger)
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
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "insert", "delete", "replace" },
                    ["description"] = "Тип операции: insert, delete, replace."
                },
                ["start_line"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Строка начала (1-based). Для insert: строка ПОСЛЕ которой вставляем (0 = начало документа)."
                },
                ["end_line"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Конечная строка (1-based, для delete/replace опционально)."
                },
                ["content"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Новый markdown-контент (для insert/replace)."
                }
            },
            ["required"] = new[] { "operation", "start_line" }
        };
    }

    public async Task<DocumentToolResult> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        var result = new DocumentToolResult();

        try
        {
            if (!arguments.TryGetValue("document_id", out _) || !arguments.TryGetValue("user_id", out _))
            {
                result.ResultMessage = "Ошибка: document_id и user_id обязательны для propose_document_changes.";
                return result;
            }

            var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
            var userId = Guid.Parse(GetStringValue(arguments, "user_id"));

            var operation = GetStringValue(arguments, "operation").Trim().ToLowerInvariant();
            var startLine = GetIntValueFlexible(arguments, "start_line");
            var endLine = arguments.ContainsKey("end_line")
                ? GetIntValueFlexible(arguments, "end_line")
                : startLine;
            var content = arguments.TryGetValue("content", out _)
                ? (GetStringValue(arguments, "content") ?? string.Empty).Replace("\r\n", "\n")
                : string.Empty;

            var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
            if (document == null)
            {
                result.ResultMessage = "Ошибка: документ не найден.";
                return result;
            }

            var fullContent = (document.Content ?? string.Empty).Replace("\r\n", "\n");
            var lines = fullContent.Length == 0 ? new List<string>() : fullContent.Split('\n').ToList();
            var totalLines = lines.Count;

            if (operation == "insert")
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    result.ResultMessage = "Ошибка: content обязателен для insert.";
                    return result;
                }

                var changeId = Guid.NewGuid().ToString("N")[..12];
                var insertAfterLine = Math.Max(0, Math.Min(startLine, totalLines));
                
                var wrappedContent = $"<!-- AI:INSERT:{changeId} -->\n{content.Trim()}\n<!-- /AI:INSERT:{changeId} -->";
                var wrappedLines = wrappedContent.Split('\n').ToList();

                if (insertAfterLine > 0 && insertAfterLine <= lines.Count && 
                    !string.IsNullOrWhiteSpace(lines[insertAfterLine - 1]))
                {
                    wrappedLines.Insert(0, "");
                }
                if (insertAfterLine < lines.Count && 
                    !string.IsNullOrWhiteSpace(lines[insertAfterLine]))
                {
                    wrappedLines.Add("");
                }

                lines.InsertRange(insertAfterLine, wrappedLines);
                
                var updatedContent = string.Join("\n", lines);
                await _documentService.UpdateDocumentContentAsync(documentId, userId, updatedContent);

                result.ResultMessage = $"Вставка предложена (changeId: {changeId}). Контент добавлен после строки {insertAfterLine} с маркерами AI:INSERT.";
                return result;
            }

            if (operation == "delete")
            {
                var (rangeStart, rangeEnd) = NormalizeRange(startLine, endLine, totalLines);
                
                if (rangeStart > totalLines || totalLines == 0)
                {
                    result.ResultMessage = $"Ошибка: указанный диапазон {rangeStart}-{rangeEnd} выходит за пределы документа ({totalLines} строк).";
                    return result;
                }

                var changeId = Guid.NewGuid().ToString("N")[..12];
                
                var contentToDelete = string.Join("\n", lines.GetRange(rangeStart - 1, rangeEnd - rangeStart + 1));
                
                var wrappedContent = $"<!-- AI:DELETE:{changeId} -->\n{contentToDelete}\n<!-- /AI:DELETE:{changeId} -->";
                
                lines.RemoveRange(rangeStart - 1, rangeEnd - rangeStart + 1);
                lines.Insert(rangeStart - 1, wrappedContent);
                
                var updatedContent = string.Join("\n", lines);
                await _documentService.UpdateDocumentContentAsync(documentId, userId, updatedContent);

                result.ResultMessage = $"Удаление предложено (changeId: {changeId}). Строки {rangeStart}-{rangeEnd} обёрнуты маркерами AI:DELETE.";
                return result;
            }

            if (operation == "replace")
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    result.ResultMessage = "Ошибка: content обязателен для replace.";
                    return result;
                }

                var (rangeStart, rangeEnd) = NormalizeRange(startLine, endLine, totalLines);
                
                if (rangeStart > totalLines || totalLines == 0)
                {
                    result.ResultMessage = $"Ошибка: указанный диапазон {rangeStart}-{rangeEnd} выходит за пределы документа ({totalLines} строк).";
                    return result;
                }

                var deleteChangeId = Guid.NewGuid().ToString("N")[..12];
                var insertChangeId = Guid.NewGuid().ToString("N")[..12];
                
                var contentToDelete = string.Join("\n", lines.GetRange(rangeStart - 1, rangeEnd - rangeStart + 1));
                
                var deleteMarker = $"<!-- AI:DELETE:{deleteChangeId} -->\n{contentToDelete}\n<!-- /AI:DELETE:{deleteChangeId} -->";
                var insertMarker = $"<!-- AI:INSERT:{insertChangeId} -->\n{content.Trim()}\n<!-- /AI:INSERT:{insertChangeId} -->";
                
                var combinedReplacement = $"{deleteMarker}\n{insertMarker}";
                
                lines.RemoveRange(rangeStart - 1, rangeEnd - rangeStart + 1);
                lines.Insert(rangeStart - 1, combinedReplacement);
                
                var updatedContent = string.Join("\n", lines);
                await _documentService.UpdateDocumentContentAsync(documentId, userId, updatedContent);

                result.ResultMessage = $"Замена предложена. Строки {rangeStart}-{rangeEnd} помечены на удаление (changeId: {deleteChangeId}), новый контент добавлен (changeId: {insertChangeId}).";
                return result;
            }

            result.ResultMessage = $"Ошибка: неизвестная operation '{operation}'. Разрешены insert, delete, replace.";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProposeDocumentChangesTool: failed to execute");
            result.ResultMessage = $"Ошибка propose_document_changes: {ex.Message}";
            return result;
        }
    }

    private static (int Start, int End) NormalizeRange(int start, int end, int totalLines)
    {
        if (totalLines <= 0)
        {
            return (1, 1);
        }

        start = Math.Max(1, Math.Min(start, totalLines));
        end = Math.Max(1, Math.Min(end, totalLines));

        if (start > end)
        {
            (start, end) = (end, start);
        }

        return (start, end);
    }

    private static string GetStringValue(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value))
            throw new ArgumentException($"Missing required argument: {key}");
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
            throw new ArgumentException($"Missing required argument: {key}");

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
}
