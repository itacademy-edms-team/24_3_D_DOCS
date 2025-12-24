using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.TitlePages;
using RusalProject.Services.TitlePages;
using System.Security.Claims;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/title-pages")]
[Authorize]
public class TitlePagesController : ControllerBase
{
    private readonly ITitlePageService _titlePageService;
    private readonly ILogger<TitlePagesController> _logger;

    public TitlePagesController(ITitlePageService titlePageService, ILogger<TitlePagesController> logger)
    {
        _titlePageService = titlePageService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    [HttpGet]
    public async Task<ActionResult<List<TitlePageMetaDTO>>> GetAll()
    {
        var userId = GetUserId();
        var titlePages = await _titlePageService.GetAllTitlePagesAsync(userId);
        return Ok(titlePages);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TitlePageDTO>> GetById(Guid id)
    {
        var userId = GetUserId();
        var titlePage = await _titlePageService.GetTitlePageByIdAsync(id, userId);
        if (titlePage == null) return NotFound();
        return Ok(titlePage);
    }

    [HttpPost]
    public async Task<ActionResult<TitlePageDTO>> Create([FromBody] CreateTitlePageDTO dto)
    {
        var userId = GetUserId();
        var titlePage = await _titlePageService.CreateTitlePageAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = titlePage.Id }, titlePage);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TitlePageDTO>> Update(Guid id, [FromBody] UpdateTitlePageDTO dto)
    {
        var userId = GetUserId();
        var titlePage = await _titlePageService.UpdateTitlePageAsync(id, dto, userId);
        if (titlePage == null) return NotFound();
        return Ok(titlePage);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var deleted = await _titlePageService.DeleteTitlePageAsync(id, userId);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
