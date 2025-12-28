using System.Linq;
using System.Text;
using System.Text.Json;
using RusalProject.Services.Document;
using RusalProject.Services.Storage;

namespace RusalProject.Services.Agent.Tools;

public class DocumentEditTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly IMinioService _minioService;
    private readonly ILogger<DocumentEditTool> _logger;

    public string Name => "document_edit";
    public string Description => "Редактирует документ: вставляет текст после указанной строки, обновляет строку или удаляет строку.";

    public DocumentEditTool(
        IDocumentService documentService,
        IMinioService minioService,
        ILogger<DocumentEditTool> logger)
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
                ["document_id"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "ID документа"
                },
                ["user_id"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "ID пользователя"
                },
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "insert", "update", "delete" },
                    ["description"] = "Тип операции: insert (вставить после строки), update (обновить строку), delete (удалить строку)"
                },
                ["line_number"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Номер строки (1-based)"
                },
                ["text"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Текст для вставки/обновления (не требуется для delete)"
                }
            },
            ["required"] = new[] { "document_id", "user_id", "operation", "line_number" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
            var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
            var operation = GetStringValue(arguments, "operation");
            var lineNumber = GetIntValue(arguments, "line_number") - 1; // Convert to 0-based

            // Get current document content
            var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
            if (document == null)
            {
                return "Ошибка: Документ не найден";
            }

            var lines = (document.Content ?? string.Empty).Split('\n').ToList();
            var lineNumber1Based = GetIntValue(arguments, "line_number");
            _logger.LogInformation("DocumentEditTool: Starting {Operation} operation. Document has {LineCount} lines. LineNumber (1-based): {LineNumber1Based}, LineNumber (0-based): {LineNumber0Based}", 
                operation, lines.Count, lineNumber1Based, lineNumber);

            switch (operation.ToLower())
            {
                case "insert":
                    if (!arguments.ContainsKey("text"))
                    {
                        return "Ошибка: text обязателен для операции insert";
                    }
                    var textToInsert = GetStringValue(arguments, "text");
                    
                    // Log context around insertion point
                    if (lineNumber >= 0 && lineNumber < lines.Count)
                    {
                        var contextStart = Math.Max(0, lineNumber - 2);
                        var contextEnd = Math.Min(lines.Count - 1, lineNumber + 2);
                        var contextLines = Enumerable.Range(contextStart, contextEnd - contextStart + 1)
                            .Select(i => $"  Line {i + 1}: {(i == lineNumber ? ">>> " : "    ")}{lines[i]}")
                            .ToList();
                        _logger.LogInformation("DocumentEditTool: Context around insertion point (line {LineNumber}):\n{ContextLines}", 
                            lineNumber1Based, string.Join("\n", contextLines));
                    }
                    
                    _logger.LogInformation("DocumentEditTool: Inserting text: {TextToInsert}", textToInsert);
                    
                    // Split text on newlines to handle multi-line inserts properly
                    var linesToInsert = textToInsert.Split('\n').ToList();
                    
                    if (lineNumber < 0 || lineNumber >= lines.Count)
                    {
                        // Append to end
                        lines.AddRange(linesToInsert);
                        _logger.LogInformation("DocumentEditTool: Added {LineCount} line(s) to end of document (lineNumber out of range)", linesToInsert.Count);
                    }
                    else
                    {
                        // Insert all lines at the correct position
                        for (int i = 0; i < linesToInsert.Count; i++)
                        {
                            lines.Insert(lineNumber + 1 + i, linesToInsert[i]);
                        }
                        _logger.LogInformation("DocumentEditTool: Inserted {LineCount} line(s) after line {LineNumber} (1-based: {LineNumber1Based})", 
                            linesToInsert.Count, lineNumber, lineNumber1Based);
                    }
                    break;

                case "update":
                    if (!arguments.ContainsKey("text"))
                    {
                        return "Ошибка: text обязателен для операции update";
                    }
                    var textToUpdate = GetStringValue(arguments, "text");
                    if (lineNumber < 0 || lineNumber >= lines.Count)
                    {
                        return "Ошибка: Номер строки вне диапазона";
                    }
                    lines[lineNumber] = textToUpdate;
                    break;

                case "delete":
                    if (lineNumber < 0 || lineNumber >= lines.Count)
                    {
                        return "Ошибка: Номер строки вне диапазона";
                    }
                    lines.RemoveAt(lineNumber);
                    break;

                default:
                    return $"Ошибка: Неизвестная операция {operation}";
            }

            var newContent = string.Join("\n", lines);
            _logger.LogInformation("DocumentEditTool: New content length: {NewContentLength} characters, {NewLineCount} lines", newContent.Length, lines.Count);
            
            await _documentService.UpdateDocumentContentAsync(documentId, userId, newContent);
            _logger.LogInformation("DocumentEditTool: Content saved to MinIO successfully");

            return $"Операция {operation} выполнена успешно";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing document edit tool");
            return $"Ошибка при редактировании документа: {ex.Message}";
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

    private static int GetIntValue(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value))
        {
            throw new ArgumentException($"Missing required argument: {key}");
        }

        return value switch
        {
            int i => i,
            long l => (int)l,
            JsonElement jsonElement => jsonElement.GetInt32(),
            _ => Convert.ToInt32(value)
        };
    }
}
