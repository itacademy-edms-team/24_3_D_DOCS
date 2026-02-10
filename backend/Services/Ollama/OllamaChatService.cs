using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Provider.Database;
using RusalProject.Services.Agent;
using RusalProject.Services.Chat;

namespace RusalProject.Services.Ollama;

public class OllamaChatService : IOllamaChatService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserOllamaApiKeyService _keyService;
    private readonly IChatService _chatService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaChatService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaChatService(
        ApplicationDbContext context,
        IUserOllamaApiKeyService keyService,
        IChatService chatService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OllamaChatService> logger)
    {
        _context = context;
        _keyService = keyService;
        _chatService = chatService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OllamaChatResult> ChatAsync(
        Guid userId,
        Guid chatId,
        string userMessage,
        string? clientMessageId,
        Guid documentId,
        Func<string, Task>? onChunk = null,
        Func<string, Task>? onStatusCheck = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _keyService.HasApiKeyAsync(userId, cancellationToken))
        {
            throw new InvalidOperationException("Настройте Ollama API ключ перед использованием ассистента.");
        }

        var apiKey = await _keyService.GetDecryptedApiKeyAsync(userId, cancellationToken);
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Не удалось получить API ключ. Попробуйте настроить его заново.");
        }

        var chat = await _chatService.GetChatByIdAsync(chatId, userId);
        if (chat == null)
        {
            throw new InvalidOperationException("Чат не найден.");
        }

        var ollamaMessages = BuildMessages(chat.Messages);
        var model = _configuration["Ollama:DefaultModel"] ?? "gpt-oss:120b";
        var baseUrl = _configuration["Ollama:BaseUrl"] ?? "https://ollama.com";
        var url = $"{baseUrl.TrimEnd('/')}/api/chat";

        var requestBody = new
        {
            model,
            messages = ollamaMessages,
            stream = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Ollama API returned 401 for user {UserId}", userId);
            throw new InvalidOperationException("API ключ недействителен. Проверьте ключ на ollama.com/settings/keys.");
        }

        if (response.StatusCode == (HttpStatusCode)429)
        {
            _logger.LogWarning("Ollama API returned 429 for user {UserId}", userId);
            throw new InvalidOperationException("Провайдер временно ограничил запросы. Попробуйте позже.");
        }

        if ((int)response.StatusCode >= 500)
        {
            _logger.LogWarning("Ollama API returned {StatusCode} for user {UserId}", response.StatusCode, userId);
            throw new InvalidOperationException("Временная ошибка сервиса. Попробуйте позже.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama API returned {StatusCode} for user {UserId}", response.StatusCode, userId);
            throw new InvalidOperationException("Ошибка при обращении к Ollama. Попробуйте позже.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var fullContent = new StringBuilder();
        string? lastContent = null;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var evt = JsonSerializer.Deserialize<OllamaStreamEvent>(line);
                if (evt?.Message?.Content != null)
                {
                    fullContent.Append(evt.Message.Content);
                    lastContent = evt.Message.Content;
                    if (onChunk != null)
                    {
                        await onChunk(evt.Message.Content);
                    }
                }

                if (evt?.Done == true)
                {
                    break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Ollama stream line: {Line}", line);
            }
        }

        var finalMessage = fullContent.ToString();
        if (string.IsNullOrEmpty(finalMessage))
        {
            finalMessage = lastContent ?? string.Empty;
        }

        await _chatService.AddMessageAsync(chatId, new ChatMessageDTO
        {
            Role = "assistant",
            Content = finalMessage
        }, userId);

        return new OllamaChatResult
        {
            FinalMessage = finalMessage,
            IsComplete = true,
            IsIdempotentRetry = false
        };
    }

    private static List<OllamaMessage> BuildMessages(List<ChatMessageDTO> history)
    {
        var result = new List<OllamaMessage>();

        result.Add(new OllamaMessage
        {
            Role = "system",
            Content = AgentSystemPrompt.GetSystemPrompt("ru")
        });

        foreach (var m in history.OrderBy(x => x.CreatedAt))
        {
            if (m.Role is "user" or "assistant" or "system")
            {
                result.Add(new OllamaMessage { Role = m.Role, Content = m.Content });
            }
        }
        return result;
    }

    private class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OllamaStreamEvent
    {
        [JsonPropertyName("message")]
        public OllamaStreamMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool? Done { get; set; }
    }

    private class OllamaStreamMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
