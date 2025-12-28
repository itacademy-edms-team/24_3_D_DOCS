using System.Text.Json;

namespace RusalProject.Services.Agent.Tools;

/// <summary>
/// get_datetime(): Returns the current date and time for timestamps or dynamic content.
/// </summary>
public class GetDateTimeTool : ITool
{
    private readonly ILogger<GetDateTimeTool> _logger;

    public string Name => "get_datetime";
    public string Description => "Возвращает текущие дату и время в ISO и человекочитаемом форматах.";

    public GetDateTimeTool(ILogger<GetDateTimeTool> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, object> GetParametersSchema()
    {
        // Параметров не требуется
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
            var utcNow = DateTime.UtcNow;

            var result = new
            {
                isoLocal = now.ToString("O"),
                isoUtc = utcNow.ToString("O"),
                date = now.ToString("yyyy-MM-dd"),
                time = now.ToString("HH:mm:ss"),
                display = now.ToString("dd.MM.yyyy HH:mm")
            };

            var json = System.Text.Json.JsonSerializer.Serialize(result);
            return Task.FromResult(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDateTimeTool: ошибка при получении даты/времени");
            return Task.FromResult($"Ошибка при получении даты/времени: {ex.Message}");
        }
    }
}

