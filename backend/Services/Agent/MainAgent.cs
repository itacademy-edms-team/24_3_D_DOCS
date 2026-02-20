using System.Collections.Generic;
using System.Linq;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Services.Chat;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

/// <summary>
/// Main agent for document management (scope=global). Uses CRUDdocTools with a tool-calling loop.
/// </summary>
public class MainAgent : IMainAgent
{
    private readonly IOllamaChatService _ollamaChatService;
    private readonly IChatService _chatService;
    private readonly MainAgentToolExecutor _toolExecutor;
    private readonly ILogger<MainAgent> _logger;

    private const int MaxToolIterations = 5;

    public MainAgent(
        IOllamaChatService ollamaChatService,
        IChatService chatService,
        MainAgentToolExecutor toolExecutor,
        ILogger<MainAgent> logger)
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
        Func<string, Task>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        if (!request.ChatId.HasValue)
            throw new InvalidOperationException("ChatId обязателен для Main Agent.");

        _logger.LogInformation("MainAgent: Running. ChatId={ChatId}, UserId={UserId}", request.ChatId, userId);

        var chat = await _chatService.GetChatByIdAsync(request.ChatId.Value, userId);
        if (chat == null)
            throw new InvalidOperationException("Чат не найден.");

        var messages = new List<OllamaMessageInput>();
        foreach (var m in chat.Messages.OrderBy(x => x.CreatedAt))
        {
            if (m.Role is "user" or "assistant" or "system")
                messages.Add(new OllamaMessageInput(m.Role, m.Content));
        }

        var systemPrompt = MainAgentSystemPrompt.GetSystemPrompt();
        var steps = new List<AgentStepDTO>();
        var stepNumber = 0;
        string? finalMessage = null;

        Func<string, Task>? CreateFilteredChunkCallback()
        {
            if (onChunk == null) return null;
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
                        await onChunk(toSend);
                    stopped = true;
                    return;
                }
                var holdBack = Math.Min(8, s.Length);
                var safeEnd = s.Length - holdBack;
                if (safeEnd > lastForwarded)
                {
                    var toSend = s.Substring(lastForwarded, safeEnd - lastForwarded);
                    await onChunk(toSend);
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

            var (_, toolBlock) = _toolExecutor.SplitToolCall(response);

            if (string.IsNullOrEmpty(toolBlock))
            {
                finalMessage = response.Trim();
                break;
            }

            if (!_toolExecutor.TryParseToolCall(toolBlock, out var toolName, out var args))
            {
                _logger.LogWarning("Failed to parse TOOL_CALL block: {Block}", toolBlock);
                finalMessage = response.Trim();
                break;
            }

            stepNumber++;
            var (toolResult, toolCallDto) = await _toolExecutor.ExecuteAsync(toolName, args, userId, cancellationToken);

            var description = MainAgentToolExecutor.GetToolDescription(toolName, args);
            var step = new AgentStepDTO
            {
                StepNumber = stepNumber,
                Description = description,
                ToolCalls = new List<ToolCallDTO> { toolCallDto },
                ToolResult = toolResult
            };
            steps.Add(step);
            await (onStepUpdate?.Invoke(step) ?? Task.CompletedTask);

            messages.Add(new OllamaMessageInput("assistant", response));
            messages.Add(new OllamaMessageInput("user", $"Результат вызова {toolName}: {toolResult}\n\nИспользуй этот результат для ответа пользователю."));
        }

        if (string.IsNullOrEmpty(finalMessage) && messages.Count > 0)
        {
            var lastAssistant = messages.LastOrDefault(m => m.Role == "assistant");
            finalMessage = lastAssistant.Content ?? "Не удалось получить ответ.";
        }

        finalMessage ??= "Готово.";

        await _chatService.AddMessageAsync(request.ChatId.Value, new ChatMessageDTO
        {
            Role = "assistant",
            Content = finalMessage
        }, userId);

        if (steps.Count > 0 && onChunk != null && !string.IsNullOrWhiteSpace(finalMessage))
        {
            await onChunk("\n\n");
            await onChunk(finalMessage);
        }

        return new AgentResponseDTO
        {
            FinalMessage = finalMessage,
            Steps = steps,
            IsComplete = true
        };
    }
}
