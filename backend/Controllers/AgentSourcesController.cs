using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Services.AgentSources;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/ai/agent-sources")]
[Authorize]
public class AgentSourcesController : ControllerBase
{
    private readonly IAgentSourceService _agentSourceService;
    private readonly ILogger<AgentSourcesController> _logger;

    public AgentSourcesController(IAgentSourceService agentSourceService, ILogger<AgentSourcesController> logger)
    {
        _agentSourceService = agentSourceService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        return Guid.Parse(userIdClaim);
    }

    /// <summary>
    /// Загрузка файла для контекста Document Agent (PDF, текст, изображение).
    /// </summary>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(AgentSourceIngestResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ingest(
        [FromForm] Guid documentId,
        [FromForm] Guid chatId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Файл не предоставлен." });

        try
        {
            var userId = GetUserId();
            var result = await _agentSourceService.IngestAsync(userId, documentId, chatId, file, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent source ingest failed");
            return StatusCode(500, new { message = "Не удалось обработать файл." });
        }
    }
}
