using Microsoft.AspNetCore.Http;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.Entities;

namespace RusalProject.Services.AgentSources;

public interface IAgentSourceService
{
    Task<AgentSourceIngestResponseDTO> IngestAsync(
        Guid userId,
        Guid documentId,
        Guid chatSessionId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<AgentSourceSession?> GetValidatedSessionAsync(
        Guid userId,
        Guid sessionId,
        Guid documentId,
        Guid chatSessionId,
        CancellationToken cancellationToken = default);

    string BuildCatalog(AgentSourceSession session, string? notes = null);

    Task<string> LoadPartTextAsync(AgentSourcePart part, Guid userId, CancellationToken cancellationToken = default);

    Task<byte[]> LoadPartImageAsync(AgentSourcePart part, Guid userId, CancellationToken cancellationToken = default);
}
