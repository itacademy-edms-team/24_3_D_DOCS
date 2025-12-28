using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace RusalProject.Services.Ollama;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly string _embeddingModel;
    private readonly string _llmModel;
    private readonly string _visionModel;
    private readonly int _timeout;
    private readonly float _temperature;
    private readonly float _topP;
    private readonly int _topK;

    public OllamaService(
        IConfiguration configuration,
        ILogger<OllamaService> logger)
    {
        _logger = logger;
        var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _embeddingModel = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        _llmModel = configuration["Ollama:LlmModel"] ?? "qwen3-vl:235b-instruct-cloud";
        _visionModel = configuration["Ollama:Vision"] ?? "qwen3-vl:235b-instruct-cloud";
        _timeout = int.Parse(configuration["Ollama:Timeout"] ?? "300");
        _temperature = float.Parse(configuration["Ollama:Temperature"] ?? "0.2", System.Globalization.CultureInfo.InvariantCulture);
        _topP = float.Parse(configuration["Ollama:TopP"] ?? "0.9", System.Globalization.CultureInfo.InvariantCulture);
        _topK = int.Parse(configuration["Ollama:TopK"] ?? "40");

        // #region agent log
        try {
            var logEntry = JsonSerializer.Serialize(new {
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "A",
                location = "OllamaService.cs:20",
                message = "OllamaService constructor - BaseUrl configuration",
                data = new {
                    baseUrlFromConfig = configuration["Ollama:BaseUrl"],
                    baseUrlUsed = baseUrl,
                    embeddingModel = _embeddingModel,
                    llmModel = _llmModel,
                    timeout = _timeout,
                    envOllamaBaseUrl = Environment.GetEnvironmentVariable("Ollama__BaseUrl")
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
        } catch {}
        // #endregion

        // Ensure baseUrl doesn't end with / to avoid double slashes
        if (baseUrl.EndsWith("/"))
        {
            baseUrl = baseUrl.TrimEnd('/');
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(_timeout)
        };

        // #region agent log
        try {
            var logEntry = JsonSerializer.Serialize(new {
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "B",
                location = "OllamaService.cs:45",
                message = "HttpClient created with BaseAddress",
                data = new {
                    baseAddress = _httpClient.BaseAddress?.ToString(),
                    timeout = _httpClient.Timeout.TotalSeconds
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
        } catch {}
        // #endregion
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        // #region agent log
        try {
            var logEntry = JsonSerializer.Serialize(new {
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "C",
                location = "OllamaService.cs:75",
                message = "GenerateEmbeddingAsync entry",
                data = new {
                    baseAddress = _httpClient.BaseAddress?.ToString(),
                    requestUrl = _httpClient.BaseAddress + "/api/embeddings",
                    model = _embeddingModel,
                    textLength = text?.Length ?? 0
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
        } catch {}
        // #endregion

        try
        {
            var request = new
            {
                model = _embeddingModel,
                prompt = text
            };

            // #region agent log
            try {
                var logEntry = JsonSerializer.Serialize(new {
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "D",
                    location = "OllamaService.cs:95",
                    message = "Before HTTP request to Ollama",
                    data = new {
                        fullUrl = _httpClient.BaseAddress + "/api/embeddings"
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
            } catch {}
            // #endregion

            var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);
            if (result?.Embedding == null)
            {
                throw new InvalidOperationException("Embedding response is null or empty");
            }

            // #region agent log
            try {
                var logEntry = JsonSerializer.Serialize(new {
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "C",
                    location = "OllamaService.cs:110",
                    message = "GenerateEmbeddingAsync success",
                    data = new {
                        embeddingLength = result.Embedding.Length
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
            } catch {}
            // #endregion

            return result.Embedding;
        }
        catch (Exception ex)
        {
            // #region agent log
            try {
                var logEntry = JsonSerializer.Serialize(new {
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "D",
                    location = "OllamaService.cs:125",
                    message = "GenerateEmbeddingAsync error",
                    data = new {
                        exceptionType = ex.GetType().Name,
                        exceptionMessage = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace != null ? ex.StackTrace.Substring(0, Math.Min(500, ex.StackTrace.Length)) : null
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
            } catch {}
            // #endregion

            _logger.LogError(ex, "Error generating embedding");
            throw;
        }
    }

    public async Task<string> GenerateChatAsync(string systemPrompt, string userMessage, List<ChatMessage>? messages = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatMessages = new List<object>();

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                chatMessages.Add(new { role = "system", content = systemPrompt });
            }

            if (messages != null)
            {
                foreach (var msg in messages)
                {
                    var messageObj = new Dictionary<string, object>
                    {
                        ["role"] = msg.Role,
                        ["content"] = msg.Content
                    };

                    if (msg.ToolCall != null)
                    {
                        messageObj["tool_calls"] = new[]
                        {
                            new
                            {
                                name = msg.ToolCall.Name,
                                arguments = msg.ToolCall.Arguments
                            }
                        };
                    }

                    if (!string.IsNullOrEmpty(msg.ToolCallId))
                    {
                        messageObj["tool_call_id"] = msg.ToolCallId;
                    }

                    chatMessages.Add(messageObj);
                }
            }

            chatMessages.Add(new { role = "user", content = userMessage });

            var request = new
            {
                model = _llmModel,
                messages = chatMessages,
                stream = false,
                options = new
                {
                    temperature = _temperature,
                    top_p = _topP,
                    top_k = _topK
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatApiResponse>(cancellationToken: cancellationToken);
            if (result?.Message?.Content == null)
            {
                throw new InvalidOperationException("Chat response is null or empty");
            }

            return result.Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chat response");
            throw;
        }
    }

    public async Task<ChatResponse> GenerateChatWithToolsAsync(
        string systemPrompt,
        string userMessage,
        List<ToolDefinition> tools,
        List<ChatMessage>? messages = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatMessages = new List<object>();

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                chatMessages.Add(new { role = "system", content = systemPrompt });
            }

            if (messages != null)
            {
                foreach (var msg in messages)
                {
                    var messageObj = new Dictionary<string, object>
                    {
                        ["role"] = msg.Role,
                        ["content"] = msg.Content
                    };

                    if (msg.ToolCall != null)
                    {
                        messageObj["tool_calls"] = new[]
                        {
                            new
                            {
                                name = msg.ToolCall.Name,
                                arguments = msg.ToolCall.Arguments
                            }
                        };
                    }

                    if (!string.IsNullOrEmpty(msg.ToolCallId))
                    {
                        messageObj["tool_call_id"] = msg.ToolCallId;
                    }

                    chatMessages.Add(messageObj);
                }
            }

            chatMessages.Add(new { role = "user", content = userMessage });

            // Convert tools to Ollama format
            var toolsList = tools.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.Parameters
                }
            }).ToList();

            var request = new
            {
                model = _llmModel,
                messages = chatMessages,
                tools = toolsList,
                stream = false,
                options = new
                {
                    temperature = _temperature,
                    top_p = _topP,
                    top_k = _topK
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            _logger.LogDebug("Ollama chat API request: Model={Model}, MessagesCount={MessagesCount}, ToolsCount={ToolsCount}", 
                _llmModel, chatMessages.Count, toolsList.Count);

            var response = await _httpClient.PostAsJsonAsync("/api/chat", request, jsonOptions, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama chat API error: Status={StatusCode}, Response={ErrorContent}, Model={Model}, ToolsCount={ToolsCount}", 
                    response.StatusCode, errorContent, _llmModel, toolsList.Count);
                
                // Provide more helpful error message for 500 errors with tools
                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError && toolsList.Count > 0)
                {
                    var errorMessage = $"Ollama API вернул ошибку 500 при использовании tools. Модель '{_llmModel}' может не поддерживать function calling/tools. " +
                        $"Vision модели (например, qwen3-vl) обычно не поддерживают tools. Попробуйте использовать модель с поддержкой function calling (например, qwen2.5:7b, qwen2.5:14b, llama3.2). " +
                        $"Ошибка от сервера: {errorContent}";
                    throw new HttpRequestException(errorMessage);
                }
                
                throw new HttpRequestException($"Ollama chat API returned error {response.StatusCode}: {errorContent}");
            }
            
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatApiResponse>(cancellationToken: cancellationToken);
            if (result?.Message == null)
            {
                throw new InvalidOperationException("Chat response is null or empty");
            }

            var chatResponse = new ChatResponse
            {
                Content = result.Message.Content ?? string.Empty,
                IsDone = result.Done
            };

            // Parse tool calls if present
            if (result.Message.ToolCalls != null && result.Message.ToolCalls.Count > 0)
            {
                chatResponse.ToolCalls = result.Message.ToolCalls.Select(tc => new ToolCall
                {
                    Name = tc.Function?.Name ?? string.Empty,
                    Arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        tc.Function?.Arguments?.ToString() ?? "{}") ?? new Dictionary<string, object>()
                }).ToList();
            }

            return chatResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chat response with tools");
            throw;
        }
    }

    public async Task<string> GenerateVisionChatAsync(
        string prompt,
        string imageBase64,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ollama vision API format: content is a string, images are passed separately
            // Format: messages with content as string, and images array with base64 strings
            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = prompt,
                    images = new[] { imageBase64 }
                }
            };

            var request = new
            {
                model = _visionModel,
                messages = messages,
                stream = false,
                options = new
                {
                    temperature = _temperature,
                    top_p = _topP,
                    top_k = _topK
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama vision API error: Status={StatusCode}, Response={ErrorContent}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Ollama vision API returned error {response.StatusCode}: {errorContent}");
            }
            
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatApiResponse>(cancellationToken: cancellationToken);
            if (result?.Message?.Content == null)
            {
                throw new InvalidOperationException("Vision chat response is null or empty");
            }

            return result.Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating vision chat response");
            throw;
        }
    }

    private class EmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }

    private class ChatApiResponse
    {
        [JsonPropertyName("message")]
        public ChatApiMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    private class ChatApiMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<ChatApiToolCall>? ToolCalls { get; set; }
    }

    private class ChatApiToolCall
    {
        [JsonPropertyName("function")]
        public ChatApiFunction? Function { get; set; }
    }

    private class ChatApiFunction
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments")]
        public object? Arguments { get; set; }
    }
}
