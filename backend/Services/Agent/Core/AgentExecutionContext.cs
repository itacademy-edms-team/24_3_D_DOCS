namespace RusalProject.Services.Agent.Core;

public sealed class AgentExecutionContext
{
    public Guid UserId { get; init; }
    public Guid? DocumentId { get; init; }
    public Guid? ChatSessionId { get; init; }
    public Guid? SourceSessionId { get; init; }
}
