using System.Text;
using System.Text.Json;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

public class ReadDocumentTool : IDocumentAgentTool
{
    private const int MaxReturnedLinesWithoutRange = 400;

    private readonly IDocumentService _documentService;
    private readonly ILogger<ReadDocumentTool> _logger;

    public string Name => "read_document";
    public string Description => "Читает документ целиком или в диапазоне строк (1-based). Возвращает текст с номерами строк.";

    public ReadDocumentTool(IDocumentService documentService, ILogger<ReadDocumentTool> logger)
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
                ["start_line"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Начальная строка (1-based, опционально)"
                },
                ["end_line"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Конечная строка (1-based, опционально)"
                }
            },
            ["required"] = Array.Empty<string>()
        };
    }

    public async Task<DocumentToolResult> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        var result = new DocumentToolResult();

        try
        {
            if (!arguments.TryGetValue("document_id", out _) || !arguments.TryGetValue("user_id", out _))
            {
                result.ResultMessage = "Ошибка: document_id и user_id обязательны для read_document.";
                return result;
            }

            var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
            var userId = Guid.Parse(GetStringValue(arguments, "user_id"));

            var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
            if (document == null)
            {
                result.ResultMessage = "Ошибка: документ не найден.";
                return result;
            }

            var content = (document.Content ?? string.Empty).Replace("\r\n", "\n");
            var lines = content.Length == 0 ? Array.Empty<string>() : content.Split('\n');
            var totalLines = lines.Length;

            var hasStart = arguments.ContainsKey("start_line");
            var hasEnd = arguments.ContainsKey("end_line");

            var startLine = hasStart ? GetIntValueFlexible(arguments, "start_line") : 1;
            var endLine = hasEnd ? GetIntValueFlexible(arguments, "end_line") : totalLines;

            if (!hasStart && !hasEnd && totalLines > MaxReturnedLinesWithoutRange)
            {
                endLine = MaxReturnedLinesWithoutRange;
            }

            startLine = Math.Max(1, Math.Min(startLine, totalLines == 0 ? 1 : totalLines));
            endLine = Math.Max(1, Math.Min(endLine, totalLines == 0 ? 1 : totalLines));

            if (startLine > endLine)
            {
                (startLine, endLine) = (endLine, startLine);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"document_name: {document.Name}");
            sb.AppendLine($"total_lines: {totalLines}");
            sb.AppendLine($"range: {startLine}-{endLine}");
            if (!hasStart && !hasEnd && totalLines > MaxReturnedLinesWithoutRange)
            {
                sb.AppendLine($"note: документ длинный, возвращены только первые {MaxReturnedLinesWithoutRange} строк. Для навигации используй start_line/end_line.");
            }
            sb.AppendLine("content:");

            if (totalLines == 0)
            {
                sb.AppendLine("(пустой документ)");
            }
            else
            {
                for (var i = startLine; i <= endLine; i++)
                {
                    sb.AppendLine($"{i}: {lines[i - 1]}");
                }
            }

            result.ResultMessage = sb.ToString().TrimEnd();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadDocumentTool: failed to execute");
            result.ResultMessage = $"Ошибка read_document: {ex.Message}";
            return result;
        }
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
