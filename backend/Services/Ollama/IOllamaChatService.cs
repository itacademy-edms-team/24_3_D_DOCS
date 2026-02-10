namespace RusalProject.Services.Ollama;

public interface IOllamaChatService
{
    Task<OllamaChatResult> ChatAsync(
        Guid userId,
        Guid chatId,
        string userMessage,
        string? clientMessageId,
        Guid documentId,
        Func<string, Task>? onChunk = null,
        Func<string, Task>? onStatusCheck = null,
        CancellationToken cancellationToken = default);
}

public class OllamaChatResult
{
    public string FinalMessage { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool IsIdempotentRetry { get; set; }
}
