namespace RusalProject.Services.Agent.Core;

public sealed class AgentDelegationRequest
{
    public Guid DocumentId { get; init; }
    public string Task { get; init; } = string.Empty;
}
