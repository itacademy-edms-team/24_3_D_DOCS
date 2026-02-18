namespace RusalProject.Services.Agent.Tools.ChangeDocTools;

public class GetDateTimeTool : ITool
{
    private readonly ILogger<GetDateTimeTool> _logger;

    public string Name => "get_datetime";
    public string Description => "Возвращает текущие дату и время в ISO и человекочитаемом форматах.";

    public GetDateTimeTool(ILogger<GetDateTimeTool> logger) => _logger = logger;

    public Dictionary<string, object> GetParametersSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>(),
            ["required"] = Array.Empty<string>()
        };
    }

    public Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.Now;
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                isoLocal = now.ToString("O"),
                isoUtc = DateTime.UtcNow.ToString("O"),
                date = now.ToString("yyyy-MM-dd"),
                time = now.ToString("HH:mm:ss"),
                display = now.ToString("dd.MM.yyyy HH:mm")
            });
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDateTimeTool: ошибка");
            return Task.FromResult($"Ошибка: {ex.Message}");
        }
    }
}
