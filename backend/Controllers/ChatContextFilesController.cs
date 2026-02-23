using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Services.ChatContext;
using System.Security.Claims;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/chats/{chatId:guid}/context-files")]
[Authorize]
public class ChatContextFilesController : ControllerBase
{
    private readonly IChatContextFileService _service;
    private readonly ILogger<ChatContextFilesController> _logger;

    public ChatContextFilesController(IChatContextFileService service, ILogger<ChatContextFilesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found");
        return Guid.Parse(userIdClaim);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChatContextFileDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upload(Guid chatId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Выберите файл для загрузки." });

        try
        {
            var userId = GetUserId();
            var result = await _service.UploadAsync(chatId, userId, file, cancellationToken);
            return CreatedAtAction(nameof(List), new { chatId }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload context file to chat {ChatId}", chatId);
            return StatusCode(500, new { message = "Ошибка при загрузке файла." });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ChatContextFileDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(Guid chatId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var files = await _service.ListAsync(chatId, userId, cancellationToken);
            return Ok(files);
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid chatId, Guid fileId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            await _service.DeleteAsync(fileId, userId, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
