using System.Text.Json;
using System.Text.Json.Serialization;

namespace RusalProject.Services.Ollama;

public interface IOllamaChatService
{
    Task<OllamaChatResponse> CompleteAsync(
        Guid userId,
        IReadOnlyList<OllamaMessageInput> messages,
        string systemPrompt,
        IReadOnlyList<OllamaToolDefinition> tools,
        Func<string, Task>? onChunk = null,
        CancellationToken cancellationToken = default);
}

public sealed class OllamaMessageInput
{
    public string Role { get; init; } = string.Empty;
    public string? Content { get; init; }
    public string? Thinking { get; init; }
    public string? ToolName { get; init; }
    public List<OllamaToolCall>? ToolCalls { get; init; }
}

public sealed class OllamaToolDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public object Parameters { get; init; } = new();
}

public sealed class OllamaToolCall
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("function")]
    public OllamaToolFunctionCall? Function { get; init; }

    [JsonIgnore]
    public string Name => Function?.Name ?? string.Empty;

    [JsonIgnore]
    public JsonElement Arguments => Function?.Arguments ?? default;
}

public sealed class OllamaToolFunctionCall
{
    [JsonPropertyName("index")]
    public int? Index { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public JsonElement Arguments { get; init; }
}

public sealed class OllamaChatResponse
{
    public string Content { get; init; } = string.Empty;
    public List<OllamaToolCall> ToolCalls { get; init; } = new();
    public string? Thinking { get; init; }

    public OllamaMessageInput ToAssistantMessage()
    {
        return new OllamaMessageInput
        {
            Role = "assistant",
            Thinking = Thinking,
            Content = Content,
            ToolCalls = ToolCalls.Count > 0 ? ToolCalls : null
        };
    }
}
