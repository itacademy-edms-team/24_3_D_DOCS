using System.Linq;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.DTOs.AI;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Models.Types;
using RusalProject.Services.Agent;
using RusalProject.Services.AgentSources;
using RusalProject.Services.Chat;
using RusalProject.Services.Ollama;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IChatService _chatService;
    private readonly IAgentLogService _logService;
    private readonly IUserOllamaApiKeyService _ollamaKeyService;
    private readonly IUserOllamaModelSettingsService _ollamaModelSettingsService;
    private readonly IAgentSourceService _agentSourceService;
    private readonly ILogger<AIController> _logger;
    private static readonly JsonSerializerOptions ToolCallsJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AIController(
        IAgentService agentService,
        IChatService chatService,
        IAgentLogService logService,
        IUserOllamaApiKeyService ollamaKeyService,
        IUserOllamaModelSettingsService ollamaModelSettingsService,
        IAgentSourceService agentSourceService,
        ILogger<AIController> logger)
    {
        _agentService = agentService;
        _chatService = chatService;
        _logService = logService;
        _ollamaKeyService = ollamaKeyService;
        _ollamaModelSettingsService = ollamaModelSettingsService;
        _agentSourceService = agentSourceService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        return Guid.Parse(userIdClaim);
    }

    /// <summary>
    /// Основной endpoint для работы с агентом (потоковая передача через SSE)
    /// </summary>
    [HttpPost("agent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task Agent([FromBody] AgentRequestDTO request, CancellationToken cancellationToken)
    {
        // Настройка SSE (делаем это в начале, чтобы можно было отправлять события в любом случае)
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Отключаем буферизацию в nginx
        
        // Отключаем буферизацию ответа для streaming
        Response.Headers.Append("X-Content-Type-Options", "nosniff");
        
        // Отправляем начальный комментарий для инициализации SSE соединения
        await SendSSEComment("SSE connection established");

        try
        {
            if (request == null)
            {
                await SendSSEEvent("error", new { message = "Тело запроса обязательно." });
                return;
            }

            if (!ModelState.IsValid)
            {
                await SendSSEEvent("error", new { message = "Ошибка валидации", errors = ModelState });
                return;
            }

            if (!request.ChatId.HasValue)
            {
                await SendSSEEvent("error", new { message = "ChatId обязателен. Создайте чат через POST /api/chats." });
                return;
            }

            var scope = request.Scope ?? ChatScope.Document;
            if (scope == ChatScope.Document && (!request.DocumentId.HasValue || request.DocumentId == Guid.Empty))
            {
                await SendSSEEvent("error", new { message = "При scope=document параметр documentId обязателен." });
                return;
            }

            var userId = GetUserId();

            // Сохраняем сообщение пользователя в чат
            try
            {
                string? attachmentsJson = null;
                if (request.SourceSessionId.HasValue)
                {
                    var docForAttachment = scope == ChatScope.Document ? request.DocumentId : null;
                    attachmentsJson = await _agentSourceService.BuildAttachmentsJsonAsync(
                        userId,
                        request.ChatId.Value,
                        docForAttachment,
                        request.SourceSessionId.Value,
                        cancellationToken);
                }

                var userMessage = new ChatMessageDTO
                {
                    Role = "user",
                    Content = request.UserMessage,
                    AttachmentsJson = attachmentsJson
                };
                await _chatService.AddMessageAsync(request.ChatId.Value, userMessage, userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save user message to chat {ChatId}", request.ChatId);
            }

            var collectedSteps = new List<AgentStepDTO>();

            Func<string, Task> onChunk = async (chunk) =>
            {
                try
                {
                    await SendSSEEvent("chunk", new { content = chunk });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending chunk via SSE");
                }
            };

            var finalResponse = await _agentService.ProcessAsync(request, userId, async (step) =>
            {
                try
                {
                    _logger.LogInformation("Sending SSE step event: StepNumber={StepNumber}, Description={Description}", 
                        step.StepNumber, step.Description);
                    await SendSSEEvent("step", step);
                    
                    // Сохраняем шаг в чат, если указан ChatId
                    if (request.ChatId.HasValue)
                    {
                        collectedSteps.Add(step);
                        
                        var toolCallsJson = step.ToolCalls != null && step.ToolCalls.Count > 0
                            ? JsonSerializer.Serialize(step.ToolCalls, ToolCallsJsonOptions)
                            : null;

                        var assistantMessage = new ChatMessageDTO
                        {
                            Role = "assistant",
                            Content = step.Description,
                            StepNumber = step.StepNumber,
                            ToolCalls = toolCallsJson
                        };
                        
                        try
                        {
                            await _chatService.AddMessageAsync(request.ChatId.Value, assistantMessage, userId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to save step message to chat {ChatId}", request.ChatId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending step event via SSE");
                }
            }, null, onChunk, cancellationToken);

            // Отправляем финальное событие
            await SendSSEEvent("complete", finalResponse);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Agent request validation/configuration error");
            await SendSSEEvent("error", new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent request");
            await SendSSEEvent("error", new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    private async Task SendSSEComment(string comment)
    {
        try
        {
            var commentData = $": {comment}\n\n";
            var bytes = Encoding.UTF8.GetBytes(commentData);
            await Response.Body.WriteAsync(bytes, 0, bytes.Length);
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SSE comment: {Error}", ex.Message);
        }
    }

    private async Task SendSSEEvent(string eventType, object data)
    {
        try
        {
            // Используем camelCase для совместимости с фронтендом
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(data, jsonOptions);
            var eventData = $"event: {eventType}\ndata: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(eventData);
            
            _logger.LogInformation("SSE event sending: {EventType}, data length: {DataLength}", 
                eventType, json.Length);
            
            await Response.Body.WriteAsync(bytes, 0, bytes.Length);
            await Response.Body.FlushAsync();
            
            _logger.LogDebug("SSE event sent successfully: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendSSEEvent: EventType={EventType}, Error={Error}", eventType, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Получить логи работы агента для документа
    /// </summary>
    [HttpGet("logs/{documentId}")]
    [ProducesResponseType(typeof(List<AgentLogDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAgentLogs(Guid documentId, [FromQuery] Guid? chatSessionId = null)
    {
        try
        {
            var userId = GetUserId();
            var logs = await _logService.GetLogsAsync(documentId, userId, chatSessionId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent logs");
            return StatusCode(500, new { message = "Ошибка при получении логов агента", details = ex.Message });
        }
    }

    /// <summary>
    /// Справочник моделей LLM для настроек
    /// </summary>
    [HttpGet("ollama-models")]
    [ProducesResponseType(typeof(IReadOnlyList<LlmModelOptionDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOllamaModels(CancellationToken cancellationToken)
    {
        var list = await _ollamaModelSettingsService.GetCatalogAsync(cancellationToken);
        return Ok(list);
    }

    /// <summary>
    /// Текущие сохранённые и эффективные модели пользователя
    /// </summary>
    [HttpGet("ollama-model-preferences")]
    [ProducesResponseType(typeof(OllamaModelPreferencesResponseDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOllamaModelPreferences(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var dto = await _ollamaModelSettingsService.GetPreferencesAsync(userId, cancellationToken);
        return Ok(dto);
    }

    /// <summary>
    /// Сохранить выбор моделей (пустая строка/null сбрасывает переопределение для поля)
    /// </summary>
    [HttpPut("ollama-model-preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutOllamaModelPreferences(
        [FromBody] SetOllamaModelPreferencesDTO? dto,
        CancellationToken cancellationToken)
    {
        if (dto == null)
            return BadRequest(new { message = "Тело запроса обязательно." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetUserId();
            await _ollamaModelSettingsService.SavePreferencesAsync(userId, dto, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Проверить Ollama API ключ без сохранения
    /// </summary>
    [HttpPost("ollama-key/verify")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOllamaKey([FromBody] SetOllamaApiKeyDTO dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _ollamaKeyService.VerifyApiKeyAsync(dto.ApiKey.Trim(), cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Сохранить Ollama API ключ (с валидацией)
    /// </summary>
    [HttpPost("ollama-key")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetOllamaKey([FromBody] SetOllamaApiKeyDTO dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetUserId();
            await _ollamaKeyService.SetApiKeyAsync(userId, dto.ApiKey.Trim(), cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Получить расшифрованный Ollama API ключ (для копирования)
    /// </summary>
    [HttpGet("ollama-key")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOllamaKey(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var apiKey = await _ollamaKeyService.GetDecryptedApiKeyAsync(userId, cancellationToken);
        if (string.IsNullOrEmpty(apiKey))
        {
            return NotFound(new { message = "API ключ не найден" });
        }
        return Ok(new { apiKey });
    }

    /// <summary>
    /// Проверить наличие Ollama API ключа
    /// </summary>
    [HttpGet("ollama-key/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOllamaKeyStatus(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var hasKey = await _ollamaKeyService.HasApiKeyAsync(userId, cancellationToken);
        return Ok(new { hasKey });
    }

    /// <summary>
    /// Удалить Ollama API ключ
    /// </summary>
    [HttpDelete("ollama-key")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOllamaKey(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _ollamaKeyService.RemoveApiKeyAsync(userId, cancellationToken);
        return NoContent();
    }
}
