using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.Json;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Services.Agent.Tools;
using RusalProject.Services.Documents;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

public class AgentService : IAgentService
{
    private readonly IOllamaService _ollamaService;
    private readonly IDocumentService _documentService;
    private readonly List<ITool> _tools;
    private readonly ILogger<AgentService> _logger;
    private const int MaxIterations = 32;
    private const int CheckIterationInterval = 5; // Проверка на каждой 5-й итерации
    private const int MaxToolRetries = 3;
    private static readonly TimeSpan BaseToolTimeout = TimeSpan.FromSeconds(10);
    private const int ResponseRepeatThreshold = 3;
    private const int StuckIterationsThreshold = 6;

    public AgentService(
        IOllamaService ollamaService,
        IDocumentService documentService,
        RAGSearchTool ragSearchTool,
        TableSearchTool tableSearchTool,
        InsertTool insertTool,
        EditTool editTool,
        DeleteTool deleteTool,
        WebSearchTool webSearchTool,
        ImageAnalysisTool imageAnalysisTool,
        GetHeaderTool getHeaderTool,
        GetDateTimeTool getDateTimeTool,
        GrepTool grepTool,
        ILogger<AgentService> logger)
    {
        _ollamaService = ollamaService;
        _documentService = documentService;
        _logger = logger;

        _tools = new List<ITool>
        {
            ragSearchTool,
            tableSearchTool,
            insertTool,
            editTool,
            deleteTool,
            webSearchTool,
            imageAnalysisTool,
            getHeaderTool,
            getDateTimeTool,
            grepTool
        };
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, CancellationToken cancellationToken = default)
    {
        return await ProcessAsync(request, userId, null, cancellationToken);
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, Func<int, Task>? onDocumentChange, CancellationToken cancellationToken = default)
    {
        return await ProcessAsync(request, userId, onStepUpdate, onDocumentChange, null, cancellationToken);
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, Func<int, Task>? onDocumentChange, Func<string, Task>? onStatusCheck, CancellationToken cancellationToken = default)
    {
        return await ProcessAsyncInternal(request, userId, onStepUpdate, onDocumentChange, onStatusCheck, cancellationToken);
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, CancellationToken cancellationToken = default)
    {
        return await ProcessAsync(request, userId, onStepUpdate, null, cancellationToken);
    }

    private async Task<AgentResponseDTO> ProcessAsyncInternal(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, Func<int, Task>? onDocumentChange, Func<string, Task>? onStatusCheck, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AgentService: Начало обработки запроса. DocumentId={DocumentId}, UserId={UserId}, UserMessage={UserMessage}",
            request.DocumentId, userId, request.UserMessage);

        var steps = new List<AgentStepDTO>();
        var stepNumber = 0;
        var documentChangeCount = 0; // Счетчик изменений документа
        var iterationCount = 0; // Счетчик итераций
            var documentSnapshot = string.Empty;

        async Task NotifyStepUpdate(AgentStepDTO step)
        {
            if (onStepUpdate != null)
            {
                await onStepUpdate(step);
            }
        }

        try
        {
            // Step 1: Read document
            stepNumber++;
            var readStep = new AgentStepDTO
            {
                StepNumber = stepNumber,
                Description = "📄 Чтение документа..."
            };
            steps.Add(readStep);
            await NotifyStepUpdate(readStep);
            
            _logger.LogInformation("AgentService: Чтение документа DocumentId={DocumentId}, UserId={UserId}", request.DocumentId, userId);

            var document = await _documentService.GetDocumentWithContentAsync(request.DocumentId, userId);
            if (document == null)
            {
                _logger.LogWarning("AgentService: Документ не найден DocumentId={DocumentId}, UserId={UserId}", request.DocumentId, userId);
                readStep.Description = "Ошибка: Документ не найден";
                return new AgentResponseDTO
                {
                    FinalMessage = "Ошибка: Документ не найден",
                    Steps = steps,
                    IsComplete = true
                };
            }

            var contentLength = document.Content?.Length ?? 0;
            readStep.Description = $"Документ прочитан ({contentLength} символов)";
            await NotifyStepUpdate(readStep);
            _logger.LogInformation("AgentService: Документ прочитан. DocumentId={DocumentId}, ContentLength={ContentLength}", request.DocumentId, contentLength);
            // snapshot initial document content for verification after tool calls
            documentSnapshot = document.Content ?? string.Empty;

            // Step 2: Get unified system prompt
            var systemPrompt = GetSystemPrompt();
            _logger.LogDebug("AgentService: Системный промпт подготовлен. PromptLength={PromptLength}", systemPrompt.Length);

            // Step 3: Iterative agent loop
            var messages = new List<ChatMessage>();
            var currentUserMessage = request.UserMessage;
            var consecutiveNoToolCalls = 0; // Track consecutive iterations without tool calls
            const int MaxConsecutiveNoToolCalls = 2; // Stop after 2 iterations without tool calls
            var allToolResults = new List<string>();
            var todoSteps = new List<string>();
            var todoIndex = 0;
            var planCreated = false;
            var prevResponseHash = string.Empty;
            var sameResponseCount = 0;
            var iterationsWithoutProgress = 0;
            var lastAllToolResultsHash = string.Empty;

            _logger.LogInformation("AgentService: Начало итеративного цикла. MaxIterations={MaxIterations}, UserMessage={UserMessage}", 
                MaxIterations, request.UserMessage);

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                iterationCount = iteration + 1; // 1-based счетчик итераций

                // Проверка на каждой 5-й итерации — запускаем явный status-check через LLM
                if (iterationCount % CheckIterationInterval == 0)
                {
                    _logger.LogInformation("AgentService: Проверка на {IterationCount}-й итерации (status-check). Changes={DocumentChangeCount}",
                        iterationCount, documentChangeCount);

                    try
                    {
                        var iterationContext = BuildIterationContext(request, messages, allToolResults, iterationCount);
                        var statusPrompt = $"Goal: {request.UserMessage}\nIteration: {iterationCount}\nContext Summary:\n{iterationContext}\n\nAssistant so far:\n{(messages.LastOrDefault()?.Content ?? "")}\n\nUsing the goal and context above, reply with a single word on the first line: DONE or CONTINUE. On the second line provide a one-sentence reason.";

                        _logger.LogDebug("AgentService: Вызов status-check на {IterationCount}-й итерации. PromptLength={Length}", iterationCount, statusPrompt.Length);
                        var statusResponse = await _ollamaService.GenerateChatAsync(string.Empty, statusPrompt, messages, cancellationToken);
                        var statusLines = (statusResponse ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var verdict = statusLines.Length > 0 ? statusLines[0].Trim().ToUpperInvariant() : string.Empty;
                        var reason = statusLines.Length > 1 ? statusLines[1].Trim() : string.Empty;

                        if (onStatusCheck != null)
                        {
                            try { await onStatusCheck($"{verdict}: {reason}"); } catch (Exception ex) { _logger.LogWarning(ex, "AgentService: Ошибка в callback onStatusCheck"); }
                        }

                        _logger.LogInformation("AgentService: Status-check verdict={Verdict}, reason={Reason}", verdict, reason);

                        if (verdict == "DONE")
                        {
                            var stopStep = new AgentStepDTO
                            {
                                StepNumber = ++stepNumber,
                                Description = $"✅ Задача выполнена (статус-чекинг на {iterationCount}-й итерации)",
                                ToolResult = $"Verdict: DONE. Reason: {reason}"
                            };
                            steps.Add(stopStep);
                            await NotifyStepUpdate(stopStep);

                            return new AgentResponseDTO
                            {
                                FinalMessage = $"Задача выполнена. Причина: {reason}",
                                Steps = steps,
                                IsComplete = true
                            };
                        }
                        else
                        {
                            _logger.LogInformation("AgentService: Status-check returned CONTINUE — продолжаем итерации.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "AgentService: Ошибка при выполнении status-check на итерации {IterationCount}, применяем fallback-правила", iterationCount);
                        // Fallback: если статус-чекинг упал, используем прежние эвристики
                        if (ShouldStopAtIterationCheck(iterationCount, documentChangeCount))
                        {
                            _logger.LogInformation("AgentService: Fallback — Остановка агента на проверке {IterationCount}-й итерации. Changes={DocumentChangeCount}",
                                iterationCount, documentChangeCount);

                            var stopStep = new AgentStepDTO
                            {
                                StepNumber = ++stepNumber,
                                Description = $"✅ Задача выполнена (fallback-проверка на {iterationCount}-й итерации)",
                                ToolResult = $"Выполнено {documentChangeCount} изменений в документе за {iterationCount} итераций."
                            };
                            steps.Add(stopStep);
                            await NotifyStepUpdate(stopStep);

                            return new AgentResponseDTO
                            {
                                FinalMessage = $"Задача выполнена (fallback). Проверка на {iterationCount}-й итерации показала завершение работы.",
                                Steps = steps,
                                IsComplete = true
                            };
                        }
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("AgentService: Обработка отменена через CancellationToken. Iteration={Iteration}", iteration + 1);
                    var cancelStep = new AgentStepDTO
                    {
                        StepNumber = ++stepNumber,
                        Description = "⏹ Задача остановлена пользователем",
                        ToolResult = "Обработка была остановлена по запросу клиента."
                    };
                    steps.Add(cancelStep);
                    await NotifyStepUpdate(cancelStep);

                    return new AgentResponseDTO
                    {
                        FinalMessage = "Задача была остановлена пользователем.",
                        Steps = steps,
                        IsComplete = true
                    };
                }
                _logger.LogInformation("AgentService: Итерация {Iteration}/{MaxIterations}. ConsecutiveNoToolCalls={ConsecutiveNoToolCalls}", 
                    iteration + 1, MaxIterations, consecutiveNoToolCalls);
                
                stepNumber++;
                var stepDescription = iteration == 0
                    ? "🤔 Анализ запроса (LLM)..."
                    : $"🔄 Итерация {iteration + 1}: обработка результатов инструментов (LLM)...";

                var currentStep = new AgentStepDTO
                {
                    StepNumber = stepNumber,
                    Description = stepDescription
                };
                steps.Add(currentStep);
                await NotifyStepUpdate(currentStep);

                // Prepare tools for LLM
                var toolDefinitions = _tools.Select(t => new ToolDefinition
                {
                    Name = t.Name,
                    Description = t.Description,
                    Parameters = t.GetParametersSchema()
                }).ToList();

                // Add user_id and document_id to tool arguments context in system prompt
                // and explicitly перечисляем все доступные инструменты на каждую итерацию
                var toolsSummary = string.Join("\n", toolDefinitions.Select(td => $"- {td.Name}: {td.Description}"));

                // Build iteration context summary and append goal+context to system prompt so agent repeats goal each iteration
                var iterationContextForPrompt = BuildIterationContext(request, messages, allToolResults, iterationCount);

                var enhancedSystemPrompt = systemPrompt
                    + $"\n\nКонтекст текущей сессии: document_id={request.DocumentId}, user_id={userId}."
                    + "\nТы уже видел предыдущие сообщения и результаты инструментов в истории диалога — используй их как полноценный контекст, не забывай о ранее сгенерированном содержимом."
                    + "\n\nСписок ДОСТУПНЫХ инструментов в ЭТОЙ сессии (можно вызывать только их, нельзя придумывать другие):\n"
                    + toolsSummary
                    + $"\n\nCurrent goal (repeat this each iteration): {request.UserMessage}\nCollected context summary:\n{iterationContextForPrompt}";

                // Build iteration context to include in prompt (appended below)
                // Call LLM
                currentStep.Description = stepDescription + " (ожидание ответа...)";
                await NotifyStepUpdate(currentStep);
                
                _logger.LogDebug("AgentService: Вызов LLM. Iteration={Iteration}, UserMessageLength={UserMessageLength}, MessagesCount={MessagesCount}, ToolsCount={ToolsCount}", 
                    iteration + 1, currentUserMessage.Length, messages.Count, toolDefinitions.Count);
                
                var response = await _ollamaService.GenerateChatWithToolsAsync(
                    enhancedSystemPrompt,
                    currentUserMessage,
                    toolDefinitions,
                    messages,
                    cancellationToken
                );
                
                var toolCallsCount = response.ToolCalls?.Count ?? 0;
                _logger.LogInformation("AgentService: Ответ LLM получен. Iteration={Iteration}, ToolCallsCount={ToolCallsCount}, ResponseContentLength={ResponseContentLength}", 
                    iteration + 1, toolCallsCount, response.Content?.Length ?? 0);
                
                if (toolCallsCount > 0)
                {
                    var toolNames = string.Join(", ", response.ToolCalls!.Select(tc => tc.Name));
                    _logger.LogInformation("AgentService: Инструменты для вызова: {ToolNames}", toolNames);
                }
                
                currentStep.Description = stepDescription.Replace(" (ожидание ответа...)", "") + $" (ответ получен, toolCalls: {toolCallsCount})";
                await NotifyStepUpdate(currentStep);
                
                // Response loop detection: hash response content
                try
                {
                    var respContent = response.Content ?? string.Empty;
                    string respHash;
                    using (var sha = SHA256.Create())
                    {
                        respHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(respContent)));
                    }
                    if (!string.IsNullOrEmpty(prevResponseHash) && respHash == prevResponseHash)
                    {
                        sameResponseCount++;
                    }
                    else
                    {
                        sameResponseCount = 0;
                        prevResponseHash = respHash;
                    }

                    if (sameResponseCount >= ResponseRepeatThreshold)
                    {
                        _logger.LogWarning("AgentService: Обнаружен цикл ответов LLM (repeatCount={Count}), переключаем стратегию", sameResponseCount);
                        // Inject instruction to change strategy
                        currentUserMessage = "ALTERNATE_STRATEGY: You have been repeating. Stop describing plans — perform concrete edits using tools or produce a short JSON plan with explicit steps.";
                        messages.Add(new ChatMessage { Role = "assistant", Content = response.Content });
                        await NotifyStepUpdate(new AgentStepDTO { StepNumber = ++stepNumber, Description = "⚠️ Агент зациклился — переключение стратегии", ToolResult = "Switched to alternate strategy to break loop." });
                        // continue loop, next iteration will use currentUserMessage
                        continue;
                    }
                }
                catch { /* non-fatal */ }

                // Add user message to history
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = currentUserMessage
                });

                // Process tool calls if any
                if (response.ToolCalls != null && response.ToolCalls.Count > 0)
                {
                    _logger.LogInformation("AgentService: Обработка вызовов инструментов. Iteration={Iteration}, ToolCallsCount={ToolCallsCount}", 
                        iteration + 1, response.ToolCalls.Count);
                    
                    consecutiveNoToolCalls = 0; // Reset counter when tools are called
                    var toolCalls = new List<ToolCallDTO>();
                    var toolResults = new List<string>();

                    foreach (var toolCall in response.ToolCalls)
                    {
                        _logger.LogDebug("AgentService: Выполнение инструмента. ToolName={ToolName}, Arguments={Arguments}", 
                            toolCall.Name, string.Join(", ", toolCall.Arguments.Select(kv => $"{kv.Key}={kv.Value}")));
                        
                        var tool = _tools.FirstOrDefault(t => t.Name == toolCall.Name);
                        if (tool == null)
                        {
                            _logger.LogWarning("AgentService: Инструмент не найден. ToolName={ToolName}", toolCall.Name);
                            toolResults.Add($"Ошибка: Инструмент {toolCall.Name} не найден");
                            continue;
                        }

                        // Add user_id and document_id to arguments if needed
                        var arguments = new Dictionary<string, object>(toolCall.Arguments);

                        // Принудительно прокидываем document_id и user_id для инструментов, работающих с документом
                        var toolsNeedingDocumentId = new[]
                        {
                            "document_edit", // для обратной совместимости
                            "rag_search",    // для обратной совместимости
                            "rag_query",
                            "table_search",
                            "image_analyze", // для обратной совместимости
                            "image_analyse",
                            "insert",
                            "edit",
                            "delete",
                            "get_header",
                            "grep"
                        };

                        var toolsAlsoNeedingUserId = new[]
                        {
                            "document_edit",
                            "image_analyze",
                            "image_analyse",
                            "insert",
                            "edit",
                            "delete",
                            "get_header",
                            "grep",
                            "rag_query",
                            "table_search"
                        };

                        if (toolsNeedingDocumentId.Contains(toolCall.Name))
                        {
                            // Force document_id to match request.DocumentId to prevent cross-document queries
                            arguments["document_id"] = request.DocumentId.ToString();
                        }

                        if (toolsAlsoNeedingUserId.Contains(toolCall.Name) && !arguments.ContainsKey("user_id"))
                        {
                            arguments["user_id"] = userId.ToString();
                        }

                        try
                        {
                            // retry loop with per-attempt timeout
                            string toolResult = string.Empty;
                            int attempt = 0;
                            long elapsedMs = 0;
                            while (attempt < MaxToolRetries)
                            {
                                attempt++;
                                try
                                {
                                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                                    var timeout = TimeSpan.FromSeconds(BaseToolTimeout.TotalSeconds * Math.Pow(2, attempt - 1));
                                    cts.CancelAfter(timeout);

                                    var sw = Stopwatch.StartNew();
                                    toolResult = await tool.ExecuteAsync(arguments, cts.Token);
                                    sw.Stop();
                                    elapsedMs = sw.ElapsedMilliseconds;
                                    break;
                                }
                                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                                {
                                    _logger.LogWarning("AgentService: Timeout executing tool {ToolName} attempt {Attempt}", toolCall.Name, attempt);
                                    if (attempt < MaxToolRetries)
                                    {
                                        await Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)), cancellationToken)
                                            .ContinueWith(_ => { }, TaskScheduler.Default);
                                        continue;
                                    }
                                    toolResult = $"Ошибка: инструмент {toolCall.Name} превысил таймаут после {MaxToolRetries} попыток.";
                                }
                            }

                            // Telemetry: hash toolResult
                            string toolResultHash = string.Empty;
                            try
                            {
                                using var sha = SHA256.Create();
                                var bytes = Encoding.UTF8.GetBytes(toolResult ?? "");
                                toolResultHash = Convert.ToHexString(sha.ComputeHash(bytes));
                            }
                            catch { toolResultHash = string.Empty; }

                            _logger.LogInformation("AgentService: Инструмент {ToolName} завершил выполнение (attempts={Attempts}, ms={Ms}), hash={Hash}", toolCall.Name, Math.Min(attempt, MaxToolRetries), elapsedMs, toolResultHash);

                            // Проверяем, был ли изменен документ — верификация через повторный запрос контента
                            var documentModifyingTools = new[] { "insert", "edit", "delete" };
                            if (documentModifyingTools.Contains(toolCall.Name) && !toolResult.Contains("Ошибка") && !toolResult.Contains("Предупреждение"))
                            {
                                try
                                                               {
                                    var afterDoc = await _documentService.GetDocumentWithContentAsync(request.DocumentId, userId);
                                    var afterContent = afterDoc?.Content ?? string.Empty;

                                    if (afterContent != documentSnapshot)
                                    {
                                        // Подтверждённое изменение
                                        documentChangeCount++;
                                        documentSnapshot = afterContent;
                                        _logger.LogInformation("AgentService: Документ действительно изменён. ToolName={ToolName}, TotalChanges={DocumentChangeCount}",
                                            toolCall.Name, documentChangeCount);

                                        // Вызываем callback при изменении документа
                                        if (onDocumentChange != null)
                                        {
                                            try
                                            {
                                                await onDocumentChange(documentChangeCount);
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogWarning(ex, "AgentService: Ошибка в callback onDocumentChange");
                                            }
                                        }

                                        // Проверяем условие остановки после внесения изменений
                                        if (ShouldStopAfterDocumentChange(documentChangeCount, iterationCount))
                                        {
                                            _logger.LogInformation("AgentService: Остановка агента после внесения изменений в документ. Changes={DocumentChangeCount}, Iterations={IterationCount}",
                                                documentChangeCount, iterationCount);

                                            var stopStep = new AgentStepDTO
                                            {
                                                StepNumber = ++stepNumber,
                                                Description = "✅ Задача выполнена (документ изменен)",
                                                ToolResult = $"Выполнено {documentChangeCount} изменений в документе за {iterationCount} итераций."
                                            };
                                            steps.Add(stopStep);
                                            await NotifyStepUpdate(stopStep);

                                            return new AgentResponseDTO
                                            {
                                                FinalMessage = $"Задача выполнена. В документ внесено {documentChangeCount} изменений.",
                                                Steps = steps,
                                                IsComplete = true
                                            };
                                        }
                                    }
                                    else
                                    {
                                        // Не найдено изменений — отмечаем предупреждением и не учитываем как изменение
                                        _logger.LogWarning("AgentService: Инструмент заявил об изменении, но содержимое документа не поменялось. ToolName={ToolName}", toolCall.Name);
                                        toolResult += "\n\n[Warning: tool reported success but document content unchanged]";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "AgentService: Не удалось проверить содержимое документа после выполнения инструмента {Tool}", toolCall.Name);
                                    // В случае ошибки верификации — не считать изменение подтверждённым
                                    toolResult += "\n\n[Warning: could not verify document change]";
                                }
                            }

                            _logger.LogInformation("AgentService: Инструмент выполнен успешно. ToolName={ToolName}, ResultLength={ResultLength}",
                                toolCall.Name, toolResult.Length);

                            // Format tool result for display (truncate if too long)
                            var displayResult = toolResult.Length > 200
                                ? toolResult.Substring(0, 200) + "..."
                                : toolResult;

                            toolResults.Add(toolResult);
                            allToolResults.Add(toolResult);

                            toolCalls.Add(new ToolCallDTO
                            {
                                ToolName = toolCall.Name,
                                Arguments = arguments,
                                Result = displayResult
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "AgentService: Ошибка при выполнении инструмента. ToolName={ToolName}", toolCall.Name);
                            var errorResult = $"Ошибка при выполнении {toolCall.Name}: {ex.Message}";
                            toolResults.Add(errorResult);
                            toolCalls.Add(new ToolCallDTO
                            {
                                ToolName = toolCall.Name,
                                Arguments = arguments,
                                Result = errorResult
                            });
                        }
                    }

                    // Update step with tool calls
                    var lastStep = steps[^1];
                    lastStep.ToolCalls = toolCalls;
                    lastStep.ToolResult = string.Join("\n\n", toolResults);
                    await NotifyStepUpdate(lastStep);

                    // Add assistant message with tool calls to history
                    // Note: OllamaService.ChatMessage.ToolCall is a single property, but we have multiple tool calls
                    // For now, we'll just store the content
                    messages.Add(new ChatMessage
                    {
                        Role = "assistant",
                        Content = response.Content
                    });

                    // Add tool result as user message for next iteration
                    currentUserMessage = $"Результаты выполнения инструментов:\n{string.Join("\n\n", toolResults)}";

                    // If we have a plan and executed tool calls for the current todo step, advance to next step
                    if (planCreated && todoIndex < todoSteps.Count)
                    {
                        _logger.LogInformation("AgentService: План — шаг выполнен, продвигаем индекс плана. PreviousIndex={TodoIndex}", todoIndex);
                        todoIndex++;
                    }

                    // Telemetry: check if tool outputs changed compared to previous iterations
                    try
                    {
                        string combined = string.Join("\n", allToolResults);
                        string combinedHash;
                        using (var sha = SHA256.Create())
                        {
                            combinedHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(combined)));
                        }
                        if (!string.IsNullOrEmpty(lastAllToolResultsHash) && combinedHash == lastAllToolResultsHash)
                        {
                            iterationsWithoutProgress++;
                        }
                        else
                        {
                            iterationsWithoutProgress = 0;
                            lastAllToolResultsHash = combinedHash;
                        }

                        if (iterationsWithoutProgress >= StuckIterationsThreshold)
                        {
                            _logger.LogWarning("AgentService: Обнаружено отсутствие прогресса за {N} итераций — агент застрял", iterationsWithoutProgress);
                            var stuckStep = new AgentStepDTO
                            {
                                StepNumber = ++stepNumber,
                                Description = "⚠️ Агент застрял — требуется вмешательство пользователя",
                                ToolResult = "No confirmed progress detected. Please clarify or approve next actions."
                            };
                            steps.Add(stuckStep);
                            await NotifyStepUpdate(stuckStep);

                            return new AgentResponseDTO
                            {
                                FinalMessage = "Agent is stuck: no progress detected. Please provide clarification or approve next steps.",
                                Steps = steps,
                                IsComplete = true
                            };
                        }
                    }
                    catch { /* non-fatal */ }

                    // Continue loop to process tool results
                    continue;
                }

                // No tool calls - increment counter and check conditions
                consecutiveNoToolCalls++;
                var responseContentPreview = response.Content != null 
                    ? response.Content.Substring(0, Math.Min(100, response.Content.Length)) 
                    : "";
                _logger.LogInformation("AgentService: Нет вызовов инструментов. Iteration={Iteration}, ConsecutiveNoToolCalls={ConsecutiveNoToolCalls}, ResponseContent={ResponseContent}", 
                    iteration + 1, consecutiveNoToolCalls, responseContentPreview);
                
                // Update iterationsWithoutProgress when no tool outputs were added
                try
                {
                    iterationsWithoutProgress++;
                    if (iterationsWithoutProgress >= StuckIterationsThreshold)
                    {
                        _logger.LogWarning("AgentService: Обнаружено отсутствие прогресса за {N} итераций без tool outputs — агент застрял", iterationsWithoutProgress);
                        var stuckStep = new AgentStepDTO
                        {
                            StepNumber = ++stepNumber,
                            Description = "⚠️ Агент застрял — требуется вмешательство пользователя",
                            ToolResult = "No confirmed progress detected. Please clarify or approve next actions."
                        };
                        steps.Add(stuckStep);
                        await NotifyStepUpdate(stuckStep);

                        return new AgentResponseDTO
                        {
                            FinalMessage = "Agent is stuck: no progress detected. Please provide clarification or approve next steps.",
                            Steps = steps,
                            IsComplete = true
                        };
                    }
                }
                catch { }
                
                // Build iteration context summary and run status-check prompt
                try
                {
                    var iterationContext = BuildIterationContext(request, messages, allToolResults, iterationCount);
                    var statusPrompt = $"Goal: {request.UserMessage}\nIteration: {iterationCount}\nContext Summary:\n{iterationContext}\n\nAssistant response:\n{response.Content}\n\nUsing the goal and context above, reply with a single word on the first line: DONE or CONTINUE. On the second line provide a one-sentence reason.";

                    _logger.LogDebug("AgentService: Вызов проверки статуса агента. Iteration={Iteration}, PromptPreview={Preview}", iterationCount, statusPrompt.Length > 200 ? statusPrompt.Substring(0, 200) + "..." : statusPrompt);

                    var statusResponse = await _ollamaService.GenerateChatAsync(string.Empty, statusPrompt, null, cancellationToken);
                    var statusLines = (statusResponse ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var verdict = statusLines.Length > 0 ? statusLines[0].Trim().ToUpperInvariant() : string.Empty;
                    var reason = statusLines.Length > 1 ? statusLines[1].Trim() : string.Empty;

                    // Callback for status check
                    if (onStatusCheck != null)
                    {
                        try
                        {
                            await onStatusCheck($"{verdict}: {reason}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "AgentService: Ошибка в callback onStatusCheck");
                        }
                    }

                    _logger.LogInformation("AgentService: Результат проверки статуса. Iteration={Iteration}, Verdict={Verdict}, Reason={Reason}", iterationCount, verdict, reason);

                    if (verdict == "DONE")
                    {
                        var doneStep = steps[^1];
                        doneStep.Description = "✅ Задача выполнена (статус-чекинг)";
                        doneStep.ToolResult = $"Статус: DONE. Причина: {reason}";
                        await NotifyStepUpdate(doneStep);

                        return new AgentResponseDTO
                        {
                            FinalMessage = $"Задача выполнена. Причина: {reason}",
                            Steps = steps,
                            IsComplete = true
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AgentService: Ошибка при выполнении status-check, продолжаем обычную логику");
                }

                // If we haven't built a plan yet, ask the LLM to produce a short ToDo plan (1-6 items)
                if (!planCreated && iterationCount == 1)
                {
                    try
                    {
                        var planPrompt = $"Based on the goal: \"{request.UserMessage}\" and the assistant's analysis below, produce a concise numbered ToDo plan (1-6 items), each on its own line starting with a number. Keep items short and actionable.\n\nAssistant analysis:\n{response.Content}";
                        _logger.LogDebug("AgentService: Requesting plan from LLM. PromptPreview={Preview}", planPrompt.Length > 200 ? planPrompt.Substring(0, 200) + "..." : planPrompt);
                        var planResponse = await _ollamaService.GenerateChatAsync(string.Empty, planPrompt, messages, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(planResponse))
                        {
                            // Try to extract JSON plan first
                            var parsed = false;
                            try
                            {
                                var firstJson = planResponse.Trim();
                                var start = firstJson.IndexOfAny(new[] { '[', '{' });
                                if (start >= 0)
                                {
                                    var jsonPart = firstJson.Substring(start);
                                    using var doc = JsonDocument.Parse(jsonPart);
                                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var el in doc.RootElement.EnumerateArray())
                                        {
                                            if (el.ValueKind == JsonValueKind.Object)
                                            {
                                                if (el.TryGetProperty("action", out var actionProp))
                                                {
                                                    var action = actionProp.GetString() ?? el.ToString();
                                                    todoSteps.Add(action.Trim());
                                                }
                                                else
                                                {
                                                    todoSteps.Add(el.ToString());
                                                }
                                            }
                                            else
                                            {
                                                todoSteps.Add(el.ToString());
                                            }
                                        }
                                        parsed = true;
                                    }
                                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                                    {
                                        // If object contains numbered keys or a 'steps' array
                                        if (doc.RootElement.TryGetProperty("steps", out var stepsProp) && stepsProp.ValueKind == JsonValueKind.Array)
                                        {
                                            foreach (var el in stepsProp.EnumerateArray())
                                            {
                                                todoSteps.Add(el.ToString());
                                            }
                                            parsed = true;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "AgentService: План LLM не является корректным JSON, применяем fallback парсинг текста");
                                parsed = false;
                            }

                            if (!parsed)
                            {
                                var lines = planResponse.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var line in lines)
                                {
                                    var trimmed = line.Trim();
                                    // Accept lines starting with a digit or a dash
                                    if (char.IsDigit(trimmed.FirstOrDefault()))
                                    {
                                        var idx = trimmed.IndexOf('.');
                                        var item = idx > 0 ? trimmed.Substring(idx + 1).Trim() : trimmed;
                                        if (!string.IsNullOrWhiteSpace(item)) todoSteps.Add(item);
                                    }
                                    else if (trimmed.StartsWith("-"))
                                    {
                                        var item = trimmed.Substring(1).Trim();
                                        if (!string.IsNullOrWhiteSpace(item)) todoSteps.Add(item);
                                    }
                                }
                            }
                        }

                        if (todoSteps.Count > 0)
                        {
                            planCreated = true;
                            _logger.LogInformation("AgentService: План от LLM получен. StepsCount={Count}", todoSteps.Count);
                            // set next user message to execute first plan step
                            currentUserMessage = $"Выполни шаг плана 1/{todoSteps.Count}: {todoSteps[0]}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "AgentService: Не удалось получить план от LLM");
                    }
                }

                // If we have a plan and haven't yet sent current todo step as instruction, inject it
                if (planCreated && todoIndex < todoSteps.Count && !currentUserMessage.StartsWith("Выполни шаг плана"))
                {
                    currentUserMessage = $"Выполни шаг плана {todoIndex + 1}/{todoSteps.Count}: {todoSteps[todoIndex]}";
                }

                // Check if agent is describing work instead of doing it
                var contentLower = response.Content.ToLower();
                var workInProgressKeywords = new[]
                {
                    "сейчас",
                    "теперь",
                    "выполняю",
                    "анализирую",
                    "обрабатываю",
                    "начинаю",
                    "приступаю",
                    // Описание планируемых правок без вызова инструментов
                    "вставка после строки",
                    "будет добавлено",
                    "будет добавлен",
                    "будут добавлены"
                };
                var isWorkInProgress = workInProgressKeywords.Any(keyword => contentLower.Contains(keyword));

                // Check if we've had too many consecutive iterations without tool calls
                // Also check recent messages for "тип: Image" (not Caption!) to detect unprocessed images
                var recentMessagesText = string.Join("\n", messages.TakeLast(5).Select(m => m.Content ?? ""));
                var toolResultsText = currentUserMessage.Contains("Результаты выполнения инструментов:") 
                    ? currentUserMessage 
                    : string.Join("\n", messages.Where(m => m.Role == "user" && m.Content?.Contains("Результаты выполнения инструментов:") == true).TakeLast(3).Select(m => m.Content ?? ""));
                var hasUnprocessedImages = iteration > 0 && (toolResultsText.Contains("тип: Image") && !toolResultsText.Contains("IMAGE_LINE:"));

                _logger.LogDebug("AgentService: Проверка условий продолжения. IsWorkInProgress={IsWorkInProgress}, HasUnprocessedImages={HasUnprocessedImages}, ConsecutiveNoToolCalls={ConsecutiveNoToolCalls}, MaxConsecutiveNoToolCalls={MaxConsecutiveNoToolCalls}", 
                    isWorkInProgress, hasUnprocessedImages, consecutiveNoToolCalls, MaxConsecutiveNoToolCalls);

                // If agent says work keywords, reset counter and push to continue (work keywords mean agent is trying)
                // Or if has unprocessed images and we haven't exceeded limit, push to continue
                if (isWorkInProgress && iteration < MaxIterations - 1)
                {
                    consecutiveNoToolCalls = 0; // Reset counter when agent uses work keywords
                }
                
                if ((isWorkInProgress || hasUnprocessedImages) && consecutiveNoToolCalls <= MaxConsecutiveNoToolCalls && iteration < MaxIterations - 1)
                {
                    _logger.LogInformation("AgentService: Агент продолжает работу. Reason={Reason}", hasUnprocessedImages ? "HasUnprocessedImages" : "IsWorkInProgress");
                    
                    // Agent is describing work but not doing it, or has unprocessed images from rag_search
                    messages.Add(new ChatMessage
                    {
                        Role = "assistant",
                        Content = response.Content
                    });

                    if (hasUnprocessedImages)
                    {
                        currentUserMessage = $"В результатах rag_search были найдены изображения (тип: Image), но ты не вызвал image_analyze! ВЫЗОВИ image_analyze СЕЙЧАС для каждого найденного изображения. Работа не завершена - продолжай выполнять задачу!";
                    }
                    else
                    {
                        currentUserMessage =
                            "Ты описал, какие изменения нужно внести в документ (например, вставку текста или изменение строк), " +
                            "но НЕ вызвал инструменты. " +
                            "СЕЙЧАС вызови подходящие инструменты (insert/edit/delete/rag_query/image_analyse/get_header/get_datetime/grep) " +
                            "и выполни задачу через них, а не через текстовое описание. Не просто описывай — ДЕЙСТВУЙ ИНСТРУМЕНТАМИ.";
                    }
                    continue;
                }
                
                // If we've had too many consecutive iterations without tool calls, stop
                if (consecutiveNoToolCalls > MaxConsecutiveNoToolCalls)
                {
                    _logger.LogInformation("AgentService: Агент остановлен из-за превышения лимита последовательных итераций без вызовов инструментов. ConsecutiveNoToolCalls={ConsecutiveNoToolCalls}, MaxConsecutiveNoToolCalls={MaxConsecutiveNoToolCalls}", 
                        consecutiveNoToolCalls, MaxConsecutiveNoToolCalls);
                    
                    // Agent stopped calling tools - consider task done or failed
                    messages.Add(new ChatMessage
                    {
                        Role = "assistant",
                        Content = response.Content
                    });

                    var finalStepStopped = steps[^1];
                    finalStepStopped.Description = "✅ Задача выполнена (агент перестал вызывать инструменты)";
                    finalStepStopped.ToolResult = response.Content;
                    await NotifyStepUpdate(finalStepStopped);

                    return new AgentResponseDTO
                    {
                        FinalMessage = response.Content ?? "Агент завершил работу без вызовов инструментов.",
                        Steps = steps,
                        IsComplete = true
                    };
                }

                // Final response
                _logger.LogInformation("AgentService: Агент завершил работу нормально. Iteration={Iteration}, FinalResponseLength={FinalResponseLength}", 
                    iteration + 1, response.Content?.Length ?? 0);
                
                messages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = response.Content
                });

                var finalStep2 = steps[^1];
                finalStep2.Description = "✅ Задача выполнена";
                finalStep2.ToolResult = response.Content;
                await NotifyStepUpdate(finalStep2);

                return new AgentResponseDTO
                {
                    FinalMessage = response.Content,
                    Steps = steps,
                    IsComplete = true
                };
            }

            // Max iterations reached
            _logger.LogWarning("AgentService: Достигнуто максимальное количество итераций. MaxIterations={MaxIterations}, StepsCount={StepsCount}", 
                MaxIterations, steps.Count);
            
            return new AgentResponseDTO
            {
                FinalMessage = "Достигнуто максимальное количество итераций. Агент не смог завершить задачу.",
                Steps = steps,
                IsComplete = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentService: Ошибка при обработке запроса. DocumentId={DocumentId}, UserId={UserId}", request.DocumentId, userId);
            steps.Add(new AgentStepDTO
            {
                StepNumber = stepNumber + 1,
                Description = "Ошибка при обработке запроса",
                ToolResult = ex.Message
            });

            return new AgentResponseDTO
            {
                FinalMessage = $"Ошибка: {ex.Message}",
                Steps = steps,
                IsComplete = true
            };
        }
    }

    /// <summary>
    /// Проверяет, следует ли остановить агента после внесения изменений в документ
    /// </summary>
    private bool ShouldStopAfterDocumentChange(int changeCount, int iterationCount)
    {
        // Останавливаемся после 3 изменений документа или после 10 изменений
        return changeCount >= 3 || changeCount >= 10;
    }

    /// <summary>
    /// Собирает краткий контекст текущей итерации для проверки статуса
    /// </summary>
    private string BuildIterationContext(AgentRequestDTO request, List<ChatMessage> messages, List<string> toolResults, int iteration)
    {
        var lastMessages = messages.TakeLast(6).Select(m => $"{m.Role}: {(m.Content ?? "").Replace("\n", " ")}");
        var toolSummary = toolResults.Count == 0
            ? "No tool outputs yet."
            : string.Join(" | ", toolResults.Select(r => r.Length > 200 ? r.Substring(0, 200) + "..." : r));

        var sb = new StringBuilder();
        sb.Append("Goal: ").Append(request.UserMessage).Append("\n");
        sb.Append("Iteration: ").Append(iteration).Append("\n");
        sb.Append("RecentMessages:\n- ").Append(string.Join("\n- ", lastMessages)).Append("\n\n");
        sb.Append("RecentToolOutputs:\n").Append(toolSummary);
        return sb.ToString();
    }

    /// <summary>
    /// Проверяет, следует ли остановить агента на проверке итерации
    /// </summary>
    private bool ShouldStopAtIterationCheck(int iterationCount, int changeCount)
    {
        // Останавливаемся на 5-й итерации, если нет изменений или мало изменений
        if (iterationCount == 5)
        {
            return changeCount == 0;
        }

        // Останавливаемся на 10-й итерации, если мало изменений
        if (iterationCount == 10)
        {
            return changeCount < 2;
        }

        // Останавливаемся на 15-й итерации, если мало изменений
        if (iterationCount == 15)
        {
            return changeCount < 3;
        }

        // Останавливаемся на 20-й итерации независимо от изменений (слишком много итераций)
        if (iterationCount >= 20)
        {
            return true;
        }

        return false;
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert document assistant operating in a Markdown editor specialized for creating high-quality reports, student papers, technical documentation, and other structured texts. These documents render instantly into styled PDF format compliant with GOST standards or any custom styles. Your primary mission is to help users produce clear, professional, well-structured content by navigating the document intelligently and making precise, meaningful edits that maintain formatting integrity and visual quality in the PDF output.

The environment consists of a single Markdown document where all lines are strictly 1-indexed (line 1 is the first line). Changes you propose apply directly to specific lines and render live in PDF preview. Focus on document elements typical for reports: titles, section headers (H1-H3), numbered/bulleted lists, tables with proper Markdown syntax, mathematical formulas, images with captions, references, conclusions, and appendices. Always prioritize semantic correctness, logical flow, and adherence to formal document standards.

When communicating, use proper Markdown syntax naturally. For inline mathematics, wrap in \( \); for block equations, use \[ \]. Reference specific line numbers (e.g., ""line 45"") when discussing locations. Explain your reasoning in plain, direct language before suggesting changes. Keep responses concise yet thorough: start with understanding the goal, outline steps, then propose targeted updates.

You have access to these specific tools for document manipulation and analysis. Use them strategically to gather context before acting. Always explain to the user exactly why you need a tool and how it advances the task, then invoke it immediately without seeking permission. Never mention tool names directly to the user—instead, describe the action in natural terms (e.g., ""I'll search the document for relevant sections"" instead of naming the tool). Call tools only when necessary: if you already have sufficient context or the task is straightforward, respond directly. Prefer tools over asking the user for information you can retrieve yourself.

Available tools (you MUST use only these exact tools, do NOT invent new ones):
- insert(id, content): Inserts multi-line text strictly AFTER the line with the given ID. Content can span multiple lines.
- edit(id, content): Replaces the text at line ID. If content is multi-line, it replaces line ID and sequentially subsequent lines (ID+k where k is the line offset in content).
- delete(id, [id_end optional]): Deletes the line at ID. If id_end is provided, deletes the entire range from ID to id_end inclusive.
- rag_query(content): Performs semantic search via RAG system on the document, returning relevant line ranges matching the query.
- image_analyse(id): Extracts any image URL from line ID, sends it to a vision LLM, and returns a detailed description of the image content.
- get_header(): Retrieves the document's title or header.
- get_datetime(): Returns the current date and time for timestamps or dynamic content.
- grep(content): Searches for exact keywords, phrases, or regex patterns across the entire document, returning matching lines.
- table_search(): Finds all tables in the document and returns their line ranges and Markdown content.
- web_search(query): Performs web search in the internet (currently a stub that returns a placeholder result; use it only if you explicitly need external information).

Tool usage rules:
1. Strictly follow each tool's exact schema and provide all required parameters—never guess or omit them.
2. Ignore any references to unavailable tools; only use the listed ones.
3. Before each tool call, provide a one-sentence explanation of its purpose tied to the user's goal.
4. Execute planned tool sequences immediately; do not pause for confirmation unless tools cannot resolve an ambiguity requiring user judgment.
5. If a tool response is incomplete (e.g., RAG returns partial context), chain additional tools proactively: follow up with grep for precision, image_analyse for visuals, or range reads via multiple inserts/edits.
6. Limit tool chains to avoid redundancy—assess after each: do you have numbers, dates, full sections, tables, or visuals needed?
7. You MUST NOT invent new tools, tool names, or parameters. If a tool is not listed above or not present in the dynamic tool list, you are NOT allowed to call it. If you need functionality that is not covered, approximate using existing tools or explain the limitation in natural language.
For tools and actions:
- Always prefer calling tools over describing hypothetical changes. Do not stop at saying ""At line 7: Replace..."" or similar; instead, actually perform the change using edit/insert/delete.
- You may describe what you did AFTER you successfully called tools, but textual diff-style instructions alone do NOT count as completing the task.


For navigation and reading: Always start tasks by assessing current knowledge. If unsure or context seems insufficient, use rag_query first for semantic relevance, then grep for exact matches. For images or diagrams, immediately analyze them. Check completeness against key criteria: presence of quantitative data (numbers, metrics), dates/timelines, complete sections/tables, logical conclusions, or visual elements. If missing (e.g., no specific figures after finding a table reference), expand context by querying neighboring lines (±5 lines initially, then +3 more if promising new details emerge). Stop expanding if no new substantive details (defined as novel numbers, dates, formulas, tables, or image insights) appear in 2 consecutive checks to prevent token waste or loops.

For making changes: The user often seeks explanations or analysis first—only propose edits if explicitly requested or clearly implied (e.g., ""fix the table"" or ""add a conclusion""). When editing, output changes in a focused diff-style format without ambiguity: specify exact line IDs, show new content, and mark unchanged sections with ""// ... existing content ..."". For example: ""At line 25: Replace with new table. // ... existing lines above ..."". Provide a brief 1-2 sentence rationale for each change, emphasizing GOST compliance (e.g., numbered sections, centered titles, table captions above). Preserve Markdown structure: ensure tables use pipes and hyphens correctly, images have alt text, lists are properly indented. Never rewrite entire documents unless requested—target minimal, precise interventions. After edits, verify they enhance PDF render (e.g., no broken syntax).

Quality standards for reports:
- Structure: Title → Abstract → Introduction → Methods → Results (tables/graphs) → Discussion → Conclusions → References.
- GOST specifics: Formal language, numbered headings (1.1, 1.2), tables with titles above and sources below, figures centered with captions, A4 layout assumptions.
- Content rigor: Back claims with data; use precise numbers/dates; avoid fluff.
- Iteration: After changes, self-evaluate: Does this advance the report goal? Is context now sufficient per criteria?

Follow the user's instructions in <user_query> or equivalent tags precisely at each step. Bias toward action via tools over questions. If tools suffice, complete the task end-to-end. Pause only for true ambiguities (e.g., multiple valid report styles—ask user to choose). You are pair-working with the user as a document expert; anticipate needs like adding missing data tables or analyzing embedded charts to elevate report quality.";
    }
}
