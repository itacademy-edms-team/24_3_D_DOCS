using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Document;
using RusalProject.Models.Types;
using RusalProject.Services.Document;
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
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        IPdfGeneratorService pdfGeneratorService,
        IMinioService minioService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _pdfGeneratorService = pdfGeneratorService;
        _minioService = minioService;
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
    /// Получить содержание документа
    /// </summary>
    [HttpGet("{id}/table-of-contents")]
    [ProducesResponseType(typeof(List<TocItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTableOfContents(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var toc = await _documentService.GetTableOfContentsAsync(id, userId);
            if (toc == null)
                return Ok(new List<TocItem>());
            return Ok(toc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting table of contents for document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Сгенерировать содержание из заголовков
    /// </summary>
    [HttpPost("{id}/table-of-contents/generate")]
    [ProducesResponseType(typeof(List<TocItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateTableOfContents(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var toc = await _documentService.GenerateTableOfContentsAsync(id, userId);
            return Ok(toc);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating table of contents for document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Обновить содержание вручную
    /// </summary>
    [HttpPut("{id}/table-of-contents")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTableOfContents(Guid id, [FromBody] List<TocItem> items)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.UpdateTableOfContentsAsync(id, userId, items);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating table of contents for document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Сбросить содержание к автогенерации из заголовков
    /// </summary>
    [HttpPost("{id}/table-of-contents/reset")]
    [ProducesResponseType(typeof(List<TocItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetTableOfContents(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var toc = await _documentService.ResetTableOfContentsAsync(id, userId);
            return Ok(toc);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting table of contents for document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Сохранить текущую версию документа
    /// </summary>
    [HttpPost("{id}/versions")]
    [ProducesResponseType(typeof(DocumentVersionDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveDocumentVersion(Guid id, [FromBody] SaveDocumentVersionDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Имя версии обязательно" });
        }

        try
        {
            var userId = GetUserId();
            var version = await _documentService.SaveVersionAsync(id, userId, dto.Name);
            return StatusCode(StatusCodes.Status201Created, version);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving version for document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить список версий документа
    /// </summary>
    [HttpGet("{id}/versions")]
    [ProducesResponseType(typeof(List<DocumentVersionDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentVersions(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var versions = await _documentService.GetVersionsAsync(id, userId);
            return Ok(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting versions for document {DocumentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить контент версии документа
    /// </summary>
    [HttpGet("{id}/versions/{versionId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentVersionContent(Guid id, Guid versionId)
    {
        try
        {
            var userId = GetUserId();
            var content = await _documentService.GetVersionContentAsync(id, versionId, userId);
            return Ok(new { content });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Версия или документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version content for document {DocumentId}, version {VersionId}", id, versionId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Восстановить документ из версии
    /// </summary>
    [HttpPost("{id}/versions/{versionId}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreDocumentVersion(Guid id, Guid versionId)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.RestoreVersionAsync(id, versionId, userId);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Версия или документ не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring version {VersionId} for document {DocumentId}", versionId, id);
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

            try
            {
                if (exportStream.CanSeek)
                {
                    _logger.LogInformation("Exporting document {DocumentId} as .ddoc, size {Length} bytes", id, exportStream.Length);
                    exportStream.Position = 0;
                }
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Failed to read export stream length for document {DocumentId}", id);
            }

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
