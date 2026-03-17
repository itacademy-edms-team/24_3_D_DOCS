using System.Linq;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Agent.Tools.CRUDdocTools;
using RusalProject.Services.Agent.Tools.DocumentTools;
using RusalProject.Services.Chat;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

public class MainAgent : IMainAgent
{
    private readonly IChatService _chatService;
    private readonly IAgentAttachmentContextService _attachmentContext;
    private readonly AgentLoopRunner _runner;
    private readonly IDocumentAgent _documentAgent;
    private readonly IReadOnlyList<IAgentTool> _tools;

    private const int MaxToolIterations = 8;

    public MainAgent(
        IChatService chatService,
        IAgentAttachmentContextService attachmentContext,
        AgentLoopRunner runner,
        IDocumentAgent documentAgent,
        ListDocumentTool listDocumentsTool,
        CreateDocumentTool createDocumentTool,
        DeleteDocumentTool deleteDocumentTool,
        RenameDocumentTool renameDocumentTool,
        DelegateToDocumentAgentTool delegateToDocumentAgentTool,
        QueryAttachmentTextTool queryAttachmentTextTool,
        QueryAttachmentImageTool queryAttachmentImageTool)
    {
        _chatService = chatService;
        _attachmentContext = attachmentContext;
        _runner = runner;
        _documentAgent = documentAgent;
        _tools = new IAgentTool[]
        {
            listDocumentsTool,
            createDocumentTool,
            deleteDocumentTool,
            renameDocumentTool,
            delegateToDocumentAgentTool,
            queryAttachmentTextTool,
            queryAttachmentImageTool
        };
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

        var messages = chat.Messages.OrderBy(x => x.CreatedAt).ToList();
        var sourceSessionIdForContext = await _attachmentContext.ResolveAndInjectCatalogAsync(
            userId,
            request.ChatId.Value,
            AgentAttachmentContextScope.Global,
            documentId: null,
            request.SourceSessionId,
            messages,
            history,
            cancellationToken);

        var loopResult = await _runner.RunAsync(
            new AgentExecutionContext
            {
                UserId = userId,
                ChatSessionId = request.ChatId.Value,
                SourceSessionId = sourceSessionIdForContext,
                DocumentId = null
            },
            GetSystemPrompt(),
            _tools,
            history,
            MaxToolIterations,
            onStepUpdate,
            onChunk,
            cancellationToken);

        if (loopResult.Delegation != null)
        {
            var delegatedRequest = new AgentRequestDTO
            {
                ChatId = request.ChatId,
                Scope = Models.Types.ChatScope.Document,
                DocumentId = loopResult.Delegation.DocumentId,
                UserMessage = BuildDocumentAgentTask(loopResult.Delegation.Task),
                SourceSessionId = sourceSessionIdForContext
            };

            var delegatedResponse = await _documentAgent.RunAsync(
                delegatedRequest,
                userId,
                onStepUpdate,
                null,
                onChunk,
                cancellationToken);

            return new AgentResponseDTO
            {
                FinalMessage = delegatedResponse.FinalMessage,
                Steps = loopResult.Steps.Concat(delegatedResponse.Steps).ToList(),
                IsComplete = delegatedResponse.IsComplete
            };
        }

        var finalMessage = string.IsNullOrWhiteSpace(loopResult.FinalMessage)
            ? "Готово."
            : loopResult.FinalMessage;

        await _chatService.AddMessageAsync(request.ChatId.Value, new ChatMessageDTO
        {
            Role = "assistant",
            Content = finalMessage
        }, userId);

        if (loopResult.Steps.Count > 0 && onChunk != null && !string.IsNullOrWhiteSpace(finalMessage))
        {
            await onChunk("\n\n");
            await onChunk(finalMessage);
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

Ты главный агент управления документами.

Ты умеешь:
- получать список документов;
- создавать документы;
- переименовывать документы;
- удалять документы;
- передавать задачу агенту документа для работы с содержимым;
- при наличии вложения в чате: query_attachment_text и query_attachment_image (см. каталог в сообщениях).

Правила:
- Если в сообщениях есть блок «[Контекст вложений…]», используй индексы частей; для вопросов к тексту вложения — query_attachment_text(part_index, question), к изображению — query_attachment_image(part_index, question).
- Никогда не редактируй содержимое документа сам.
- Если пользователь просит изменить текст, раздел, абзац, таблицу, заголовок или другой контент документа, используй delegate_to_document_agent.
- Если пользователь просит создать новый документ и сразу наполнить его содержимым, сначала создай документ через create_document, а затем в этом же ответе обязательно вызови delegate_to_document_agent для наполнения документа.
- Не останавливайся после create_document, если запрос пользователя подразумевает содержимое документа, план, отчёт, статью, описание или другой текст внутри файла.
- Если для делегации непонятно, какой документ нужен, сначала используй list_documents и выбери документ по названию.
- Если пользователь просто общается или задаёт вопрос без действия, отвечай коротко без вызова инструментов.
- В итоговом ответе не показывай пользователю внутренние id документов и сырые tool-результаты.
- Когда задача сделана, дай короткий итоговый ответ.
""";
    }

    private static string BuildDocumentAgentTask(string task)
    {
        var normalizedTask = task.Trim();
        return $"""
Выполни задачу через инструменты редактирования документа, а не обычным ответом в чат.
Нужно изменить содержимое текущего документа.
Оформляй вставляемый и заменяемый текст по правилам markdown редактора (см. системные инструкции агента документа).
Если документ пустой и требуется написать новый текст, используй propose_insert со start_line = 0.
Если для точной правки нужен контекст, сначала используй read_document, затем propose_insert, propose_replace или propose_delete.

Задача:
{normalizedTask}
""";
    }
}
