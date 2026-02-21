namespace RusalProject.Services.Agent.Tools.DocumentTools;

public interface IDocumentAgentTool
{
    string Name { get; }
    string Description { get; }
    Dictionary<string, object> GetParametersSchema();
    Task<DocumentToolResult> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default);
}
