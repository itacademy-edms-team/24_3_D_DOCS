using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Document;
using RusalProject.Services.Documents;
using RusalProject.Services.Embedding;
using RusalProject.Services.Pdf;
using RusalProject.Services.Storage;
using System.Security.Claims;
using System.IO;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly IMinioService _minioService;
    private readonly IEmbeddingStorageService _embeddingStorageService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        IPdfGeneratorService pdfGeneratorService,
        IMinioService minioService,
        IEmbeddingStorageService embeddingStorageService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _pdfGeneratorService = pdfGeneratorService;
        _minioService = minioService;
        _embeddingStorageService = embeddingStorageService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }

        return userId;
    }

    /// <summary>
    /// Получить список документов
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DocumentDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments([FromQuery] string? status, [FromQuery] string? search)
    {
        try
        {
            var userId = GetUserId();
            var documents = await _documentService.GetDocumentsAsync(userId, status, search);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить документ по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentWithContentDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var document = await _documentService.GetDocumentWithContentAsync(id, userId);
            
            if (document == null)
                return NotFound(new { message = "Документ не найден" });

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Создать новый документ
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetUserId();
            var document = await _documentService.CreateDocumentAsync(userId, dto);
            return StatusCode(StatusCodes.Status201Created, document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document: {Message}\n{StackTrace}", ex.Message, ex.StackTrace);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    /// <summary>
    /// Обновить документ
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DocumentDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentDTO dto)
    {
        try
        {
            var userId = GetUserId();
            var document = await _documentService.UpdateDocumentAsync(id, userId, dto);
            return Ok(document);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Обновить контент документа (Markdown)
    /// </summary>
    [HttpPut("{id}/content")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDocumentContent(Guid id, [FromBody] UpdateDocumentContentDTO dto)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.UpdateDocumentContentAsync(id, userId, dto.Content);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document content {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить статус покрытия эмбеддингами
    /// </summary>
    [HttpGet("{id}/embeddings/status")]
    [ProducesResponseType(typeof(EmbeddingStatusDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmbeddingStatus(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var document = await _documentService.GetDocumentWithContentAsync(id, userId);
            
            if (document == null)
                return NotFound(new { message = "Документ не найден" });

            var status = await _embeddingStorageService.GetEmbeddingStatusAsync(
                id, 
                document.Content ?? string.Empty, 
                HttpContext.RequestAborted
            );

            return Ok(status);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting embedding status for document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Обновить эмбеддинги для документа (фоновое обновление)
    /// </summary>
    [HttpPost("{id}/embeddings/update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmbeddings(Guid id)
    {
        _logger.LogInformation("📥 POST /api/documents/{DocumentId}/embeddings/update - Запрос получен", id);
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("🔄 Начало обновления эмбеддингов для документа {DocumentId}, userId {UserId}", id, userId);
            await _embeddingStorageService.UpdateEmbeddingsForDocumentAsync(id, userId, HttpContext.RequestAborted);
            _logger.LogInformation("✅ Завершено обновление эмбеддингов для документа {DocumentId}", id);
            return NoContent();
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning("❌ Документ {DocumentId} не найден: {Message}", id, ex.Message);
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при обновлении эмбеддингов для документа {DocumentId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Обновить переопределения стилей
    /// </summary>
    [HttpPut("{id}/overrides")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDocumentOverrides(Guid id, [FromBody] UpdateDocumentOverridesDTO dto)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.UpdateDocumentOverridesAsync(id, userId, dto.Overrides);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document overrides {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Обновить метаданные документа (переменные титульного листа)
    /// </summary>
    [HttpPut("{id}/metadata")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDocumentMetadata(Guid id, [FromBody] DocumentMetadataDTO metadata)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.UpdateDocumentMetadataAsync(id, userId, metadata);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document metadata {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Удалить документ (в корзину)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.DeleteDocumentAsync(id, userId);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Восстановить документ из корзины
    /// </summary>
    [HttpPost("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreDocument(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.RestoreDocumentAsync(id, userId);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Удалить документ навсегда
    /// </summary>
    [HttpDelete("{id}/permanent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocumentPermanently(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.DeleteDocumentPermanentlyAsync(id, userId);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Архивировать документ
    /// </summary>
    [HttpPost("{id}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveDocument(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.ArchiveDocumentAsync(id, userId);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Разархивировать документ
    /// </summary>
    [HttpPost("{id}/unarchive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnarchiveDocument(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.UnarchiveDocumentAsync(id, userId);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unarchiving document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Генерация PDF документа
    /// </summary>
    [HttpPost("{id}/pdf")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePdf(Guid id, [FromQuery] Guid? titlePageId = null)
    {
        try
        {
            var userId = GetUserId();
            var pdfBytes = await _pdfGeneratorService.GeneratePdfAsync(id, userId, titlePageId);
            
            // Сохраняем PDF в MinIO
            var document = await _documentService.GetDocumentByIdAsync(id, userId);
            if (document != null)
            {
                var bucket = $"user-{userId}";
                var pdfPath = $"documents/{id}/document.pdf";
                using var pdfStream = new MemoryStream(pdfBytes);
                await _minioService.UploadFileAsync(bucket, pdfPath, pdfStream, "application/pdf");
                
                // Обновляем PdfMinioPath в базе данных
                await _documentService.UpdatePdfPathAsync(id, userId, pdfPath);
            }

            return File(pdfBytes, "application/pdf", $"{document?.Name ?? "document"}.pdf");
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Скачать PDF документа
    /// </summary>
    [HttpGet("{id}/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var document = await _documentService.GetDocumentByIdAsync(id, userId);
            
            if (document == null)
            {
                return NotFound(new { message = "Документ не найден" });
            }

            if (!document.HasPdf)
            {
                return NotFound(new { message = "PDF не найден. Сначала сгенерируйте PDF." });
            }

            var bucket = $"user-{userId}";
            var pdfPath = $"documents/{id}/document.pdf";
            using var pdfStream = await _minioService.DownloadFileAsync(bucket, pdfPath);
            
            var pdfBytes = new byte[pdfStream.Length];
            await pdfStream.ReadAsync(pdfBytes, 0, (int)pdfStream.Length);

            return File(pdfBytes, "application/pdf", $"{document.Name}.pdf");
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "PDF не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PDF for document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Экспортировать документ в формат .ddoc
    /// </summary>
    [HttpGet("{id}/export")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportDocument(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var document = await _documentService.GetDocumentByIdAsync(id, userId);
            
            if (document == null)
            {
                return NotFound(new { message = "Документ не найден" });
            }

            var exportStream = await _documentService.ExportDocumentAsync(id, userId);
            var fileName = $"{document.Name}.ddoc";
            
            return File(exportStream, "application/x-tar", fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Импортировать документ из формата .ddoc
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportDocument([FromForm] IFormFile file, [FromForm] string? name = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Файл не предоставлен" });
            }

            // Проверка расширения файла
            var fileName = file.FileName;
            if (!fileName.EndsWith(".ddoc", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Файл должен иметь расширение .ddoc" });
            }

            var userId = GetUserId();
            var documentName = name ?? Path.GetFileNameWithoutExtension(fileName);
            
            using var fileStream = file.OpenReadStream();
            var document = await _documentService.ImportDocumentAsync(userId, fileStream, documentName);
            
            return StatusCode(StatusCodes.Status201Created, document);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid .ddoc file format");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing document");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }
}
