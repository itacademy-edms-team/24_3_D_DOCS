using System.Text.Json;
using System.Text.RegularExpressions;
using RusalProject.Services.Documents;

namespace RusalProject.Services.Agent.Tools;

/// <summary>
/// grep(content): Searches for exact keywords, phrases, or regex patterns across the entire document, returning matching lines.
/// </summary>
public class GrepTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<GrepTool> _logger;

    public string Name => "grep";
    public string Description => "Ищет точные ключевые слова, фразы или regex-паттерны по всему документу и возвращает совпадающие строки с номерами.";

    public GrepTool(
        IDocumentService documentService,
        ILogger<GrepTool> logger)
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
                ["content"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Поисковый запрос: ключевые слова или regex-паттерн"
                }
            },
            ["required"] = new[] { "content" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!arguments.TryGetValue("document_id", out var _) ||
                !arguments.TryGetValue("user_id", out var _))
            {
                return "Ошибка: document_id и user_id обязательны для grep";
            }

            var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
            var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
            var query = GetStringValue(arguments, "content");

            if (string.IsNullOrWhiteSpace(query))
            {
                return "Ошибка: content для grep не должен быть пустым";
            }

            var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
            if (document == null)
            {
                return "Ошибка: Документ не найден";
            }

            var content = document.Content ?? string.Empty;
            var lines = content.Split('\n');

            // Пытаемся интерпретировать как regex, но безопасно падаем в простой поиск при ошибке
            bool useRegex = false;
            Regex? regex = null;

            try
            {
                // Простая эвристика: если есть специальные regex-символы, пробуем как regex
                if (query.IndexOfAny(new[] { '.', '*', '+', '?', '(', ')', '[', ']', '{', '}', '|', '\\' }) >= 0)
                {
                    regex = new Regex(query, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    useRegex = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GrepTool: не удалось интерпретировать запрос как regex, будет использован обычный поиск");
                useRegex = false;
                regex = null;
            }

            var matches = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                bool isMatch;

                if (useRegex && regex != null)
                {
                    isMatch = regex.IsMatch(line);
                }
                else
                {
                    isMatch = line.Contains(query, StringComparison.OrdinalIgnoreCase);
                }

                if (isMatch)
                {
                    matches.Add($"{i + 1}: {line}");
                }
            }

            if (matches.Count == 0)
            {
                return "Совпадений не найдено";
            }

            // Ограничиваем размер ответа
            var limitedMatches = matches.Take(100).ToList();
            var resultText = string.Join("\n", limitedMatches);

            if (matches.Count > limitedMatches.Count)
            {
                resultText += $"\n... и ещё {matches.Count - limitedMatches.Count} совпадений";
            }

            return resultText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GrepTool: ошибка при выполнении grep");
            return $"Ошибка при выполнении grep: {ex.Message}";
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
}

