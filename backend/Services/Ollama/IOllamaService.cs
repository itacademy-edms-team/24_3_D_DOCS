namespace RusalProject.Services.Ollama;

public interface IOllamaService
{
    /// <summary>
    /// Генерирует эмбеддинг для текста
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Вызывает LLM для генерации ответа
    /// </summary>
    Task<string> GenerateChatAsync(string systemPrompt, string userMessage, List<ChatMessage>? messages = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Вызывает LLM с поддержкой tool calls
    /// </summary>
    Task<ChatResponse> GenerateChatWithToolsAsync(
        string systemPrompt,
        string userMessage,
        List<ToolDefinition> tools,
        List<ChatMessage>? messages = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Вызывает vision модель для анализа изображения (base64)
    /// </summary>
    Task<string> GenerateVisionChatAsync(
        string prompt,
        string imageBase64,
        CancellationToken cancellationToken = default);
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public ToolCall? ToolCall { get; set; }
    public string? ToolCallId { get; set; }
}

public class ToolCall
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
}

public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ChatResponse
{
    public string Content { get; set; } = string.Empty;
    public List<ToolCall>? ToolCalls { get; set; }
    public bool IsDone { get; set; }
}
