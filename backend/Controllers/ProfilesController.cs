using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Profile;
using RusalProject.Services.Profile;
using System.Security.Claims;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/profiles")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfilesController> _logger;

    public ProfilesController(IProfileService profileService, ILogger<ProfilesController> logger)
    {
        _profileService = profileService;
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
    /// Получить список профилей
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProfileDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfiles([FromQuery] bool includePublic = true)
    {
        try
        {
            var userId = GetUserId();
            var profiles = await _profileService.GetProfilesAsync(userId, includePublic);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить профиль по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProfileWithDataDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var profile = await _profileService.GetProfileWithDataAsync(id, userId);
            
            if (profile == null)
                return NotFound(new { message = "Профиль не найден" });

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile {ProfileId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Создать новый профиль
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProfileDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileDTO dto)
    {
        try
        {
            var userId = GetUserId();
            var profile = await _profileService.CreateProfileAsync(userId, dto);
            return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Обновить профиль
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProfileDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileDTO dto)
    {
        try
        {
            var userId = GetUserId();
            var profile = await _profileService.UpdateProfileAsync(id, userId, dto);
            return Ok(profile);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Профиль не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {ProfileId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Удалить профиль
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfile(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _profileService.DeleteProfileAsync(id, userId);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Профиль не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile {ProfileId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Дублировать профиль
    /// </summary>
    [HttpPost("{id}/duplicate")]
    [ProducesResponseType(typeof(ProfileDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DuplicateProfile(Guid id, [FromBody] DuplicateProfileRequestDTO? dto = null)
    {
        try
        {
            var userId = GetUserId();
            var profile = await _profileService.DuplicateProfileAsync(id, userId, dto?.Name);
            return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, profile);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Профиль не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating profile {ProfileId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}

public class DuplicateProfileRequestDTO
{
    public string? Name { get; set; }
}
