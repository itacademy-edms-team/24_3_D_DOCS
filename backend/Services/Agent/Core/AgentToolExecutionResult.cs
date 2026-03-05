using RusalProject.Models.DTOs.Agent;

namespace RusalProject.Services.Agent.Core;

public sealed class AgentToolExecutionResult
{
    public string ResultMessage { get; init; } = string.Empty;
    public List<DocumentEntityChangeDTO> DocumentChanges { get; init; } = new();
    public AgentDelegationRequest? Delegation { get; init; }
}
