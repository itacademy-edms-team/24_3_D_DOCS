using RusalProject.Models.DTOs.Agent;

namespace RusalProject.Services.Agent;

public interface IMainAgent
{
    Task<AgentResponseDTO> RunAsync(
        AgentRequestDTO request,
        Guid userId,
        Func<AgentStepDTO, Task>? onStepUpdate = null,
        Func<string, Task>? onChunk = null,
        CancellationToken cancellationToken = default);
}
