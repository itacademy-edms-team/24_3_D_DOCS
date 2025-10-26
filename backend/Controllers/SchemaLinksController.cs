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
public class SchemaLinksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;
    private readonly ILogger<SchemaLinksController> _logger;

    public SchemaLinksController(
        ApplicationDbContext context, 
        IMinioService minioService, 
        ILogger<SchemaLinksController> logger)
    {
        _context = context;
        _minioService = minioService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchemaLinkDTO>>> GetSchemaLinks()
    {
        var userId = GetCurrentUserId();
        var schemaLinks = await _context.SchemaLinks
            .Where(s => s.CreatorId == userId)
            .Select(s => new SchemaLinkDTO
            {
                Id = s.Id,
                CreatorId = s.CreatorId,
                Name = s.Name,
                Description = s.Description,
                MinioPath = s.MinioPath,
                PandocOptions = s.PandocOptions,
                IsPublic = s.IsPublic,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return Ok(schemaLinks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SchemaLinkDTO>> GetSchemaLink(Guid id)
    {
        var userId = GetCurrentUserId();
        var schemaLink = await _context.SchemaLinks
            .Where(s => s.Id == id && s.CreatorId == userId)
            .Select(s => new SchemaLinkDTO
            {
                Id = s.Id,
                CreatorId = s.CreatorId,
                Name = s.Name,
                Description = s.Description,
                MinioPath = s.MinioPath,
                PandocOptions = s.PandocOptions,
                IsPublic = s.IsPublic,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (schemaLink == null)
            return NotFound();

        return Ok(schemaLink);
    }

    [HttpPost]
    public async Task<ActionResult<SchemaLinkDTO>> CreateSchemaLink([FromForm] CreateSchemaLinkDTO dto, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required");

        var userId = GetCurrentUserId();
        var schemaId = Guid.NewGuid();
        var fileName = $"{schemaId}.tex";
        var minioPath = $"users/{userId}/schemas/{fileName}";

        // Ensure documents bucket exists
        if (!await _minioService.BucketExistsAsync("documents"))
        {
            await _minioService.CreateBucketAsync("documents");
        }

        // Upload file to MinIO
        using var stream = file.OpenReadStream();
        await _minioService.UploadFileAsync("documents", minioPath, stream, "text/plain");

        // Create database record
        var schemaLink = new SchemaLink
        {
            Id = schemaId,
            CreatorId = userId,
            Name = dto.Name,
            Description = dto.Description,
            MinioPath = minioPath,
            PandocOptions = dto.PandocOptions,
            IsPublic = dto.IsPublic
        };

        _context.SchemaLinks.Add(schemaLink);
        await _context.SaveChangesAsync();

        var result = new SchemaLinkDTO
        {
            Id = schemaLink.Id,
            CreatorId = schemaLink.CreatorId,
            Name = schemaLink.Name,
            Description = schemaLink.Description,
            MinioPath = schemaLink.MinioPath,
            PandocOptions = schemaLink.PandocOptions,
            IsPublic = schemaLink.IsPublic,
            CreatedAt = schemaLink.CreatedAt,
            UpdatedAt = schemaLink.UpdatedAt
        };

        return CreatedAtAction(nameof(GetSchemaLink), new { id = schemaLink.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SchemaLinkDTO>> UpdateSchemaLink(Guid id, [FromForm] CreateSchemaLinkDTO dto, IFormFile file)
    {
        var userId = GetCurrentUserId();
        var schemaLink = await _context.SchemaLinks
            .Where(s => s.Id == id && s.CreatorId == userId)
            .FirstOrDefaultAsync();

        if (schemaLink == null)
            return NotFound();

        // Update file if provided
        if (file != null && file.Length > 0)
        {
            // Delete old file
            try
            {
                await _minioService.DeleteFileAsync("documents", schemaLink.MinioPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting old file for schema {SchemaId}", id);
            }

            // Upload new file
            var fileName = $"{schemaLink.Id}.tex";
            var minioPath = $"users/{userId}/schemas/{fileName}";
            
            using var stream = file.OpenReadStream();
            await _minioService.UploadFileAsync("documents", minioPath, stream, "text/plain");
            schemaLink.MinioPath = minioPath;
        }

        // Update database record
        schemaLink.Name = dto.Name;
        schemaLink.Description = dto.Description;
        schemaLink.PandocOptions = dto.PandocOptions;
        schemaLink.IsPublic = dto.IsPublic;
        schemaLink.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var result = new SchemaLinkDTO
        {
            Id = schemaLink.Id,
            CreatorId = schemaLink.CreatorId,
            Name = schemaLink.Name,
            Description = schemaLink.Description,
            MinioPath = schemaLink.MinioPath,
            PandocOptions = schemaLink.PandocOptions,
            IsPublic = schemaLink.IsPublic,
            CreatedAt = schemaLink.CreatedAt,
            UpdatedAt = schemaLink.UpdatedAt
        };

        return Ok(result);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadSchemaLink(Guid id)
    {
        var userId = GetCurrentUserId();
        var schemaLink = await _context.SchemaLinks
            .Where(s => s.Id == id && s.CreatorId == userId)
            .FirstOrDefaultAsync();

        if (schemaLink == null)
            return NotFound();

        try
        {
            var stream = await _minioService.DownloadFileAsync("documents", schemaLink.MinioPath);
            return File(stream, "text/plain", $"{schemaLink.Name}.tex");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading schema {SchemaId}", id);
            return NotFound("File not found");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchemaLink(Guid id)
    {
        var userId = GetCurrentUserId();
        var schemaLink = await _context.SchemaLinks
            .Where(s => s.Id == id && s.CreatorId == userId)
            .FirstOrDefaultAsync();

        if (schemaLink == null)
            return NotFound();

        // Delete file from MinIO
        try
        {
            await _minioService.DeleteFileAsync("documents", schemaLink.MinioPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deleting file from MinIO for schema {SchemaId}", id);
        }

        // Delete database record
        _context.SchemaLinks.Remove(schemaLink);
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
