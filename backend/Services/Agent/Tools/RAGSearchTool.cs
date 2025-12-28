using System.Text.Json;
using RusalProject.Services.RAG;

namespace RusalProject.Services.Agent.Tools;

public class RAGSearchTool : ITool
{
    private readonly IRAGService _ragService;

    // Имя инструмента соответствует новому системному промпту: rag_query(content)
    public string Name => "rag_query";
    public string Description => "Выполняет семантический поиск по документу и возвращает релевантные диапазоны строк с текстом.";

    public RAGSearchTool(IRAGService ragService)
    {
        _ragService = ragService;
    }

    public Dictionary<string, object> GetParametersSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                // document_id добавляется агентом автоматически
                ["content"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Поисковый запрос"
                },
                ["top_k"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Количество результатов (по умолчанию 5)",
                    ["default"] = 5
                }
            },
            ["required"] = new[] { "content" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.ContainsKey("document_id"))
        {
            return "Ошибка: document_id обязателен для rag_query";
        }

        var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
        
        // Извлекаем userId из аргументов (добавляется автоматически AgentService)
        if (!arguments.ContainsKey("user_id"))
        {
            return "Ошибка: user_id обязателен для rag_query";
        }
        var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
        
        // Поддерживаем и новый ключ content, и старый query для обратной совместимости
        string query;
        if (arguments.ContainsKey("content"))
        {
            query = GetStringValue(arguments, "content");
        }
        else if (arguments.ContainsKey("query"))
        {
            query = GetStringValue(arguments, "query");
        }
        else
        {
            return "Ошибка: content (или query) обязателен для rag_query";
        }

        var topK = arguments.ContainsKey("top_k") ? GetIntValue(arguments, "top_k") : 5;

        var results = await _ragService.SearchAsync(documentId, userId, query, topK, cancellationToken);

        if (results.Count == 0)
        {
            return "Не найдено результатов по запросу";
        }

        var resultText = string.Join("\n\n", results.Select((r, i) =>
        {
            var text = r.BlockType == "Image" ? r.RawText : r.NormalizedText;
            return $"Результат {i + 1} (строки {r.StartLine}-{r.EndLine}, тип: {r.BlockType}):\n{text}";
        }));

        return resultText;
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
