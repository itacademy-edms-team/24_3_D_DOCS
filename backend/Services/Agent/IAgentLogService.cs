using RusalProject.Models.DTOs.Agent;

namespace RusalProject.Services.Agent;

public interface IAgentLogService
{
    Task LogUserMessageAsync(Guid documentId, Guid userId, Guid? chatSessionId, string message, int iterationNumber, CancellationToken cancellationToken = default);
    Task LogLlmRequestAsync(Guid documentId, Guid userId, Guid? chatSessionId, string prompt, string? systemPrompt, int iterationNumber, int? stepNumber, CancellationToken cancellationToken = default);
    Task LogLlmResponseAsync(Guid documentId, Guid userId, Guid? chatSessionId, string response, int iterationNumber, int? stepNumber, CancellationToken cancellationToken = default);
    Task LogToolCallAsync(Guid documentId, Guid userId, Guid? chatSessionId, string toolName, Dictionary<string, object> arguments, int iterationNumber, int? stepNumber, CancellationToken cancellationToken = default);
    Task LogToolResultAsync(Guid documentId, Guid userId, Guid? chatSessionId, string toolName, string result, int iterationNumber, int? stepNumber, CancellationToken cancellationToken = default);
    Task LogDocumentChangeAsync(Guid documentId, Guid userId, Guid? chatSessionId, string toolName, string beforeContent, string afterContent, int iterationNumber, int? stepNumber, CancellationToken cancellationToken = default);
    Task LogStatusCheckAsync(Guid documentId, Guid userId, Guid? chatSessionId, string verdict, string reason, int iterationNumber, CancellationToken cancellationToken = default);
    Task LogCompletionAsync(Guid documentId, Guid userId, Guid? chatSessionId, string reason, int documentChangeCount, int iterationNumber, CancellationToken cancellationToken = default);
    Task<List<AgentLogDTO>> GetLogsAsync(Guid documentId, Guid userId, Guid? chatSessionId = null, CancellationToken cancellationToken = default);
}
