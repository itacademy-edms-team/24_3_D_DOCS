using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Services.Attachment;
using System.Security.Claims;
using RusalProject.Models.Entities;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/attachments")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;
    private readonly ILogger<AttachmentsController> _logger;

    public AttachmentsController(IAttachmentService attachmentService, ILogger<AttachmentsController> logger)
    {
        _attachmentService = attachmentService;
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

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? type = null, [FromQuery] string sort = "modified_desc")
    {
        try
        {
            var userId = GetUserId();
            var items = await _attachmentService.ListAttachmentsAsync(userId, type, sort);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing attachments");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var attachment = await _attachmentService.GetAttachmentAsync(id, userId);
            if (attachment == null)
                return NotFound(new { message = "Файл не найден" });

            using var stream = await _attachmentService.DownloadAttachmentAsync(id, userId);
            var bytes = new byte[stream.Length];
            await stream.ReadAsync(bytes, 0, (int)stream.Length);
            return File(bytes, attachment.ContentType, attachment.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Файл не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading attachment {AttachmentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpGet("{id}/presigned")]
    public async Task<IActionResult> Presigned(Guid id, [FromQuery] int expirySeconds = 3600)
    {
        try
        {
            var userId = GetUserId();
            var url = await _attachmentService.GetPresignedUrlAsync(id, userId, expirySeconds);
            return Ok(new { url });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Файл не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for {AttachmentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    public class RenameDto
    {
        public string? Name { get; set; }
    }

    [HttpPatch("{id}/rename")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Invalid name" });

        try
        {
            var userId = GetUserId();
            await _attachmentService.RenameAttachmentAsync(id, userId, dto.Name);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Файл не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming attachment {AttachmentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _attachmentService.DeleteAttachmentAsync(id, userId);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Файл не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {AttachmentId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}
