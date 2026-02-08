using RusalProject.Models.DTOs.Agent;
using RusalProject.Services.Chat;

namespace RusalProject.Services.Agent;

/// <summary>
/// Document assistant agent. Currently returns a stub response.
/// Will be connected via Ollama Cloud API later with Microsoft Agent Framework.
/// </summary>
public class DocumentAgent : IDocumentAgent
{
    private readonly IChatService _chatService;
    private readonly IAgentLogService _logService;
    private readonly ILogger<DocumentAgent> _logger;

    private const string StubMessage = "AI Помощник пока не настроен. Подключение через Ollama Cloud API будет добавлено позже.";

    public DocumentAgent(
        IChatService chatService,
        IAgentLogService logService,
        ILogger<DocumentAgent> logger)
    {
        _chatService = chatService;
        _logService = logService;
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
        _logger.LogInformation("DocumentAgent: Stub run. DocumentId={DocumentId}, UserId={UserId}", request.DocumentId, userId);

        await _logService.LogUserMessageAsync(request.DocumentId, userId, request.ChatId, request.UserMessage, 0, cancellationToken);

        if (onStatusCheck != null)
        {
            await onStatusCheck("DONE: " + StubMessage);
        }

        return new AgentResponseDTO
        {
            FinalMessage = StubMessage,
            Steps = new List<AgentStepDTO>(),
            IsComplete = true
        };
    }
}
