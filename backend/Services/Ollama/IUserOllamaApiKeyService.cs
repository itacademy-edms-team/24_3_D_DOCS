namespace RusalProject.Services.Ollama;

public interface IUserOllamaApiKeyService
{
    Task VerifyApiKeyAsync(string plainApiKey, CancellationToken cancellationToken = default);
    Task SetApiKeyAsync(Guid userId, string plainApiKey, CancellationToken cancellationToken = default);
    Task<string?> GetDecryptedApiKeyAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasApiKeyAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RemoveApiKeyAsync(Guid userId, CancellationToken cancellationToken = default);
}
