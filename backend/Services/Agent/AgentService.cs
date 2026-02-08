using RusalProject.Models.DTOs.Agent;

namespace RusalProject.Services.Agent;

public class AgentService : IAgentService
{
    private readonly IDocumentAgent _documentAgent;

    public AgentService(IDocumentAgent documentAgent)
    {
        _documentAgent = documentAgent;
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _documentAgent.RunAsync(request, userId, null, null, null, cancellationToken);
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, Func<int, Task>? onDocumentChange, CancellationToken cancellationToken = default)
    {
        return await _documentAgent.RunAsync(request, userId, onStepUpdate, onDocumentChange, null, cancellationToken);
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, Func<int, Task>? onDocumentChange, Func<string, Task>? onStatusCheck, CancellationToken cancellationToken = default)
    {
        return await _documentAgent.RunAsync(request, userId, onStepUpdate, onDocumentChange, onStatusCheck, cancellationToken);
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, CancellationToken cancellationToken = default)
    {
        return await _documentAgent.RunAsync(request, userId, onStepUpdate, null, null, cancellationToken);
    }
}
