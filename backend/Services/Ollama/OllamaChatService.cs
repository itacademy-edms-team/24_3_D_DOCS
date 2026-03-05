using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RusalProject.Services.Ollama;

public class OllamaChatService : IOllamaChatService
{
    private readonly IUserOllamaApiKeyService _keyService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaChatService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaChatService(
        IUserOllamaApiKeyService keyService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OllamaChatService> logger)
    {
        _keyService = keyService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OllamaChatResponse> CompleteAsync(
        Guid userId,
        IReadOnlyList<OllamaMessageInput> messages,
        string systemPrompt,
        IReadOnlyList<OllamaToolDefinition> tools,
        Func<string, Task>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _keyService.HasApiKeyAsync(userId, cancellationToken))
            throw new InvalidOperationException("Настройте Ollama API ключ.");

        var apiKey = await _keyService.GetDecryptedApiKeyAsync(userId, cancellationToken);
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Не удалось получить API ключ.");

        var model = _configuration["Ollama:DefaultModel"] ?? "gpt-oss:120b";
        var baseUrl = _configuration["Ollama:BaseUrl"] ?? "https://ollama.com";
        var url = $"{baseUrl.TrimEnd('/')}/api/chat";

        var requestBody = new
        {
            model,
            stream = onChunk != null,
            messages = BuildMessages(systemPrompt, messages),
            tools = tools.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.Parameters
                }
            }).ToList()
        };
        var serializedRequestBody = JsonSerializer.Serialize(requestBody, JsonOptions);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(serializedRequestBody, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("API ключ недействителен.");
        if (response.StatusCode == (HttpStatusCode)429)
            throw new InvalidOperationException("Провайдер временно ограничил запросы.");
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Ollama returned error {StatusCode}. Response body: {ResponseBody}. Request body: {RequestBody}",
                (int)response.StatusCode,
                Truncate(errorBody, 4000),
                Truncate(serializedRequestBody, 8000));
            throw new InvalidOperationException($"Ошибка при обращении к Ollama: {(int)response.StatusCode}.");
        }

        if (onChunk == null)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonSerializer.Deserialize<OllamaStreamEvent>(payload, JsonOptions)
                ?? throw new InvalidOperationException("Пустой ответ от Ollama.");
            return ToResponse(parsed);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var fullContent = new StringBuilder();
        var toolCalls = new List<OllamaToolCall>();
        string? thinking = null;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var evt = JsonSerializer.Deserialize<OllamaStreamEvent>(line, JsonOptions);
                if (evt?.Message == null)
                    continue;

                if (!string.IsNullOrEmpty(evt.Message.Content))
                {
                    fullContent.Append(evt.Message.Content);
                    await onChunk(evt.Message.Content);
                }

                if (!string.IsNullOrEmpty(evt.Message.Thinking))
                {
                    thinking = evt.Message.Thinking;
                }

                if (evt.Message.ToolCalls is { Count: > 0 })
                {
                    MergeToolCalls(toolCalls, evt.Message.ToolCalls);
                }

                if (evt.Done == true)
                    break;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Ollama stream line: {Line}", line);
            }
        }

        return new OllamaChatResponse
        {
            Content = fullContent.ToString(),
            ToolCalls = toolCalls,
            Thinking = thinking
        };
    }

    private static List<object> BuildMessages(string systemPrompt, IReadOnlyList<OllamaMessageInput> messages)
    {
        var payload = new List<object>
        {
            new
            {
                role = "system",
                content = systemPrompt
            }
        };

        foreach (var message in messages)
        {
            payload.Add(new
            {
                role = message.Role,
                thinking = message.Thinking,
                content = message.Content,
                tool_name = message.ToolName,
                tool_calls = message.ToolCalls?.Select(tc => new
                {
                    type = tc.Type,
                    id = string.IsNullOrWhiteSpace(tc.Id) ? null : tc.Id,
                    function = new
                    {
                        index = tc.Function?.Index,
                        name = tc.Name,
                        arguments = tc.Arguments
                    }
                }).ToList()
            });
        }

        return payload;
    }

    private static OllamaChatResponse ToResponse(OllamaStreamEvent evt)
    {
        return new OllamaChatResponse
        {
            Content = evt.Message?.Content ?? string.Empty,
            ToolCalls = evt.Message?.ToolCalls ?? new List<OllamaToolCall>(),
            Thinking = evt.Message?.Thinking
        };
    }

    private static void MergeToolCalls(List<OllamaToolCall> target, List<OllamaToolCall> incoming)
    {
        foreach (var toolCall in incoming)
        {
            var existingIndex = target.FindIndex(t => IsSameToolCall(t, toolCall));
            if (existingIndex >= 0)
                target[existingIndex] = toolCall;
            else
                target.Add(toolCall);
        }
    }

    private static bool IsSameToolCall(OllamaToolCall left, OllamaToolCall right)
    {
        if (!string.IsNullOrWhiteSpace(left.Id) && !string.IsNullOrWhiteSpace(right.Id))
            return string.Equals(left.Id, right.Id, StringComparison.Ordinal);

        if (left.Function?.Index is not null && right.Function?.Index is not null)
            return left.Function.Index == right.Function.Index;

        return string.Equals(left.Name, right.Name, StringComparison.Ordinal);
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? string.Empty;

        return value[..maxLength] + "...[truncated]";
    }

    private sealed class OllamaStreamEvent
    {
        [JsonPropertyName("message")]
        public OllamaStreamMessage? Message { get; init; }

        [JsonPropertyName("done")]
        public bool? Done { get; init; }
    }

    private sealed class OllamaStreamMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }

        [JsonPropertyName("thinking")]
        public string? Thinking { get; init; }

        [JsonPropertyName("tool_calls")]
        public List<OllamaToolCall>? ToolCalls { get; init; }
    }
}
