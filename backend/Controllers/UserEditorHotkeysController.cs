using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs;
using RusalProject.Services.EditorHotkeys;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/user/editor-hotkeys")]
[Authorize]
public class UserEditorHotkeysController : ControllerBase
{
	private readonly IUserEditorHotkeysService _hotkeys;

	public UserEditorHotkeysController(IUserEditorHotkeysService hotkeys)
	{
		_hotkeys = hotkeys;
	}

	private Guid GetUserId()
	{
		var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? User.FindFirst("sub")?.Value
			?? throw new UnauthorizedAccessException("User ID not found in token");
		return Guid.Parse(userIdClaim);
	}

	[HttpGet]
	[ProducesResponseType(typeof(EditorHotkeysResponseDto), StatusCodes.Status200OK)]
	public async Task<IActionResult> Get(CancellationToken cancellationToken)
	{
		var userId = GetUserId();
		var dto = await _hotkeys.GetAsync(userId, cancellationToken);
		return Ok(dto);
	}

	[HttpPut]
	[ProducesResponseType(typeof(EditorHotkeysResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> Put([FromBody] SetEditorHotkeysDto? body, CancellationToken cancellationToken)
	{
		if (body is null)
			return BadRequest(new { message = "Тело запроса обязательно." });

		try
		{
			var userId = GetUserId();
			await _hotkeys.SaveAsync(userId, body, cancellationToken);
			var dto = await _hotkeys.GetAsync(userId, cancellationToken);
			return Ok(dto);
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}
}
