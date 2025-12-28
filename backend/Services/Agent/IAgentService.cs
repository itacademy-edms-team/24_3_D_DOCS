using RusalProject.Models.DTOs.Agent;

namespace RusalProject.Services.Agent;

public interface IAgentService
{
    Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, CancellationToken cancellationToken = default);
    Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, CancellationToken cancellationToken = default);
    Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, Func<int, Task>? onDocumentChange, CancellationToken cancellationToken = default);
    Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, Func<int, Task>? onDocumentChange, Func<string, Task>? onStatusCheck, CancellationToken cancellationToken = default);
}
