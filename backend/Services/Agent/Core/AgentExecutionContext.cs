namespace RusalProject.Services.Agent.Core;

public sealed class AgentExecutionContext
{
    public Guid UserId { get; init; }
    public Guid? DocumentId { get; init; }
}
