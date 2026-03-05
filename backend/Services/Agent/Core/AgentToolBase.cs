using System.Text.Json;

namespace RusalProject.Services.Agent.Core;

public abstract class AgentToolBase<TArgs> : IAgentTool
    where TArgs : new()
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract object ParametersSchema { get; }

    public async Task<AgentToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        TArgs typedArgs;

        if (arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            typedArgs = new TArgs();
        }
        else
        {
            typedArgs = arguments.Deserialize<TArgs>(JsonOptions) ?? new TArgs();
        }

        return await ExecuteTypedAsync(typedArgs, context, cancellationToken);
    }

    protected abstract Task<AgentToolExecutionResult> ExecuteTypedAsync(
        TArgs arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken);
}
