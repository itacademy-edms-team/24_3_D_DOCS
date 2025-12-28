using RusalProject.Models.DTOs.RAG;

namespace RusalProject.Services.RAG;

public interface IRAGService
{
    /// <summary>
    /// Поиск по документу с использованием векторного поиска
    /// </summary>
    Task<List<RAGSearchResult>> SearchAsync(Guid documentId, Guid userId, string query, int topK = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Специализированный поиск таблиц в документе
    /// </summary>
    Task<List<RAGSearchResult>> SearchTablesAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
}
