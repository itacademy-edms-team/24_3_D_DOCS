using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Profiles;
using RusalProject.Services.Profiles;
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
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    [HttpGet]
    public async Task<ActionResult<List<ProfileMetaDTO>>> GetAll()
    {
        var userId = GetUserId();
        var profiles = await _profileService.GetAllProfilesAsync(userId);
        return Ok(profiles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProfileDTO>> GetById(Guid id)
    {
        var userId = GetUserId();
        var profile = await _profileService.GetProfileByIdAsync(id, userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult<ProfileDTO>> Create([FromBody] CreateProfileDTO dto)
    {
        var userId = GetUserId();
        var profile = await _profileService.CreateProfileAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = profile.Id }, profile);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProfileDTO>> Update(Guid id, [FromBody] UpdateProfileDTO dto)
    {
        var userId = GetUserId();
        var profile = await _profileService.UpdateProfileAsync(id, dto, userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var deleted = await _profileService.DeleteProfileAsync(id, userId);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
