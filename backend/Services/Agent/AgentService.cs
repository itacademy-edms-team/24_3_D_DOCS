using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.Types;

namespace RusalProject.Services.Agent;

public class AgentService : IAgentService
{
    private readonly IDocumentAgent _documentAgent;
    private readonly IMainAgent _mainAgent;

    public AgentService(IDocumentAgent documentAgent, IMainAgent mainAgent)
    {
        _documentAgent = documentAgent;
        _mainAgent = mainAgent;
    }

    private bool UseMainAgent(AgentRequestDTO request) =>
        request.Scope == ChatScope.Global;

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, CancellationToken cancellationToken = default)
    {
        return await ProcessAsync(request, userId, null, null, null, cancellationToken);
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, Func<int, Task>? onDocumentChange, CancellationToken cancellationToken = default)
    {
        return await ProcessAsync(request, userId, onStepUpdate, onDocumentChange, null, cancellationToken);
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, Func<int, Task>? onDocumentChange, Func<string, Task>? onStatusCheck, CancellationToken cancellationToken = default)
    {
        if (UseMainAgent(request))
            return await _mainAgent.RunAsync(request, userId, onStepUpdate, onStatusCheck, cancellationToken);

        return await _documentAgent.RunAsync(request, userId, onStepUpdate, onDocumentChange, onStatusCheck, cancellationToken);
    }

    public async Task<AgentResponseDTO> ProcessAsync(AgentRequestDTO request, Guid userId, Func<AgentStepDTO, Task>? onStepUpdate, CancellationToken cancellationToken = default)
    {
        return await ProcessAsync(request, userId, onStepUpdate, null, null, cancellationToken);
    }
}
