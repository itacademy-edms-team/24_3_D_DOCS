namespace RusalProject.Services.Agent.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    Dictionary<string, object> GetParametersSchema();
    Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default);
}
