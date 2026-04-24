using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Notification;
using RusalProject.Services.Notification;
using System.Security.Claims;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        try
        {
            var items = await _notificationService.ListForUserAsync(GetUserId());
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List notifications");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        try
        {
            await _notificationService.MarkReadAsync(id, GetUserId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkRead {Id}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}
