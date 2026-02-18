namespace RusalProject.Services.Agent.Tools.ChangeDocTools;

public class WebSearchTool : ITool
{
    public string Name => "web_search";
    public string Description => "Выполняет поиск в интернете (заглушка для будущей реализации).";

    public Dictionary<string, object> GetParametersSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["query"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Поисковый запрос" }
            },
            ["required"] = new[] { "query" }
        };
    }

    public Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
        => Task.FromResult("Веб-поиск пока не реализован");
}
