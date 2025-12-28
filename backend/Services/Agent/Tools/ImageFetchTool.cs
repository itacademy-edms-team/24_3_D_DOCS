namespace RusalProject.Services.Agent.Tools;

public class ImageFetchTool : ITool
{
    public string Name => "image_fetch";
    public string Description => "Получает изображение по URL (заглушка для будущей реализации). Используй только с явного согласия пользователя.";

    public Dictionary<string, object> GetParametersSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["url"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "URL изображения"
                }
            },
            ["required"] = new[] { "url" }
        };
    }

    public Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Получение изображений пока не реализовано");
    }
}
