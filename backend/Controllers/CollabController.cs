using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Services.Collab;
using System.Security.Claims;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/collab")]
[Authorize]
public class CollabController : ControllerBase
{
    private readonly ICollabService _collabService;
    private readonly ILogger<CollabController> _logger;

    public CollabController(ICollabService collabService, ILogger<CollabController> logger)
    {
        _collabService = collabService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user token");
        return userId;
    }

    [HttpPost("invites/{inviteId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AcceptInvite(Guid inviteId)
    {
        try
        {
            await _collabService.AcceptInviteAsync(inviteId, GetUserId());
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AcceptInvite {InviteId}", inviteId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpPost("invites/{inviteId:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeclineInvite(Guid inviteId)
    {
        try
        {
            await _collabService.DeclineInviteAsync(inviteId, GetUserId());
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeclineInvite {InviteId}", inviteId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpPost("documents/{documentId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveCollab(Guid documentId)
    {
        try
        {
            await _collabService.LeaveCollabAsync(documentId, GetUserId());
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LeaveCollab {DocumentId}", documentId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}
