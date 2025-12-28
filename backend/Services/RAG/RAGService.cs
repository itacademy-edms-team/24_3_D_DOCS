using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.RAG;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;
using RusalProject.Services.Ollama;
using RusalProject.Services.Documents;

namespace RusalProject.Services.RAG;

public class RAGService : IRAGService
{
    private readonly ApplicationDbContext _context;
    private readonly IOllamaService _ollamaService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<RAGService> _logger;

    public RAGService(
        ApplicationDbContext context,
        IOllamaService ollamaService,
        IDocumentService documentService,
        ILogger<RAGService> logger)
    {
        _context = context;
        _ollamaService = ollamaService;
        _documentService = documentService;
        _logger = logger;
    }

    public async Task<List<RAGSearchResult>> SearchAsync(Guid documentId, Guid userId, string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем, что документ принадлежит пользователю
            var documentExists = await _documentService.DocumentExistsAsync(documentId, userId);
            if (!documentExists)
            {
                _logger.LogWarning("RAG search: Document {DocumentId} not found or access denied for user {UserId}", documentId, userId);
                throw new UnauthorizedAccessException("Document not found or access denied");
            }

            _logger.LogInformation("RAG search: generating query embedding (query length: {QueryLength})", query.Length);
            
            // Generate embedding for query
            var queryEmbedding = await _ollamaService.GenerateEmbeddingAsync(query, cancellationToken);

            _logger.LogInformation("RAG search: query embedding generated ({EmbeddingSize} dimensions)", queryEmbedding.Length);

            // Get all blocks with embeddings for the document
            var blocksWithEmbeddings = await _context.DocumentBlocks
                .Where(b => b.DocumentId == documentId && b.DeletedAt == null)
                .Include(b => b.Embedding)
                .Where(b => b.Embedding != null)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("RAG search: found {BlockCount} blocks with embeddings", blocksWithEmbeddings.Count);

            // Calculate cosine similarity for each block
            var results = blocksWithEmbeddings
                .Select(b => new
                {
                    Block = b,
                    Score = CosineSimilarity(queryEmbedding, b.Embedding!.Embedding)
                })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => new RAGSearchResult
                {
                    BlockId = x.Block.Id,
                    BlockType = x.Block.BlockType,
                    StartLine = x.Block.StartLine,
                    EndLine = x.Block.EndLine,
                    RawText = x.Block.RawText,
                    NormalizedText = x.Block.NormalizedText,
                    Score = x.Score
                })
                .ToList();

            _logger.LogInformation("RAG search: returning {ResultCount} results (top score: {TopScore:F4})", 
                results.Count, results.FirstOrDefault()?.Score ?? 0.0);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing RAG search for document {DocumentId}", documentId);
            throw;
        }
    }

    private double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0.0 || normB == 0.0)
            return 0.0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    public async Task<List<RAGSearchResult>> SearchTablesAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем, что документ принадлежит пользователю
            var documentExists = await _documentService.DocumentExistsAsync(documentId, userId);
            if (!documentExists)
            {
                _logger.LogWarning("RAG search tables: Document {DocumentId} not found or access denied for user {UserId}", documentId, userId);
                throw new UnauthorizedAccessException("Document not found or access denied");
            }

            var tables = await _context.DocumentBlocks
                .Where(b => b.DocumentId == documentId 
                    && b.BlockType == "TableRow" 
                    && b.DeletedAt == null)
                .OrderBy(b => b.StartLine)
                .Select(b => new RAGSearchResult
                {
                    BlockId = b.Id,
                    BlockType = b.BlockType,
                    StartLine = b.StartLine,
                    EndLine = b.EndLine,
                    RawText = b.RawText,
                    NormalizedText = b.NormalizedText,
                    Score = 1.0 // All tables get same score for now
                })
                .ToListAsync(cancellationToken);

            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching tables for document {DocumentId}", documentId);
            throw;
        }
    }
}
