namespace RusalProject.Services.Ollama;

/// <summary>
/// Одиночный вызов чата без tool-calling (для вложенных запросов по вложениям).
/// </summary>
public interface IOllamaSimpleChatService
{
    Task<string> CompleteTextAsync(
        Guid userId,
        string model,
        string systemPrompt,
        string userContent,
        CancellationToken cancellationToken = default);

    Task<string> CompleteVisionAsync(
        Guid userId,
        string model,
        string systemPrompt,
        string userTextPrompt,
        byte[] imageBytes,
        CancellationToken cancellationToken = default);
}
