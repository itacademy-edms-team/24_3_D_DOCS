using System.Text.Json;
using RusalProject.Services.Documents;

namespace RusalProject.Services.Agent.Tools;

/// <summary>
/// insert(id, content): Inserts multi-line text strictly AFTER the line with the given ID (1-based).
/// document_id и user_id прокидываются агентом автоматически и не требуются от модели.
/// </summary>
public class InsertTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<InsertTool> _logger;

    public string Name => "insert";
    public string Description => "Вставляет многострочный текст строго ПОСЛЕ строки с указанным 1-based ID.";

    public InsertTool(
        IDocumentService documentService,
        ILogger<InsertTool> logger)
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
                    ["description"] = "1-based номер строки, ПОСЛЕ которой нужно вставить текст"
                },
                ["content"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Многострочный Markdown-текст для вставки"
                }
            },
            ["required"] = new[] { "id", "content" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!arguments.TryGetValue("document_id", out var documentIdObj) ||
                !arguments.TryGetValue("user_id", out var userIdObj))
            {
                return "Ошибка: document_id и user_id обязательны для insert";
            }

            var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
            var userId = Guid.Parse(GetStringValue(arguments, "user_id"));

            var id = GetIntValueFlexible(arguments, "id");
            var content = GetStringValue(arguments, "content");

            if (id < 0)
            {
                // Исправляем очевидную ошибку (например, 0-based номер)
                id += 1;
            }

            // Загружаем документ
            var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
            if (document == null)
            {
                return "Ошибка: Документ не найден";
            }

            var currentContent = document.Content ?? string.Empty;
            var lines = currentContent.Split('\n').ToList();
            var originalLineCount = lines.Count;

            // 1-based -> 0-based индекс для строки, после которой вставляем
            var insertAfterIndex = id - 1;

            var linesToInsert = (content ?? string.Empty).Split('\n').ToList();

            if (linesToInsert.Count == 0)
            {
                return "Предупреждение: content пустой, ничего не вставлено";
            }

            // Простая защита от дубликатов: если первый непустой заголовок уже есть в документе — не дублируем раздел
            var firstNonEmpty = linesToInsert.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
            if (!string.IsNullOrWhiteSpace(firstNonEmpty) && firstNonEmpty.StartsWith("#"))
            {
                var headerExists = lines.Any(l => l.Trim() == firstNonEmpty);
                if (headerExists)
                {
                    _logger.LogInformation(
                        "InsertTool: пропускаем вставку, так как заголовок '{Header}' уже существует в документе",
                        firstNonEmpty);
                    return $"insert: пропущено — заголовок '{firstNonEmpty}' уже есть в документе, используй edit для изменения существующего раздела";
                }
            }

            // Если id выходит за границы - вставляем в конец
            if (insertAfterIndex < 0 || insertAfterIndex >= lines.Count)
            {
                lines.AddRange(linesToInsert);
                _logger.LogInformation(
                    "InsertTool: id {Id} вне диапазона (строк: {LineCount}), добавляем {Inserted} строк(и) в конец документа",
                    id, originalLineCount, linesToInsert.Count);
            }
            else
            {
                // Вставляем строки строго после insertAfterIndex
                for (int i = 0; i < linesToInsert.Count; i++)
                {
                    lines.Insert(insertAfterIndex + 1 + i, linesToInsert[i]);
                }

                _logger.LogInformation(
                    "InsertTool: вставлено {Inserted} строк(и) после строки {Id} (изначально строк: {LineCount})",
                    linesToInsert.Count, id, originalLineCount);
            }

            var newContent = string.Join("\n", lines);
            await _documentService.UpdateDocumentContentAsync(documentId, userId, newContent);

            return $"insert: успешно вставлено {linesToInsert.Count} строк(и) после строки {id}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertTool: ошибка при выполнении insert");
            return $"Ошибка при выполнении insert: {ex.Message}";
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

