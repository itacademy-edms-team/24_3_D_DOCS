using RusalProject.Models.DTOs.Agent;
using RusalProject.Services.Chat;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

/// <summary>
/// Document assistant agent. Uses Ollama Cloud API for chat.
/// </summary>
public class DocumentAgent : IDocumentAgent
{
    private readonly IChatService _chatService;
    private readonly IAgentLogService _logService;
    private readonly IOllamaChatService _ollamaChatService;
    private readonly ILogger<DocumentAgent> _logger;

    public DocumentAgent(
        IChatService chatService,
        IAgentLogService logService,
        IOllamaChatService ollamaChatService,
        ILogger<DocumentAgent> logger)
    {
        _chatService = chatService;
        _logService = logService;
        _ollamaChatService = ollamaChatService;
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
        {
            throw new InvalidOperationException("ChatId обязателен.");
        }

        var documentId = request.DocumentId ?? throw new InvalidOperationException("DocumentId обязателен для Document Agent.");
        _logger.LogInformation("DocumentAgent: Running. DocumentId={DocumentId}, ChatId={ChatId}, UserId={UserId}", documentId, request.ChatId, userId);

        await _logService.LogUserMessageAsync(documentId, userId, request.ChatId, request.UserMessage, 0, cancellationToken);

        try
        {
            var result = await _ollamaChatService.ChatAsync(
                userId,
                request.ChatId.Value,
                request.UserMessage,
                request.ClientMessageId,
                documentId,
                onChunk: onStatusCheck,
                onStatusCheck: null,
                cancellationToken: cancellationToken);

            return new AgentResponseDTO
            {
                FinalMessage = result.FinalMessage,
                Steps = new List<AgentStepDTO>(),
                IsComplete = result.IsComplete
            };
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocumentAgent error");
            throw;
        }
    }
}
