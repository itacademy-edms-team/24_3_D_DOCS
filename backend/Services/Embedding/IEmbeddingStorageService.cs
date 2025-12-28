using RusalProject.Models.DTOs.Document;

namespace RusalProject.Services.Embedding;

public interface IEmbeddingStorageService
{
    /// <summary>
    /// Инкрементально обновляет эмбеддинги для документа
    /// </summary>
    Task UpdateEmbeddingsAsync(Guid documentId, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает статус покрытия эмбеддингами для документа
    /// </summary>
    Task<EmbeddingStatusDTO> GetEmbeddingStatusAsync(Guid documentId, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет эмбеддинги для документа (загружает контент самостоятельно)
    /// </summary>
    Task UpdateEmbeddingsForDocumentAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
}
