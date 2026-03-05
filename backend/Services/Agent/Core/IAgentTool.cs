using System.Text.Json;

namespace RusalProject.Services.Agent.Core;

public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    object ParametersSchema { get; }
    Task<AgentToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);
}
