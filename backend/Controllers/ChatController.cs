using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Models.Types;
using RusalProject.Services.Chat;
using System.Security.Claims;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/chats")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
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
    /// Получить чаты по scope: ?scope=global или ?scope=document&documentId=...
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ChatSessionDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChats([FromQuery] ChatScope? scope, [FromQuery] Guid? documentId, [FromQuery] bool includeArchived = false)
    {
        try
        {
            if (!scope.HasValue)
                return BadRequest(new { message = "Параметр scope обязателен (global или document)" });
            if (scope == ChatScope.Document && !documentId.HasValue)
                return BadRequest(new { message = "При scope=document параметр documentId обязателен" });

            var userId = GetUserId();
            var chats = await _chatService.GetChatsByScopeAsync(userId, scope.Value, documentId, includeArchived);
            return Ok(chats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chats: {Error}", ex.Message);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить все чаты документа (включая архив) — обратная совместимость
    /// </summary>
    [HttpGet("document/{documentId}")]
    [ProducesResponseType(typeof(List<ChatSessionDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChatsByDocument(Guid documentId, [FromQuery] bool includeArchived = false)
    {
        try
        {
            var userId = GetUserId();
            var chats = await _chatService.GetChatsByDocumentAsync(documentId, userId, includeArchived);
            return Ok(chats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chats for document {DocumentId}: {Error}", documentId, ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    /// <summary>
    /// Получить чат с сообщениями по ID
    /// </summary>
    [HttpGet("{chatId}")]
    [ProducesResponseType(typeof(ChatSessionWithMessagesDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChatById(Guid chatId)
    {
        try
        {
            var userId = GetUserId();
            var chat = await _chatService.GetChatByIdAsync(chatId, userId);
            
            if (chat == null)
            {
                return NotFound(new { message = "Чат не найден" });
            }

            return Ok(chat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat {ChatId}", chatId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Создать новый чат
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatSessionDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatSessionDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var chat = await _chatService.CreateChatAsync(dto, userId);
            return CreatedAtAction(nameof(GetChatById), new { chatId = chat.Id }, chat);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized chat creation attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Обновить чат (переименование)
    /// </summary>
    [HttpPut("{chatId}")]
    [ProducesResponseType(typeof(ChatSessionDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateChat(Guid chatId, [FromBody] UpdateChatSessionDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var chat = await _chatService.UpdateChatAsync(chatId, dto, userId);
            return Ok(chat);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized chat update attempt for chat {ChatId}", chatId);
            return NotFound(new { message = "Чат не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chat {ChatId}", chatId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Переместить чат в архив
    /// </summary>
    [HttpDelete("{chatId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveChat(Guid chatId)
    {
        try
        {
            var userId = GetUserId();
            await _chatService.ArchiveChatAsync(chatId, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized chat archive attempt for chat {ChatId}", chatId);
            return NotFound(new { message = "Чат не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving chat {ChatId}", chatId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Восстановить чат из архива
    /// </summary>
    [HttpPost("{chatId}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreChat(Guid chatId)
    {
        try
        {
            var userId = GetUserId();
            await _chatService.RestoreChatAsync(chatId, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized chat restore attempt for chat {ChatId}", chatId);
            return NotFound(new { message = "Чат не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring chat {ChatId}", chatId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Удалить чат навсегда
    /// </summary>
    [HttpDelete("{chatId}/permanent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChatPermanently(Guid chatId)
    {
        try
        {
            var userId = GetUserId();
            await _chatService.DeleteChatPermanentlyAsync(chatId, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized chat permanent delete attempt for chat {ChatId}", chatId);
            return NotFound(new { message = "Чат не найден" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chat permanently {ChatId}", chatId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}
