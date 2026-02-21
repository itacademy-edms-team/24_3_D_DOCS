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
    public string Description => "Предлагает изменения документа как атомарные сущности (insert/delete), не изменяя документ напрямую.";

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
            var lines = fullContent.Length == 0 ? Array.Empty<string>() : fullContent.Split('\n');
            var totalLines = lines.Length;

            var existingEntities = ParseEntitiesFromLines(lines);
            var proposedChanges = new List<DocumentEntityChangeDTO>();

            if (operation == "insert")
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    result.ResultMessage = "Ошибка: content обязателен для insert.";
                    return result;
                }

                var insertAfterLine = Math.Max(0, Math.Min(startLine, totalLines));
                var insertEntities = ParseEntitiesFromText(content);
                if (insertEntities.Count == 0)
                {
                    result.ResultMessage = "Ошибка: не удалось выделить сущности для вставки (content пуст или состоит только из пустых строк).";
                    return result;
                }

                var groupId = Guid.NewGuid().ToString("N");
                for (var i = 0; i < insertEntities.Count; i++)
                {
                    var entity = insertEntities[i];
                    proposedChanges.Add(new DocumentEntityChangeDTO
                    {
                        ChangeId = Guid.NewGuid().ToString("N"),
                        ChangeType = "insert",
                        EntityType = entity.EntityType,
                        StartLine = insertAfterLine,
                        EndLine = null,
                        Content = entity.Content,
                        GroupId = groupId,
                        Order = i
                    });
                }

                result.DocumentChanges = proposedChanges;
                result.ResultMessage = $"Предложено {proposedChanges.Count} вставок по сущностям.";
                return result;
            }

            if (operation == "delete")
            {
                var (rangeStart, rangeEnd) = NormalizeRange(startLine, endLine, totalLines);
                var deleteEntities = GetIntersectingEntities(existingEntities, rangeStart, rangeEnd);

                if (deleteEntities.Count == 0)
                {
                    result.ResultMessage = $"В указанном диапазоне {rangeStart}-{rangeEnd} не найдено сущностей для удаления.";
                    return result;
                }

                foreach (var entity in deleteEntities)
                {
                    proposedChanges.Add(new DocumentEntityChangeDTO
                    {
                        ChangeId = Guid.NewGuid().ToString("N"),
                        ChangeType = "delete",
                        EntityType = entity.EntityType,
                        StartLine = entity.StartLine,
                        EndLine = entity.EndLine,
                        Content = entity.Content
                    });
                }

                result.DocumentChanges = proposedChanges;
                result.ResultMessage = $"Предложено {proposedChanges.Count} удалений по сущностям.";
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
                var deleteEntities = GetIntersectingEntities(existingEntities, rangeStart, rangeEnd);

                foreach (var entity in deleteEntities)
                {
                    proposedChanges.Add(new DocumentEntityChangeDTO
                    {
                        ChangeId = Guid.NewGuid().ToString("N"),
                        ChangeType = "delete",
                        EntityType = entity.EntityType,
                        StartLine = entity.StartLine,
                        EndLine = entity.EndLine,
                        Content = entity.Content
                    });
                }

                var insertAfterLine = Math.Max(rangeStart - 1, 0);
                var insertEntities = ParseEntitiesFromText(content);
                var groupId = Guid.NewGuid().ToString("N");

                for (var i = 0; i < insertEntities.Count; i++)
                {
                    var entity = insertEntities[i];
                    proposedChanges.Add(new DocumentEntityChangeDTO
                    {
                        ChangeId = Guid.NewGuid().ToString("N"),
                        ChangeType = "insert",
                        EntityType = entity.EntityType,
                        StartLine = insertAfterLine,
                        EndLine = null,
                        Content = entity.Content,
                        GroupId = groupId,
                        Order = i
                    });
                }

                result.DocumentChanges = proposedChanges;
                result.ResultMessage = $"Предложено {deleteEntities.Count} удалений и {insertEntities.Count} вставок (replace по сущностям).";
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

    private static List<ParsedEntity> GetIntersectingEntities(List<ParsedEntity> entities, int startLine, int endLine)
    {
        return entities
            .Where(e => e.EndLine >= startLine && e.StartLine <= endLine)
            .ToList();
    }

    private static List<ParsedEntity> ParseEntitiesFromText(string content)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        return ParseEntitiesFromLines(lines);
    }

    private static List<ParsedEntity> ParseEntitiesFromLines(string[] lines)
    {
        var entities = new List<ParsedEntity>();
        var i = 0;

        while (i < lines.Length)
        {
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
            }

            if (i >= lines.Length)
            {
                break;
            }

            var start = i + 1; // 1-based
            var chunkLines = new List<string>();
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                chunkLines.Add(lines[i]);
                i++;
            }

            var end = i; // 1-based
            var chunk = string.Join("\n", chunkLines).TrimEnd();
            var entityType = DetectEntityType(chunkLines);

            entities.Add(new ParsedEntity
            {
                StartLine = start,
                EndLine = end,
                Content = chunk,
                EntityType = entityType
            });
        }

        return entities;
    }

    private static string DetectEntityType(IReadOnlyList<string> chunkLines)
    {
        if (chunkLines.Count == 0)
        {
            return "paragraph";
        }

        var first = chunkLines[0].TrimStart();
        if (first.StartsWith("#"))
        {
            return "heading";
        }
        if (first.StartsWith("![")) // markdown image
        {
            return "image";
        }
        if (Regex.IsMatch(first, @"^\[(IMAGE|TABLE|FORMULA)-CAPTION:", RegexOptions.IgnoreCase))
        {
            return "caption";
        }
        if (first.StartsWith("```"))
        {
            return "code";
        }
        if (first.StartsWith("\\["))
        {
            return "formula";
        }
        if (first.StartsWith(">"))
        {
            return "quote";
        }
        if (Regex.IsMatch(first, @"^(\s*)[-*+]\s+") || Regex.IsMatch(first, @"^(\s*)\d+\.\s+"))
        {
            return "list";
        }
        if (first.Contains("|") && first.Count(c => c == '|') >= 2)
        {
            return "table";
        }
        if (Regex.IsMatch(first, @"^(\*{3,}|-{3,}|_{3,}|~{3,})$"))
        {
            return "horizontal_rule";
        }
        return "paragraph";
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

    private class ParsedEntity
    {
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string Content { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
    }
}
