using System.Text.Json;
using RusalProject.Services.RAG;

namespace RusalProject.Services.Agent.Tools;

public class TableSearchTool : ITool
{
    private readonly IRAGService _ragService;

    public string Name => "table_search";
    public string Description => "Ищет все таблицы в документе. Возвращает список таблиц с их позициями и содержимым.";

    public TableSearchTool(IRAGService ragService)
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
                ["document_id"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "ID документа для поиска таблиц"
                }
            },
            ["required"] = new[] { "document_id" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.ContainsKey("document_id"))
        {
            return "Ошибка: document_id обязателен";
        }

        var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
        
        // Извлекаем userId из аргументов (добавляется автоматически AgentService)
        if (!arguments.ContainsKey("user_id"))
        {
            return "Ошибка: user_id обязателен для table_search";
        }
        var userId = Guid.Parse(GetStringValue(arguments, "user_id"));

        var tables = await _ragService.SearchTablesAsync(documentId, userId, cancellationToken);

        if (tables.Count == 0)
        {
            return "Таблицы не найдены в документе";
        }

        var resultText = string.Join("\n\n", tables.Select((t, i) =>
            $"Таблица {i + 1} (строки {t.StartLine}-{t.EndLine}):\n{(t.RawText ?? t.NormalizedText)}"
        ));

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
}
