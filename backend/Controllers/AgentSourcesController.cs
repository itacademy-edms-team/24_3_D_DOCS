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
    /// Загрузка файла для контекста агента (глобальный или чат документа). Для чата документа передайте documentId.
    /// </summary>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(AgentSourceIngestResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ingest(
        [FromForm] Guid chatId,
        IFormFile file,
        [FromForm] Guid? documentId = null,
        CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Скачать оригинальный загруженный файл.
    /// </summary>
    [HttpGet("{sessionId:guid}/original")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadOriginal(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var result = await _agentSourceService.GetOriginalFileAsync(userId, sessionId, cancellationToken);
            if (result == null)
                return NotFound(new { message = "Файл не найден или сессия истекла." });

            return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download original failed for session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Не удалось скачать файл." });
        }
    }
}
