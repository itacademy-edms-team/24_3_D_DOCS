using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.Document;
using RusalProject.Models.Entities;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;
using RusalProject.Services.Documents;
using RusalProject.Services.Markdown;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Embedding;

public class EmbeddingStorageService : IEmbeddingStorageService
{
    private readonly ApplicationDbContext _context;
    private readonly IMarkdownParserService _parserService;
    private readonly IOllamaService _ollamaService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<EmbeddingStorageService> _logger;
    private readonly string _embeddingModel;
    private const int MinTextLengthForEmbedding = 1; // Минимальная длина текста для создания эмбеддинга (1 = любой непустой текст)

    public EmbeddingStorageService(
        ApplicationDbContext context,
        IMarkdownParserService parserService,
        IOllamaService ollamaService,
        IDocumentService documentService,
        IConfiguration configuration,
        ILogger<EmbeddingStorageService> logger)
    {
        _context = context;
        _parserService = parserService;
        _ollamaService = ollamaService;
        _documentService = documentService;
        _logger = logger;
        _embeddingModel = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
    }

    public async Task UpdateEmbeddingsAsync(Guid documentId, string content, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
            _logger.LogInformation("🔍 UpdateEmbeddingsAsync: начало обработки документа {DocumentId} ({ContentLength} символов)", documentId, content.Length);
        try
        {
            // Parse document into blocks
            var parseStartTime = DateTime.UtcNow;
            List<ParsedBlock> parsedBlocks;
            double parseDuration;
            try
            {
                _logger.LogInformation("📝 Перед вызовом ParseDocument для документа {DocumentId}, длина контента: {ContentLength}", documentId, content?.Length ?? 0);
                _logger.LogInformation("📝 Перед вызовом ParseDocument для документа {DocumentId}, длина контента: {ContentLength}", documentId, content?.Length ?? 0);
                parsedBlocks = _parserService.ParseDocument(content);
                var parseEndTime = DateTime.UtcNow;
                parseDuration = (parseEndTime - parseStartTime).TotalMilliseconds;
                _logger.LogInformation("✅ Парсинг завершен: {BlockCount} блоков за {ParseDuration}ms", parsedBlocks.Count, parseDuration);
            }
            catch (Exception parseEx)
            {
                var parseEndTime = DateTime.UtcNow;
                parseDuration = (parseEndTime - parseStartTime).TotalMilliseconds;
                _logger.LogError(parseEx, "❌ Ошибка при парсинге документа {DocumentId} за {ParseDuration}ms: {Message}", documentId, parseDuration, parseEx.Message);
                throw;
            }

            // Get existing blocks from database
            var existingBlocks = await _context.DocumentBlocks
                .Include(b => b.Embedding)
                .Where(b => b.DocumentId == documentId && b.DeletedAt == null)
                .ToListAsync(cancellationToken);

            // Create dictionaries for quick lookup
            var existingBlocksByRange = existingBlocks.ToDictionary(
                b => (b.StartLine, b.EndLine),
                b => b
            );

            var processedRanges = new HashSet<(int, int)>();
            var updatedCount = 0;
            var createdCount = 0;
            var skippedCount = 0;
            var embedGenerationCount = 0;
            var embedGenerationTotalMs = 0.0;

            _logger.LogInformation("🔄 Начало обработки {BlockCount} блоков", parsedBlocks.Count);
            // Process each parsed block
            foreach (var parsedBlock in parsedBlocks)
            {
                var range = (parsedBlock.StartLine, parsedBlock.EndLine);
                processedRanges.Add(range);

                if (existingBlocksByRange.TryGetValue(range, out var existingBlock))
                {
                    // Block exists - check if content changed (инкрементальное обновление: сравниваем хеши)
                    if (existingBlock.ContentHash != parsedBlock.ContentHash)
                    {
                        // Content changed - update block and regenerate embedding
                        existingBlock.RawText = parsedBlock.RawText;
                        existingBlock.NormalizedText = parsedBlock.NormalizedText;
                        existingBlock.ContentHash = parsedBlock.ContentHash;
                        existingBlock.BlockType = parsedBlock.BlockType.ToString();
                        existingBlock.UpdatedAt = DateTime.UtcNow;

                        // Generate new embedding
                        var textForEmbedding = GetTextForEmbedding(parsedBlock.NormalizedText, parsedBlock.RawText);
                        var embedStartTime = DateTime.UtcNow;
                        embedGenerationCount++;
                        var embedding = await _ollamaService.GenerateEmbeddingAsync(
                            textForEmbedding,
                            cancellationToken
                        );
                        var embedEndTime = DateTime.UtcNow;
                        var embedDuration = (embedEndTime - embedStartTime).TotalMilliseconds;
                        embedGenerationTotalMs += embedDuration;

                        // Update or create embedding
                        if (existingBlock.Embedding != null)
                        {
                            existingBlock.Embedding.Embedding = embedding;
                            existingBlock.Embedding.Version++;
                            existingBlock.Embedding.CreatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            existingBlock.Embedding = new BlockEmbedding
                            {
                                BlockId = existingBlock.Id,
                                Embedding = embedding,
                                Model = _embeddingModel,
                                Version = 1
                            };
                            _context.BlockEmbeddings.Add(existingBlock.Embedding);
                        }

                        updatedCount++;
                    }
                    else
                    {
                        // Hash matches - no update needed (инкрементальное обновление: пропускаем неизменённые блоки)
                        skippedCount++;
                    }
                }
                else
                {
                    // New block - create it
                    var newBlock = new DocumentBlock
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = documentId,
                        BlockType = parsedBlock.BlockType.ToString(),
                        StartLine = parsedBlock.StartLine,
                        EndLine = parsedBlock.EndLine,
                        RawText = parsedBlock.RawText,
                        NormalizedText = parsedBlock.NormalizedText,
                        ContentHash = parsedBlock.ContentHash
                    };

                    _context.DocumentBlocks.Add(newBlock);

                    // Generate embedding for new block
                    var textForEmbedding = GetTextForEmbedding(parsedBlock.NormalizedText, parsedBlock.RawText);
                    var embedStartTime = DateTime.UtcNow;
                    embedGenerationCount++;
                    var embedding = await _ollamaService.GenerateEmbeddingAsync(
                        textForEmbedding,
                        cancellationToken
                    );
                    var embedEndTime = DateTime.UtcNow;
                    var embedDuration = (embedEndTime - embedStartTime).TotalMilliseconds;
                    embedGenerationTotalMs += embedDuration;

                    var blockEmbedding = new BlockEmbedding
                    {
                        BlockId = newBlock.Id,
                        Embedding = embedding,
                        Model = _embeddingModel,
                        Version = 1
                    };

                    _context.BlockEmbeddings.Add(blockEmbedding);

                    createdCount++;
                }
            }

            // Mark deleted blocks
            var deletedBlocks = existingBlocks
                .Where(b => !processedRanges.Contains((b.StartLine, b.EndLine)))
                .ToList();

            foreach (var deletedBlock in deletedBlocks)
            {
                deletedBlock.DeletedAt = DateTime.UtcNow;
            }

            // Remove embeddings for deleted blocks
            var deletedBlockIds = deletedBlocks.Select(b => b.Id).ToList();
            var embeddingsToDelete = await _context.BlockEmbeddings
                .Where(e => deletedBlockIds.Contains(e.BlockId))
                .ToListAsync(cancellationToken);

            _context.BlockEmbeddings.RemoveRange(embeddingsToDelete);

            var saveStartTime = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            var saveEndTime = DateTime.UtcNow;
            var saveDuration = (saveEndTime - saveStartTime).TotalMilliseconds;
            var totalDuration = (saveEndTime - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "✅ Обновление эмбеддингов завершено для документа {DocumentId}: создано={Created}, обновлено={Updated}, пропущено={Skipped}, удалено={Deleted}. Парсинг: {ParseDuration}ms, Сохранение: {SaveDuration}ms, Всего: {TotalDuration}ms, Эмбеддингов создано: {EmbedCount}, Среднее время: {AvgEmbedTime}ms",
                documentId, createdCount, updatedCount, skippedCount, deletedBlocks.Count, parseDuration, saveDuration, totalDuration, 
                embedGenerationCount, embedGenerationCount > 0 ? embedGenerationTotalMs / embedGenerationCount : 0
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating embeddings for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<EmbeddingStatusDTO> GetEmbeddingStatusAsync(Guid documentId, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all blocks with embeddings from database
            var blocksWithEmbeddings = await _context.DocumentBlocks
                .Include(b => b.Embedding)
                .Where(b => b.DocumentId == documentId && b.DeletedAt == null && b.Embedding != null)
                .ToListAsync(cancellationToken);

            // Split content into lines
            var lines = content.Split('\n');
            var lineStatuses = new List<LineEmbeddingStatusDTO>();
            
            // Create a set of covered line numbers for quick lookup
            var coveredLines = new HashSet<int>();
            var blockIdByLine = new Dictionary<int, Guid>();
            
            foreach (var block in blocksWithEmbeddings)
            {
                for (int lineNum = block.StartLine; lineNum <= block.EndLine; lineNum++)
                {
                    if (lineNum < lines.Length)
                    {
                        coveredLines.Add(lineNum);
                        blockIdByLine[lineNum] = block.Id;
                    }
                }
            }

            // Count non-empty lines
            int totalNonEmptyLines = 0;
            int coveredNonEmptyLines = 0;

            // Build line statuses
            for (int i = 0; i < lines.Length; i++)
            {
                var isEmpty = string.IsNullOrWhiteSpace(lines[i]);
                var isCovered = coveredLines.Contains(i);
                var blockId = blockIdByLine.TryGetValue(i, out var id) ? id : (Guid?)null;

                lineStatuses.Add(new LineEmbeddingStatusDTO
                {
                    LineNumber = i,
                    IsCovered = isCovered,
                    BlockId = blockId,
                    IsEmpty = isEmpty
                });

                if (!isEmpty)
                {
                    totalNonEmptyLines++;
                    if (isCovered)
                    {
                        coveredNonEmptyLines++;
                    }
                }
            }

            // Calculate coverage percentage
            double coveragePercentage = totalNonEmptyLines > 0
                ? (double)coveredNonEmptyLines / totalNonEmptyLines * 100.0
                : 0.0;

            return new EmbeddingStatusDTO
            {
                CoveragePercentage = coveragePercentage,
                TotalLines = totalNonEmptyLines,
                CoveredLines = coveredNonEmptyLines,
                LineStatuses = lineStatuses
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting embedding status for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task UpdateEmbeddingsForDocumentAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("📋 UpdateEmbeddingsForDocumentAsync вызван для документа {DocumentId}, userId {UserId}", documentId, userId);
        try
        {
            _logger.LogInformation("📥 Загрузка документа {DocumentId} для обновления эмбеддингов", documentId);
            
            var loadStartTime = DateTime.UtcNow;
            var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
            var loadEndTime = DateTime.UtcNow;
            var loadDuration = (loadEndTime - loadStartTime).TotalMilliseconds;
            
            if (document == null)
            {
                _logger.LogWarning("❌ Документ {DocumentId} не найден для пользователя {UserId}", documentId, userId);
                return;
            }

            var content = document.Content ?? string.Empty;
            var contentLength = content.Length;
            _logger.LogInformation("📄 Документ загружен: {ContentLength} символов, загрузка заняла {LoadDuration}ms", contentLength, loadDuration);
            
            var updateStartTime = DateTime.UtcNow;
            _logger.LogInformation("🚀 Начало UpdateEmbeddingsAsync для документа {DocumentId}", documentId);
            await UpdateEmbeddingsAsync(documentId, content, cancellationToken);
            var updateEndTime = DateTime.UtcNow;
            var updateDuration = (updateEndTime - updateStartTime).TotalMilliseconds;
            var totalDuration = (updateEndTime - startTime).TotalMilliseconds;
            
            _logger.LogInformation("✅ Обновление эмбеддингов завершено для документа {DocumentId}. Загрузка: {LoadDuration}ms, Обновление: {UpdateDuration}ms, Всего: {TotalDuration}ms, Длина контента: {ContentLength}", 
                documentId, loadDuration, updateDuration, totalDuration, contentLength);
        }
        catch (Exception ex)
        {
            var errorTime = DateTime.UtcNow;
            var errorDuration = (errorTime - startTime).TotalMilliseconds;
            _logger.LogError(ex, "❌ Ошибка при обновлении эмбеддингов для документа {DocumentId}. Длительность: {ErrorDuration}ms. Исключение: {ExceptionType}, Сообщение: {Message}", 
                documentId, errorDuration, ex.GetType().Name, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Выбирает текст для создания эмбеддинга: использует нормализованный текст, если он достаточно длинный,
    /// иначе использует rawText. Это позволяет создавать эмбеддинги даже для коротких текстов.
    /// </summary>
    private string GetTextForEmbedding(string normalizedText, string rawText)
    {
        // Если нормализованный текст не пустой и достаточно длинный, используем его
        if (!string.IsNullOrWhiteSpace(normalizedText) && normalizedText.Trim().Length >= MinTextLengthForEmbedding)
        {
            return normalizedText;
        }
        
        // Иначе используем rawText, убрав только начальные и конечные пробелы
        var rawTextTrimmed = rawText?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(rawTextTrimmed))
        {
            return rawTextTrimmed;
        }
        
        // Если оба текста пустые, возвращаем нормализованный текст (может быть пустым)
        return normalizedText ?? string.Empty;
    }
}
