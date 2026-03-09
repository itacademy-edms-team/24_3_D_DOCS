using System.Linq;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Agent.Tools.DocumentTools;
using RusalProject.Services.AgentSources;
using RusalProject.Services.Chat;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

public class DocumentAgent : IDocumentAgent
{
    private const int MaxToolIterations = 16;

    private readonly IChatService _chatService;
    private readonly IAgentSourceService _agentSourceService;
    private readonly AgentLoopRunner _runner;
    private readonly IReadOnlyList<IAgentTool> _tools;

    public DocumentAgent(
        IChatService chatService,
        IAgentSourceService agentSourceService,
        AgentLoopRunner runner,
        ReadDocumentTool readDocumentTool,
        ProposeInsertTool proposeInsertTool,
        ProposeDeleteTool proposeDeleteTool,
        ProposeReplaceTool proposeReplaceTool,
        QueryAttachmentTextTool queryAttachmentTextTool,
        QueryAttachmentImageTool queryAttachmentImageTool)
    {
        _chatService = chatService;
        _agentSourceService = agentSourceService;
        _runner = runner;
        _tools = new IAgentTool[]
        {
            readDocumentTool,
            proposeInsertTool,
            proposeDeleteTool,
            proposeReplaceTool,
            queryAttachmentTextTool,
            queryAttachmentImageTool
        };
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
        if (!request.DocumentId.HasValue)
            throw new InvalidOperationException("DocumentId обязателен для Document Agent.");

        var chat = await _chatService.GetChatByIdAsync(request.ChatId.Value, userId)
            ?? throw new InvalidOperationException("Чат не найден.");

        var history = chat.Messages
            .OrderBy(x => x.CreatedAt)
            .Where(m => !m.StepNumber.HasValue && m.Role is "user" or "assistant" or "system")
            .Select(m => new OllamaMessageInput
            {
                Role = m.Role,
                Content = m.Content
            })
            .ToList();

        var lastUserMessage = history.LastOrDefault(m => m.Role == "user")?.Content?.Trim();
        var currentUserMessage = request.UserMessage?.Trim();
        if (!string.IsNullOrWhiteSpace(currentUserMessage) &&
            !string.Equals(lastUserMessage, currentUserMessage, StringComparison.Ordinal))
        {
            history.Add(new OllamaMessageInput
            {
                Role = "user",
                Content = currentUserMessage
            });
        }

        Guid? sourceSessionIdForContext = null;
        if (request.SourceSessionId.HasValue)
        {
            var sourceSession = await _agentSourceService.GetValidatedSessionAsync(
                userId,
                request.SourceSessionId.Value,
                request.DocumentId.Value,
                request.ChatId.Value,
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "Сессия вложения недействительна, истекла или не относится к этому чату и документу.");

            sourceSessionIdForContext = sourceSession.Id;
            var catalog = _agentSourceService.BuildCatalog(sourceSession);
            var lastUserIdx = history.FindLastIndex(m => m.Role == "user");
            if (lastUserIdx >= 0)
            {
                history.Insert(lastUserIdx, new OllamaMessageInput
                {
                    Role = "user",
                    Content = catalog
                });
            }
            else
            {
                history.Add(new OllamaMessageInput { Role = "user", Content = catalog });
            }
        }

        var loopResult = await _runner.RunAsync(
            new AgentExecutionContext
            {
                UserId = userId,
                DocumentId = request.DocumentId.Value,
                ChatSessionId = request.ChatId.Value,
                SourceSessionId = sourceSessionIdForContext
            },
            GetSystemPrompt(),
            _tools,
            history,
            MaxToolIterations,
            onStepUpdate,
            onStatusCheck,
            cancellationToken);

        var finalMessage = loopResult.FinalMessage;
        if (string.IsNullOrWhiteSpace(finalMessage))
        {
            finalMessage = loopResult.Steps.Any(step => step.DocumentChanges is { Count: > 0 })
                ? "Готово! Изменения предложены. Проверьте их и примите или отклоните."
                : "Готово.";
        }

        await _chatService.AddMessageAsync(request.ChatId.Value, new ChatMessageDTO
        {
            Role = "assistant",
            Content = finalMessage
        }, userId);

        if (loopResult.Steps.Count > 0 && onStatusCheck != null && !string.IsNullOrWhiteSpace(finalMessage))
        {
            await onStatusCheck("\n\n");
            await onStatusCheck(finalMessage);
        }

        return new AgentResponseDTO
        {
            FinalMessage = finalMessage,
            Steps = loopResult.Steps,
            IsComplete = true
        };
    }

    private static string GetSystemPrompt()
    {
        return """
IMPORTANT: Отвечай пользователю на русском языке.

Ты агент одного markdown-документа.

Ты умеешь:
- читать документ по диапазону строк;
- предлагать вставку нового контента;
- предлагать удаление диапазона строк;
- предлагать замену диапазона строк;
- при наличии вложения в чате: query_attachment_text (вопрос по текстовой части) и query_attachment_image (вопрос по картинке из вложения).

Правила:
- Если в сообщениях есть блок «[Контекст вложений…]», сначала ориентируйся на индексы частей; для вопросов к тексту вложения вызывай query_attachment_text(part_index, question), для изображений — query_attachment_image(part_index, question).
- Ты работаешь только с содержимым текущего документа.
- Не создавай и не удаляй документы.
- Если контекст документа непонятен, сначала используй read_document.
- Не применяй изменения окончательно: только предлагай их через propose_insert, propose_delete и propose_replace.
- Если пользователь просит написать, добавить, заполнить, составить, создать внутри документа отчёт, план, статью, раздел, список или любой новый контент, ты обязан изменить документ через инструменты, а не отвечать только сообщением в чат.
- Если документ пустой и нужно создать содержимое с нуля, сразу используй propose_insert со start_line = 0.
- Если задача явно требует правки документа, ответ без вызова инструмента недопустим.
- Для больших изменений разбивай задачу по сущностям документа, а не одним огромным блоком.
- Если пользователь просто здоровается или задаёт вопрос без необходимости менять документ, ответь коротко без вызова инструментов.
- Когда задача закончена, дай короткий итоговый ответ.
""";
    }
}
