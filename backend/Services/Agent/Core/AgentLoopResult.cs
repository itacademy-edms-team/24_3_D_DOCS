using RusalProject.Models.DTOs.Agent;

namespace RusalProject.Services.Agent.Core;

public sealed class AgentLoopResult
{
    public string FinalMessage { get; init; } = string.Empty;
    public List<AgentStepDTO> Steps { get; init; } = new();
    public AgentDelegationRequest? Delegation { get; init; }
}
