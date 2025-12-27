using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RusalProject.Services.Auth;
using RusalProject.Services.Documents;
using RusalProject.Services.Storage;
using System.Security.Claims;

namespace RusalProject.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly IMinioService _minioService;
    private readonly IDocumentService _documentService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        IMinioService minioService,
        IDocumentService documentService,
        IJwtService jwtService,
        ILogger<UploadController> logger)
    {
        _minioService = minioService;
        _documentService = documentService;
        _jwtService = jwtService;
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
    /// Get userId from token - supports both Authorization header and query parameter
    /// </summary>
    private Guid? GetUserIdFromRequest()
    {
        // First try to get from authenticated user (Authorization header via [Authorize])
        if (User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                var userId = GetUserId();
                _logger.LogInformation("GetUserIdFromRequest: Got userId from authenticated user: {UserId}", userId);
                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetUserIdFromRequest: Failed to get userId from authenticated user");
                // Fall through to manual token extraction
            }
        }

        // Try to get token from query parameter
        var tokenFromQuery = Request.Query["token"].FirstOrDefault();
        // URL decode the token in case it was encoded
        if (!string.IsNullOrEmpty(tokenFromQuery))
        {
            try
            {
                tokenFromQuery = Uri.UnescapeDataString(tokenFromQuery);
            }
            catch
            {
                // If decoding fails, use original token
            }
        }
        
        _logger.LogInformation("GetUserIdFromRequest: tokenFromQuery is null or empty: {IsEmpty}, length: {Length}", 
            string.IsNullOrEmpty(tokenFromQuery), tokenFromQuery?.Length ?? 0);
        
        if (!string.IsNullOrEmpty(tokenFromQuery))
        {
            try
            {
                var userId = _jwtService.GetUserIdFromToken(tokenFromQuery);
                if (userId.HasValue)
                {
                    _logger.LogInformation("GetUserIdFromRequest: Successfully got userId from query token: {UserId}", userId.Value);
                    return userId;
                }
                else
                {
                    _logger.LogWarning("GetUserIdFromRequest: GetUserIdFromToken returned null for query token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetUserIdFromRequest: Failed to validate token from query parameter. Token length: {TokenLength}", tokenFromQuery?.Length ?? 0);
            }
        }

        // Try to get token from Authorization header manually (for AllowAnonymous scenarios)
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var tokenFromHeader = authHeader.Substring("Bearer ".Length).Trim();
            try
            {
                var userId = _jwtService.GetUserIdFromToken(tokenFromHeader);
                if (userId.HasValue)
                {
                    _logger.LogInformation("GetUserIdFromRequest: Successfully got userId from Authorization header: {UserId}", userId.Value);
                    return userId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetUserIdFromRequest: Failed to validate token from Authorization header");
            }
        }

        _logger.LogWarning("GetUserIdFromRequest: All methods failed, returning null");
        return null;
    }

    /// <summary>
    /// Get current access token from request (for URL generation)
    /// </summary>
    private string? GetCurrentToken()
    {
        // Try Authorization header first
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // Try query parameter
        var tokenFromQuery = Request.Query["token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tokenFromQuery))
        {
            return tokenFromQuery;
        }

        return null;
    }

    private string GetUserBucket(Guid userId) => $"user-{userId}";

    /// <summary>
    /// Загрузить изображение для документа
    /// </summary>
    [HttpPost("document/{documentId}/asset")]
    [Authorize]
    [ProducesResponseType(typeof(UploadAssetResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAsset(Guid documentId, IFormFile file)
    {
        try
        {
            var userId = GetUserId();
            
            // Проверяем существование документа
            if (!await _documentService.DocumentExistsAsync(documentId, userId))
            {
                return NotFound(new { message = "Документ не найден" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Файл не предоставлен" });
            }

            // Проверяем тип файла (только изображения)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Разрешены только изображения" });
            }

            // Генерируем уникальное имя файла
            var fileName = $"{Guid.NewGuid()}{extension}";
            var bucket = GetUserBucket(userId);
            var objectPath = $"documents/{documentId}/assets/{fileName}";

            // Загружаем в MinIO
            await _minioService.EnsureBucketExistsAsync(bucket);
            using var fileStream = file.OpenReadStream();
            await _minioService.UploadFileAsync(bucket, objectPath, fileStream, file.ContentType);

            // Use proxy URL instead of direct MinIO presigned URL
            // This ensures browser can access the image regardless of Docker networking
            // Include token in query parameter for browser access via <img> tag
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var token = GetCurrentToken();
            var url = string.IsNullOrEmpty(token) 
                ? $"{baseUrl}/api/upload/document/{documentId}/asset/{fileName}"
                : $"{baseUrl}/api/upload/document/{documentId}/asset/{fileName}?token={Uri.EscapeDataString(token)}";

            return Ok(new UploadAssetResponseDTO
            {
                FileName = fileName,
                OriginalFileName = file.FileName,
                Size = file.Length,
                ContentType = file.ContentType,
                Url = url,
                Path = objectPath
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading asset for document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Удалить изображение документа
    /// </summary>
    [HttpDelete("document/{documentId}/asset/{fileName}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsset(Guid documentId, string fileName)
    {
        try
        {
            var userId = GetUserId();
            
            // Проверяем существование документа
            if (!await _documentService.DocumentExistsAsync(documentId, userId))
            {
                return NotFound(new { message = "Документ не найден" });
            }

            var bucket = GetUserBucket(userId);
            var objectPath = $"documents/{documentId}/assets/{fileName}";

            if (!await _minioService.FileExistsAsync(bucket, objectPath))
            {
                return NotFound(new { message = "Файл не найден" });
            }

            await _minioService.DeleteFileAsync(bucket, objectPath);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset {FileName} for document {DocumentId}", fileName, documentId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить список изображений документа
    /// </summary>
    [HttpGet("document/{documentId}/assets")]
    [Authorize]
    [ProducesResponseType(typeof(List<AssetInfoDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssets(Guid documentId)
    {
        try
        {
            var userId = GetUserId();
            
            // Проверяем существование документа
            if (!await _documentService.DocumentExistsAsync(documentId, userId))
            {
                return NotFound(new { message = "Документ не найден" });
            }

            var bucket = GetUserBucket(userId);
            var prefix = $"documents/{documentId}/assets/";
            var files = await _minioService.ListFilesAsync(bucket, prefix);

            var assets = new List<AssetInfoDTO>();
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var token = GetCurrentToken();
            
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                // Use proxy URL instead of direct MinIO URL
                // Include token in query parameter for browser access via <img> tag
                var url = string.IsNullOrEmpty(token)
                    ? $"{baseUrl}/api/upload/document/{documentId}/asset/{fileName}"
                    : $"{baseUrl}/api/upload/document/{documentId}/asset/{fileName}?token={Uri.EscapeDataString(token)}";
                
                assets.Add(new AssetInfoDTO
                {
                    FileName = fileName,
                    Path = file,
                    Url = url
                });
            }

            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assets for document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить изображение через proxy (для безопасности и доступа из браузера)
    /// Supports authentication via Authorization header or token query parameter
    /// </summary>
    [HttpGet("document/{documentId}/asset/{fileName}")]
    [AllowAnonymous] // Override [Authorize] at class level
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsset(Guid documentId, string fileName)
    {
        try
        {
            _logger.LogInformation("GetAsset called: documentId={DocumentId}, fileName={FileName}, hasToken={HasToken}", 
                documentId, fileName, Request.Query.ContainsKey("token"));
            
            // Get userId from token (query parameter or Authorization header)
            var userId = GetUserIdFromRequest();
            
            if (!userId.HasValue)
            {
                _logger.LogWarning("GetAsset unauthorized: documentId={DocumentId}, fileName={FileName}", documentId, fileName);
                return Unauthorized(new { message = "Необходима авторизация для доступа к изображению" });
            }

            _logger.LogInformation("GetAsset: userId extracted: {UserId}, checking document existence", userId.Value);

            // Проверяем существование документа
            var documentExists = await _documentService.DocumentExistsAsync(documentId, userId.Value);
            _logger.LogInformation("GetAsset: document exists: {Exists}", documentExists);
            
            if (!documentExists)
            {
                _logger.LogWarning("GetAsset: Document not found: documentId={DocumentId}, userId={UserId}", documentId, userId.Value);
                return NotFound(new { message = "Документ не найден" });
            }

            var bucket = GetUserBucket(userId.Value);
            var objectPath = $"documents/{documentId}/assets/{fileName}";
            
            _logger.LogInformation("GetAsset: checking file existence: bucket={Bucket}, path={Path}", bucket, objectPath);

            var fileExists = await _minioService.FileExistsAsync(bucket, objectPath);
            _logger.LogInformation("GetAsset: file exists: {Exists}", fileExists);
            
            if (!fileExists)
            {
                _logger.LogWarning("GetAsset: File not found: bucket={Bucket}, path={Path}", bucket, objectPath);
                return NotFound(new { message = "Файл не найден" });
            }

            _logger.LogInformation("GetAsset: downloading file from MinIO");
            
            // Download from MinIO and stream to client
            var stream = await _minioService.DownloadFileAsync(bucket, objectPath);
            var contentType = GetContentType(fileName);
            
            _logger.LogInformation("GetAsset success: documentId={DocumentId}, fileName={FileName}, contentType={ContentType}, streamLength={StreamLength}", 
                documentId, fileName, contentType, stream?.Length ?? 0);
            
            // Set cache headers for images (1 hour cache)
            Response.Headers["Cache-Control"] = "public, max-age=3600";
            
            // CORS headers are handled by global CORS policy in Program.cs
            // Don't set them manually here to avoid conflicts
            
            return File(stream, contentType, fileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Необходима авторизация для доступа к изображению" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset {FileName} for document {DocumentId}", fileName, documentId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}

public class UploadAssetResponseDTO
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public class AssetInfoDTO
{
    public string FileName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
