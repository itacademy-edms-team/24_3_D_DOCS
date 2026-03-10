using Microsoft.AspNetCore.Http;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.Entities;

namespace RusalProject.Services.AgentSources;

public interface IAgentSourceService
{
    Task<AgentSourceIngestResponseDTO> IngestAsync(
        Guid userId,
        Guid? documentId,
        Guid chatSessionId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<AgentSourceSession?> GetValidatedSessionAsync(
        Guid userId,
        Guid sessionId,
        Guid chatSessionId,
        Guid? documentIdForContext,
        CancellationToken cancellationToken = default);

    string BuildCatalog(AgentSourceSession session, string? notes = null);

    Task<string> LoadPartTextAsync(AgentSourcePart part, Guid userId, CancellationToken cancellationToken = default);

    Task<byte[]> LoadPartImageAsync(AgentSourcePart part, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Поток оригинального файла из MinIO; null если сессия не найдена, истекла или файл не сохранён.
    /// </summary>
    Task<(Stream Stream, string FileName, string ContentType)?> GetOriginalFileAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// JSON для поля chat_messages.attachments_json; null если сессия не прошла проверку.
    /// </summary>
    Task<string?> BuildAttachmentsJsonAsync(
        Guid userId,
        Guid chatSessionId,
        Guid? requestDocumentId,
        Guid sourceSessionId,
        CancellationToken cancellationToken = default);
}
