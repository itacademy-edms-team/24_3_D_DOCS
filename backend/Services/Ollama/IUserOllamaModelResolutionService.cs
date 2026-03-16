namespace RusalProject.Services.Ollama;

public interface IUserOllamaModelResolutionService
{
    Task<string> GetAgentModelAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<string> GetAttachmentTextModelAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<string> GetVisionModelAsync(Guid userId, CancellationToken cancellationToken = default);
}
