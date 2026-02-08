using RusalProject.Models.DTOs.Agent;

namespace RusalProject.Services.Agent;

public interface IDocumentAgent
{
    Task<AgentResponseDTO> RunAsync(
        AgentRequestDTO request,
        Guid userId,
        Func<AgentStepDTO, Task>? onStepUpdate = null,
        Func<int, Task>? onDocumentChange = null,
        Func<string, Task>? onStatusCheck = null,
        CancellationToken cancellationToken = default);
}
