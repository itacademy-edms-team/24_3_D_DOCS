using RusalProject.Models.DTOs.AI;

namespace RusalProject.Services.Ollama;

public interface IUserOllamaModelSettingsService
{
    Task<IReadOnlyList<LlmModelOptionDTO>> GetCatalogAsync(CancellationToken cancellationToken = default);

    Task<OllamaModelPreferencesResponseDTO> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SavePreferencesAsync(Guid userId, SetOllamaModelPreferencesDTO dto, CancellationToken cancellationToken = default);
}
