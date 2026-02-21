namespace RusalProject.Services.Ollama;

public interface IOllamaChatService
{
    Task<OllamaChatResult> ChatAsync(
        Guid userId,
        Guid chatId,
        string userMessage,
        Guid documentId,
        Func<string, Task>? onChunk = null,
        Func<string, Task>? onStatusCheck = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chat with custom system prompt (e.g. for Main Agent). No document context.
    /// </summary>
    Task<OllamaChatResult> ChatWithPromptAsync(
        Guid userId,
        Guid chatId,
        string userMessage,
        string systemPrompt,
        Func<string, Task>? onChunk = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send messages to Ollama without DB. Used by Main Agent for tool-calling loop.
    /// </summary>
    Task<OllamaChatResult> SendMessagesAsync(
        Guid userId,
        List<OllamaMessageInput> messages,
        string systemPrompt,
        Func<string, Task>? onChunk = null,
        CancellationToken cancellationToken = default);
}

public record OllamaMessageInput(string Role, string Content);

public class OllamaChatResult
{
    public string FinalMessage { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool IsIdempotentRetry { get; set; }
}
