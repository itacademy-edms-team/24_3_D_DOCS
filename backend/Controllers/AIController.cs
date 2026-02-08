using System.Linq;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Services.Agent;
using RusalProject.Services.Chat;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IChatService _chatService;
    private readonly IAgentLogService _logService;
    private readonly ILogger<AIController> _logger;

    public AIController(
        IAgentService agentService,
        IChatService chatService,
        IAgentLogService logService,
        ILogger<AIController> logger)
    {
        _agentService = agentService;
        _chatService = chatService;
        _logService = logService;
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
        // #region agent log
        try {
            var modelStateErrors = ModelState
                .SelectMany(kvp => kvp.Value?.Errors ?? Enumerable.Empty<Microsoft.AspNetCore.Mvc.ModelBinding.ModelError>())
                .Select(e => e.ErrorMessage)
                .ToList();
            var logEntry = JsonSerializer.Serialize(new {
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "A",
                location = "AIController.cs:52",
                message = "Agent endpoint entry",
                data = new {
                    documentId = request?.DocumentId.ToString(),
                    userMessage = request?.UserMessage?.Substring(0, Math.Min(50, request?.UserMessage?.Length ?? 0)),
                    startLine = request?.StartLine,
                    endLine = request?.EndLine,
                    modelStateIsValid = ModelState.IsValid,
                    modelStateErrors = modelStateErrors
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            System.IO.File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
        } catch {}
        // #endregion

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
            if (!ModelState.IsValid)
            {
                // #region agent log
                try {
                    var errors = ModelState
                        .Where(kvp => kvp.Value?.Errors != null && kvp.Value.Errors.Count > 0)
                        .SelectMany(kvp => kvp.Value!.Errors.Select(e => new { field = kvp.Key, message = e.ErrorMessage }))
                        .ToList();
                    var logEntry = JsonSerializer.Serialize(new {
                        sessionId = "debug-session",
                        runId = "run1",
                        hypothesisId = "C",
                        location = "AIController.cs:87",
                        message = "ModelState validation failed",
                        data = new { errors },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    System.IO.File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
                } catch {}
                // #endregion
                
                await SendSSEEvent("error", new { message = "Ошибка валидации", errors = ModelState });
                return;
            }

            var userId = GetUserId();

            // Сохраняем сообщение пользователя в чат, если указан ChatId
            if (request.ChatId.HasValue)
            {
                try
                {
                    var userMessage = new ChatMessageDTO
                    {
                        Role = "user",
                        Content = request.UserMessage
                    };
                    await _chatService.AddMessageAsync(request.ChatId.Value, userMessage, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save user message to chat {ChatId}", request.ChatId);
                }
            }

            // #region agent log
            try {
                var logEntry = JsonSerializer.Serialize(new {
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "D",
                    location = "AIController.cs:106",
                    message = "Before ProcessAsync call",
                    data = new {
                        userId = userId.ToString(),
                        documentId = request.DocumentId.ToString(),
                        chatId = request.ChatId?.ToString()
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                System.IO.File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
            } catch {}
            // #endregion

            var collectedSteps = new List<AgentStepDTO>();

            // Callback для отправки шагов через SSE и сохранения в чат
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
                            ? JsonSerializer.Serialize(step.ToolCalls)
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
            }, cancellationToken);

            // #region agent log
            try {
                var logEntry = JsonSerializer.Serialize(new {
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "D",
                    location = "AIController.cs:128",
                    message = "ProcessAsync completed successfully",
                    data = new {
                        isComplete = finalResponse.IsComplete,
                        stepsCount = finalResponse.Steps?.Count ?? 0,
                        finalMessageLength = finalResponse.FinalMessage?.Length ?? 0
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                System.IO.File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
            } catch {}
            // #endregion

            // Сохраняем финальный ответ в чат, если указан ChatId
            if (request.ChatId.HasValue && !string.IsNullOrEmpty(finalResponse.FinalMessage))
            {
                try
                {
                    var finalMessage = new ChatMessageDTO
                    {
                        Role = "assistant",
                        Content = finalResponse.FinalMessage
                    };
                    await _chatService.AddMessageAsync(request.ChatId.Value, finalMessage, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save final message to chat {ChatId}", request.ChatId);
                }
            }

            // Отправляем финальное событие
            await SendSSEEvent("complete", finalResponse);
        }
        catch (Exception ex)
        {
            // #region agent log
            try {
                var stackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length));
                var logEntry = JsonSerializer.Serialize(new {
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "E",
                    location = "AIController.cs:149",
                    message = "Agent endpoint error",
                    data = new {
                        exceptionType = ex.GetType().Name,
                        exceptionMessage = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = stackTrace
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                System.IO.File.AppendAllText("/app/.cursor/debug.log", logEntry + "\n");
            } catch {}
            // #endregion

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
}
