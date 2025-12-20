using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Documents;
using RusalProject.Services.Documents;
using System.Security.Claims;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    [HttpGet]
    public async Task<ActionResult<List<DocumentMetaDTO>>> GetAll()
    {
        var userId = GetUserId();
        var documents = await _documentService.GetAllDocumentsAsync(userId);
        return Ok(documents);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentDTO>> GetById(Guid id)
    {
        var userId = GetUserId();
        var document = await _documentService.GetDocumentByIdAsync(id, userId);
        if (document == null) return NotFound();
        return Ok(document);
    }

    [HttpPost]
    public async Task<ActionResult<DocumentDTO>> Create([FromBody] CreateDocumentDTO dto)
    {
        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentsController.cs:47\",\"message\":\"Create endpoint called\",\"data\":{{\"dtoName\":\"{dto.Name}\",\"dtoProfileId\":\"{dto.ProfileId}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run2\",\"hypothesisId\":\"A\"}}");
        // #endregion

        var userId = GetUserId();

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentsController.cs:50\",\"message\":\"User ID extracted\",\"data\":{{\"userId\":\"{userId}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run2\",\"hypothesisId\":\"A\"}}");
        // #endregion

        var document = await _documentService.CreateDocumentAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DocumentDTO>> Update(Guid id, [FromBody] UpdateDocumentDTO dto)
    {
        var userId = GetUserId();
        var document = await _documentService.UpdateDocumentAsync(id, dto, userId);
        if (document == null) return NotFound();
        return Ok(document);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var deleted = await _documentService.DeleteDocumentAsync(id, userId);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/images")]
    public async Task<ActionResult<object>> UploadImage(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var userId = GetUserId();
        using var stream = file.OpenReadStream();
        var url = await _documentService.UploadImageAsync(id, stream, file.FileName, userId);
        return Ok(new { url });
    }

    [HttpGet("{id}/images/{imageId}")]
    public async Task<IActionResult> GetImage(Guid id, string imageId)
    {
        var userId = GetUserId();
        var stream = await _documentService.GetImageAsync(id, imageId, userId);
        if (stream == null) return NotFound();

        var extension = Path.GetExtension(imageId);
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        return File(stream, contentType);
    }

    [HttpDelete("{id}/images/{imageId}")]
    public async Task<IActionResult> DeleteImage(Guid id, string imageId)
    {
        var userId = GetUserId();
        var deleted = await _documentService.DeleteImageAsync(id, imageId, userId);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
