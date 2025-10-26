using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;
using RusalProject.Services.Storage;
using System.Security.Claims;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentLinksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;
    private readonly ILogger<DocumentLinksController> _logger;

    public DocumentLinksController(
        ApplicationDbContext context, 
        IMinioService minioService, 
        ILogger<DocumentLinksController> logger)
    {
        _context = context;
        _minioService = minioService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentLinkDTO>>> GetDocumentLinks()
    {
        var userId = GetCurrentUserId();
        var documentLinks = await _context.DocumentLinks
            .Where(d => d.CreatorId == userId)
            .Select(d => new DocumentLinkDTO
            {
                Id = d.Id,
                CreatorId = d.CreatorId,
                Name = d.Name,
                Description = d.Description,
                MdMinioPath = d.MdMinioPath,
                PdfMinioPath = d.PdfMinioPath,
                Status = d.Status,
                ConversionLog = d.ConversionLog,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();

        return Ok(documentLinks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentLinkDTO>> GetDocumentLink(Guid id)
    {
        var userId = GetCurrentUserId();
        var documentLink = await _context.DocumentLinks
            .Where(d => d.Id == id && d.CreatorId == userId)
            .Select(d => new DocumentLinkDTO
            {
                Id = d.Id,
                CreatorId = d.CreatorId,
                Name = d.Name,
                Description = d.Description,
                MdMinioPath = d.MdMinioPath,
                PdfMinioPath = d.PdfMinioPath,
                Status = d.Status,
                ConversionLog = d.ConversionLog,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (documentLink == null)
            return NotFound();

        return Ok(documentLink);
    }

    [HttpPost]
    public async Task<ActionResult<DocumentLinkDTO>> CreateDocumentLink([FromForm] CreateDocumentLinkDTO dto, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required");

        var userId = GetCurrentUserId();
        var documentId = Guid.NewGuid();
        var fileName = $"{documentId}.md";
        var minioPath = $"users/{userId}/documents/{fileName}";

        // Ensure documents bucket exists
        if (!await _minioService.BucketExistsAsync("documents"))
        {
            await _minioService.CreateBucketAsync("documents");
        }

        // Upload file to MinIO
        using var stream = file.OpenReadStream();
        await _minioService.UploadFileAsync("documents", minioPath, stream, "text/markdown");

        // Create database record
        var documentLink = new DocumentLink
        {
            Id = documentId,
            CreatorId = userId,
            Name = dto.Name,
            Description = dto.Description,
            MdMinioPath = minioPath,
            Status = "draft"
        };

        _context.DocumentLinks.Add(documentLink);
        await _context.SaveChangesAsync();

        var result = new DocumentLinkDTO
        {
            Id = documentLink.Id,
            CreatorId = documentLink.CreatorId,
            Name = documentLink.Name,
            Description = documentLink.Description,
            MdMinioPath = documentLink.MdMinioPath,
            PdfMinioPath = documentLink.PdfMinioPath,
            Status = documentLink.Status,
            ConversionLog = documentLink.ConversionLog,
            CreatedAt = documentLink.CreatedAt,
            UpdatedAt = documentLink.UpdatedAt
        };

        return CreatedAtAction(nameof(GetDocumentLink), new { id = documentLink.Id }, result);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDocumentLink(Guid id)
    {
        var userId = GetCurrentUserId();
        var documentLink = await _context.DocumentLinks
            .Where(d => d.Id == id && d.CreatorId == userId)
            .FirstOrDefaultAsync();

        if (documentLink == null)
            return NotFound();

        try
        {
            var stream = await _minioService.DownloadFileAsync("documents", documentLink.MdMinioPath);
            return File(stream, "text/markdown", $"{documentLink.Name}.md");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", id);
            return NotFound("File not found");
        }
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var userId = GetCurrentUserId();
        var documentLink = await _context.DocumentLinks
            .Where(d => d.Id == id && d.CreatorId == userId)
            .FirstOrDefaultAsync();

        if (documentLink == null)
            return NotFound();

        if (string.IsNullOrEmpty(documentLink.PdfMinioPath))
            return NotFound("PDF not available");

        try
        {
            var stream = await _minioService.DownloadFileAsync("documents", documentLink.PdfMinioPath);
            return File(stream, "application/pdf", $"{documentLink.Name}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PDF for document {DocumentId}", id);
            return NotFound("PDF file not found");
        }
    }

    [HttpPost("{id}/convert")]
    public async Task<IActionResult> ConvertToPdf(Guid id, [FromBody] ConvertDocumentDTO dto)
    {
        var userId = GetCurrentUserId();
        var documentLink = await _context.DocumentLinks
            .Where(d => d.Id == id && d.CreatorId == userId)
            .FirstOrDefaultAsync();

        if (documentLink == null)
            return NotFound();

        // TODO: Implement Pandoc conversion logic
        // For now, just update status
        documentLink.Status = "processing";
        await _context.SaveChangesAsync();

        return Ok(new { message = "Conversion started", status = "processing" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocumentLink(Guid id)
    {
        var userId = GetCurrentUserId();
        var documentLink = await _context.DocumentLinks
            .Where(d => d.Id == id && d.CreatorId == userId)
            .FirstOrDefaultAsync();

        if (documentLink == null)
            return NotFound();

        // Delete files from MinIO
        try
        {
            await _minioService.DeleteFileAsync("documents", documentLink.MdMinioPath);
            if (!string.IsNullOrEmpty(documentLink.PdfMinioPath))
            {
                await _minioService.DeleteFileAsync("documents", documentLink.PdfMinioPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deleting files from MinIO for document {DocumentId}", id);
        }

        // Delete database record
        _context.DocumentLinks.Remove(documentLink);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user ID");
        return userId;
    }
}
