using System.Linq;
using System.Text.Json;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Services.Chat;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

/// <summary>
/// Document assistant agent. Uses a tool-calling loop with document-specific tools.
/// </summary>
public class DocumentAgent : IDocumentAgent
{
    private const int MaxToolIterations = 16;

    private readonly IOllamaChatService _ollamaChatService;
    private readonly IChatService _chatService;
    private readonly DocumentAgentToolExecutor _toolExecutor;
    private readonly ILogger<DocumentAgent> _logger;

    public DocumentAgent(
        IOllamaChatService ollamaChatService,
        IChatService chatService,
        DocumentAgentToolExecutor toolExecutor,
        ILogger<DocumentAgent> logger)
    {
        _ollamaChatService = ollamaChatService;
        _chatService = chatService;
        _toolExecutor = toolExecutor;
        _logger = logger;
    }

    public async Task<AgentResponseDTO> RunAsync(
        AgentRequestDTO request,
        Guid userId,
        Func<AgentStepDTO, Task>? onStepUpdate = null,
        Func<int, Task>? onDocumentChange = null,
        Func<string, Task>? onStatusCheck = null,
        CancellationToken cancellationToken = default)
    {
        if (!request.ChatId.HasValue)
            throw new InvalidOperationException("ChatId обязателен.");

        var documentId = request.DocumentId ?? throw new InvalidOperationException("DocumentId обязателен для Document Agent.");
        _logger.LogInformation("DocumentAgent: Running. DocumentId={DocumentId}, ChatId={ChatId}, UserId={UserId}", documentId, request.ChatId, userId);

        var chat = await _chatService.GetChatByIdAsync(request.ChatId.Value, userId);
        if (chat == null)
            throw new InvalidOperationException("Чат не найден.");

        var messages = new List<OllamaMessageInput>();
        _logger.LogInformation("DocumentAgent: Loading {Count} messages from chat", chat.Messages.Count);
        foreach (var m in chat.Messages.OrderBy(x => x.CreatedAt))
        {
            // Пропускаем служебные сообщения (шаги агента с StepNumber) — 
            // они нужны только для UI, не для контекста LLM
            if (m.StepNumber.HasValue)
            {
                _logger.LogDebug("DocumentAgent: Skipping step message: {Content}", m.Content.Substring(0, Math.Min(50, m.Content.Length)));
                continue;
            }
                
            if (m.Role is "user" or "assistant" or "system")
            {
                _logger.LogInformation("DocumentAgent: Adding message [{Role}]: {Content}", m.Role, m.Content.Substring(0, Math.Min(80, m.Content.Length)));
                messages.Add(new OllamaMessageInput(m.Role, m.Content));
            }
        }
        _logger.LogInformation("DocumentAgent: Total messages to send to LLM: {Count}", messages.Count);

        var systemPrompt = AgentSystemPrompt.GetSystemPrompt("ru");
        var steps = new List<AgentStepDTO>();
        var stepNumber = 0;
        string? finalMessage = null;

        Func<string, Task>? CreateFilteredChunkCallback()
        {
            if (onStatusCheck == null) return null;
            var buffer = new System.Text.StringBuilder();
            var lastForwarded = 0;
            var stopped = false;
            return async (chunk) =>
            {
                if (stopped) return;
                buffer.Append(chunk);
                var s = buffer.ToString();
                var idx = s.IndexOf("TOOL_CALL", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var toSend = s.Substring(lastForwarded, idx - lastForwarded);
                    if (toSend.Length > 0)
                        await onStatusCheck(toSend);
                    stopped = true;
                    return;
                }

                var holdBack = Math.Min(8, s.Length);
                var safeEnd = s.Length - holdBack;
                if (safeEnd > lastForwarded)
                {
                    var toSend = s.Substring(lastForwarded, safeEnd - lastForwarded);
                    await onStatusCheck(toSend);
                    lastForwarded = safeEnd;
                }
            };
        }

        for (var i = 0; i < MaxToolIterations; i++)
        {
            var chunkCallback = CreateFilteredChunkCallback();

            var result = await _ollamaChatService.SendMessagesAsync(
                userId,
                messages,
                systemPrompt,
                chunkCallback,
                cancellationToken);

            var response = result.FinalMessage;
            _logger.LogInformation("DocumentAgent: LLM response (length={Length}): {Preview}", 
                response?.Length ?? 0, 
                response?.Substring(0, Math.Min(100, response?.Length ?? 0)) ?? "(empty)");
            
            var (_, toolBlock) = _toolExecutor.SplitToolCall(response);

            if (string.IsNullOrEmpty(toolBlock))
            {
                finalMessage = response.Trim();
                break;
            }

            if (!_toolExecutor.TryParseToolCall(toolBlock, out var toolName, out var args))
            {
                _logger.LogWarning("DocumentAgent: failed to parse TOOL_CALL block: {Block}", toolBlock);
                finalMessage = response.Trim();
                break;
            }
            
            // Пропускаем неизвестные инструменты (например "TOOL_CALL" от нативного API)
            var knownTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                { "read_document", "propose_document_changes" };
            if (!knownTools.Contains(toolName))
            {
                _logger.LogWarning("DocumentAgent: skipping unknown tool: {ToolName}", toolName);
                messages.Add(new OllamaMessageInput("assistant", response));
                messages.Add(new OllamaMessageInput("user", 
                    $"Инструмент '{toolName}' не существует. Используй только: read_document, propose_document_changes. Попробуй снова."));
                continue;
            }

            stepNumber++;
            var (toolResult, toolCallDto) = await _toolExecutor.ExecuteAsync(
                toolName,
                args,
                userId,
                documentId,
                cancellationToken);

            var step = new AgentStepDTO
            {
                StepNumber = stepNumber,
                Description = DocumentAgentToolExecutor.GetToolDescription(toolName, args),
                ToolCalls = new List<ToolCallDTO> { toolCallDto },
                ToolResult = toolResult.ResultMessage,
                DocumentChanges = toolResult.DocumentChanges.Count > 0 ? toolResult.DocumentChanges : null
            };

            steps.Add(step);
            await (onStepUpdate?.Invoke(step) ?? Task.CompletedTask);

            messages.Add(new OllamaMessageInput("assistant", response));

            var toolResultMessage = toolResult.ResultMessage;
            if (toolResult.DocumentChanges.Count > 0)
            {
                var compactChanges = toolResult.DocumentChanges.Select(c => new
                {
                    c.ChangeId,
                    c.ChangeType,
                    c.EntityType,
                    c.StartLine,
                    c.EndLine,
                    c.GroupId,
                    c.Order
                });
                toolResultMessage += "\n" + JsonSerializer.Serialize(compactChanges);
            }

            messages.Add(new OllamaMessageInput(
                "user",
                $"Результат вызова {toolName}: {toolResultMessage}\n\nПродолжай работу. Если задача завершена, дай финальный ответ пользователю без TOOL_CALL."));
        }

        if (string.IsNullOrEmpty(finalMessage))
        {
            // Если были шаги (tool calls), значит агент выполнил работу — сформируем ответ автоматически
            if (steps.Count > 0)
            {
                var hasChanges = steps.Any(s => s.DocumentChanges != null && s.DocumentChanges.Count > 0);
                if (hasChanges)
                {
                    finalMessage = "Готово! Изменения предложены. Проверьте их и примите или отклоните.";
                }
                else
                {
                    finalMessage = "Готово.";
                }
                _logger.LogInformation("DocumentAgent: Generated automatic response after {StepsCount} steps", steps.Count);
            }
            else
            {
                _logger.LogWarning("DocumentAgent: LLM returned empty response, using fallback message");
                finalMessage = "Не удалось получить ответ от модели. Попробуйте ещё раз.";
            }
        }

        await _chatService.AddMessageAsync(request.ChatId.Value, new ChatMessageDTO
        {
            Role = "assistant",
            Content = finalMessage
        }, userId);

        if (steps.Count > 0 && onStatusCheck != null && !string.IsNullOrWhiteSpace(finalMessage))
        {
            await onStatusCheck("\n\n");
            await onStatusCheck(finalMessage);
        }

        return new AgentResponseDTO
        {
            FinalMessage = finalMessage,
            Steps = steps,
            IsComplete = true
        };
    }
}
