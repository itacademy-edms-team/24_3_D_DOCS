using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Models.DTOs.TitlePage;
using RusalProject.Services.Pdf;
using RusalProject.Services.TitlePage;
using System.Security.Claims;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/title-pages")]
[Authorize]
public class TitlePagesController : ControllerBase
{
    private readonly ITitlePageService _titlePageService;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly ILogger<TitlePagesController> _logger;

    public TitlePagesController(
        ITitlePageService titlePageService,
        IPdfGeneratorService pdfGeneratorService,
        ILogger<TitlePagesController> logger)
    {
        _titlePageService = titlePageService;
        _pdfGeneratorService = pdfGeneratorService;
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
    /// Получить список титульных страниц
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TitlePageDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTitlePages()
    {
        try
        {
            var userId = GetUserId();
            var titlePages = await _titlePageService.GetTitlePagesAsync(userId);
            return Ok(titlePages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting title pages: {Message}\n{StackTrace}", ex.Message, ex.StackTrace);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    /// <summary>
    /// Получить титульную страницу по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TitlePageWithDataDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTitlePage(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var titlePage = await _titlePageService.GetTitlePageWithDataAsync(id, userId);
            
            if (titlePage == null)
                return NotFound(new { message = "Титульная страница не найдена" });

            return Ok(titlePage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting title page {TitlePageId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Импорт титульной страницы с первой страницы PDF (текстовый слой и горизонтальные линии).
    /// </summary>
    [HttpPost("import-pdf")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TitlePageDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportTitlePageFromPdf([FromForm] ImportTitlePagePdfForm form)
    {
        if (form.File == null || form.File.Length == 0)
            return BadRequest(new { message = "Выберите PDF-файл" });

        var trimmedName = form.Name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
            return BadRequest(new { message = "Введите название титульника" });

        if (trimmedName.Length > 255)
            return BadRequest(new { message = "Название не должно превышать 255 символов" });

        var ext = Path.GetExtension(form.File.FileName).ToLowerInvariant();
        var contentType = form.File.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (ext != ".pdf" && !contentType.Contains("pdf"))
            return BadRequest(new { message = "Допустимы только PDF-файлы" });

        try
        {
            await using var ms = new MemoryStream();
            await form.File.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var layout = PdfFirstPageLayoutExtractor.Extract(bytes);
            var data = FirstPageLayoutToTitlePageMapper.Map(layout);

            var userId = GetUserId();
            var dto = new CreateTitlePageDTO
            {
                Name = trimmedName,
                Data = data,
            };

            var titlePage = await _titlePageService.CreateTitlePageAsync(userId, dto);
            return StatusCode(StatusCodes.Status201Created, titlePage);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing title page from PDF");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    /// <summary>
    /// Создать новую титульную страницу
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TitlePageDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTitlePage([FromBody] CreateTitlePageDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetUserId();
            var titlePage = await _titlePageService.CreateTitlePageAsync(userId, dto);
            return StatusCode(StatusCodes.Status201Created, titlePage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating title page: {Message}", ex.Message);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    /// <summary>
    /// Обновить титульную страницу
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TitlePageDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTitlePage(Guid id, [FromBody] UpdateTitlePageDTO dto)
    {
        try
        {
            var userId = GetUserId();
            var titlePage = await _titlePageService.UpdateTitlePageAsync(id, userId, dto);
            return Ok(titlePage);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Титульная страница не найдена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating title page {TitlePageId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Удалить титульную страницу
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTitlePage(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _titlePageService.DeleteTitlePageAsync(id, userId);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Титульная страница не найдена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting title page {TitlePageId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Генерация PDF титульной страницы
    /// </summary>
    [HttpPost("{id}/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateTitlePagePdf(Guid id, [FromBody] Dictionary<string, string>? variables = null)
    {
        try
        {
            var userId = GetUserId();
            var pdfBytes = await _pdfGeneratorService.GenerateTitlePagePdfAsync(id, userId, variables);
            
            var titlePage = await _titlePageService.GetTitlePageByIdAsync(id, userId);
            var fileName = titlePage?.Name ?? "title-page";

            return File(pdfBytes, "application/pdf", $"{fileName}.pdf");
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Титульная страница не найдена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for title page {TitlePageId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}
