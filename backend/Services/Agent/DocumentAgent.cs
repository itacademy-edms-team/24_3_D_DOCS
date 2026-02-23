using System.Linq;
using System.Text.Json;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Services.Chat;
using RusalProject.Services.ChatContext;
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
    private readonly IChatContextFileService _chatContextFileService;
    private readonly DocumentAgentToolExecutor _toolExecutor;
    private readonly ILogger<DocumentAgent> _logger;

    public DocumentAgent(
        IOllamaChatService ollamaChatService,
        IChatService chatService,
        IChatContextFileService chatContextFileService,
        DocumentAgentToolExecutor toolExecutor,
        ILogger<DocumentAgent> logger)
    {
        _ollamaChatService = ollamaChatService;
        _chatService = chatService;
        _chatContextFileService = chatContextFileService;
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
        var contextBlock = await _chatContextFileService.GetContextForPromptAsync(request.ChatId.Value, userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(contextBlock))
        {
            systemPrompt += "\n\n[Контекст из загруженных файлов]\n\n" + contextBlock;
            _logger.LogInformation("DocumentAgent: Injected context block for ChatId={ChatId}, length={Length}, preview={Preview}",
                request.ChatId.Value, contextBlock.Length,
                contextBlock.Length > 500 ? contextBlock[..500] + "..." : contextBlock);
        }
        else
        {
            _logger.LogInformation("DocumentAgent: No context injected for ChatId={ChatId} (empty or no files)", request.ChatId.Value);
        }

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

            var response = result.FinalMessage ?? string.Empty;
            _logger.LogInformation("DocumentAgent: LLM response (length={Length}): {Preview}", 
                response.Length, 
                response.Length > 0 ? response.Substring(0, Math.Min(100, response.Length)) : "(empty)");
            
            var (_, toolBlock) = _toolExecutor.SplitToolCall(response);

            if (string.IsNullOrEmpty(toolBlock))
            {
                finalMessage = response.Trim();
                break;
            }

            var toolCalls = _toolExecutor.ParseAllToolCalls(toolBlock);
            if (toolCalls.Count == 0)
            {
                _logger.LogWarning("DocumentAgent: failed to parse TOOL_CALL block: {Block}", toolBlock.Substring(0, Math.Min(200, toolBlock.Length)));
                finalMessage = response.Trim();
                break;
            }

            // При нескольких insert в одну позицию (start_line) порядок выполнения обратный:
            // первый в списке (заголовок) должен оказаться сверху, значит его выполняем последним
            var orderedCalls = ReorderInsertsForCorrectPlacement(toolCalls);

            var knownTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "read_document", "propose_document_changes" };

            var allResults = new System.Collections.Generic.List<string>();
            var allSteps = new List<AgentStepDTO>();

            foreach (var (toolName, args) in orderedCalls)
            {
                if (!knownTools.Contains(toolName))
                {
                    _logger.LogWarning("DocumentAgent: skipping unknown tool: {ToolName}", toolName);
                    allResults.Add($"Инструмент '{toolName}' не существует. Используй только: read_document, propose_document_changes.");
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

                allSteps.Add(step);
                await (onStepUpdate?.Invoke(step) ?? Task.CompletedTask);

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

                allResults.Add($"Результат вызова {toolName}: {toolResultMessage}");
            }

            steps.AddRange(allSteps);

            messages.Add(new OllamaMessageInput("assistant", response));

            var combinedResults = string.Join("\n\n", allResults);
            var continuationPrompt = toolCalls.Count > 1
                ? $"\n\nВсе {toolCalls.Count} вызова выполнены. Если задача завершена — дай финальный ответ пользователю без TOOL_CALL. Если нужно ещё изменения — вызови TOOL_CALL снова."
                : "\n\nПродолжай работу. Если задача завершена — дай финальный ответ пользователю без TOOL_CALL.";

            messages.Add(new OllamaMessageInput("user", combinedResults + continuationPrompt));
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

    /// <summary>
    /// При нескольких insert в одну и ту же строку (например start_line:1) каждый следующий
    /// вставленный блок сдвигает предыдущий вниз. Чтобы первый блок (заголовок) оказался
    /// сверху, выполняем блок insert-ов в обратном порядке. read_document и др. не трогаем.
    /// </summary>
    private static IReadOnlyList<(string ToolName, Dictionary<string, object> Args)> ReorderInsertsForCorrectPlacement(
        IReadOnlyList<(string ToolName, Dictionary<string, object> Args)> calls)
    {
        if (calls.Count <= 1) return calls;

        var list = calls.ToList();

        static bool IsInsert(string name, Dictionary<string, object> args) =>
            string.Equals(name, "propose_document_changes", StringComparison.OrdinalIgnoreCase)
            && GetArgString(args.TryGetValue("operation", out var o) ? o : null)?.ToLowerInvariant() == "insert";

        var insertRunStart = -1;
        var insertRunEnd = -1;
        for (var i = 0; i < list.Count; i++)
        {
            if (IsInsert(list[i].ToolName, list[i].Args))
            {
                if (insertRunStart < 0) insertRunStart = i;
                insertRunEnd = i;
            }
            else
            {
                insertRunStart = -1;
                insertRunEnd = -1;
            }
        }

        if (insertRunStart < 0 || insertRunEnd <= insertRunStart) return calls;

        var insertRun = list.Skip(insertRunStart).Take(insertRunEnd - insertRunStart + 1).ToList();
        var samePos = true;
        int? refLine = null;
        foreach (var (_, args) in insertRun)
        {
            var sl = GetArgInt(args, "start_line");
            if (refLine == null) refLine = sl;
            else if (sl != refLine) { samePos = false; break; }
        }

        if (!samePos || insertRun.Count <= 1) return calls;

        insertRun.Reverse();
        for (var i = 0; i < insertRun.Count; i++)
            list[insertRunStart + i] = insertRun[i];

        return list;
    }

    private static string? GetArgString(object? v)
    {
        if (v == null) return null;
        if (v is string s) return s;
        if (v is JsonElement je) return je.GetString();
        return v.ToString();
    }

    private static int GetArgInt(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var v)) return 1;
        if (v is int i) return i;
        if (v is long l) return (int)l;
        if (v is JsonElement je && je.ValueKind == JsonValueKind.Number) return je.GetInt32();
        if (v is JsonElement je2 && je2.ValueKind == JsonValueKind.String)
            return int.TryParse(je2.GetString(), out var n) ? n : 1;
        return 1;
    }
}
