using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.Entities;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;
using RusalProject.Services.Chat;
using RusalProject.Services.Storage;
using UglyToad.PdfPig;

namespace RusalProject.Services.AgentSources;

public class AgentSourceService : IAgentSourceService
{
    private static readonly JsonSerializerOptions AttachmentsJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;
    private readonly IChatService _chatService;
    private readonly ILogger<AgentSourceService> _logger;

    public AgentSourceService(
        ApplicationDbContext context,
        IMinioService minioService,
        IChatService chatService,
        ILogger<AgentSourceService> logger)
    {
        _context = context;
        _minioService = minioService;
        _chatService = chatService;
        _logger = logger;
    }

    private static string UserBucket(Guid userId) => $"user-{userId}";

    public async Task<AgentSourceIngestResponseDTO> IngestAsync(
        Guid userId,
        Guid? documentId,
        Guid chatSessionId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("Пустой файл.");

        if (file.Length > AgentSourceConstants.MaxUploadBytes)
            throw new InvalidOperationException($"Файл слишком большой (макс. {AgentSourceConstants.MaxUploadBytes / (1024 * 1024)} МБ).");

        var chat = await _chatService.GetChatByIdAsync(chatSessionId, userId)
            ?? throw new InvalidOperationException("Чат не найден.");

        Guid? sessionDocumentId;
        if (chat.Scope == ChatScope.Document)
        {
            if (!documentId.HasValue || documentId.Value == Guid.Empty)
                throw new InvalidOperationException("Для чата документа укажите documentId.");
            if (chat.DocumentId != documentId.Value)
                throw new InvalidOperationException("Чат не относится к указанному документу.");
            sessionDocumentId = documentId.Value;
        }
        else
        {
            if (documentId.HasValue)
                throw new InvalidOperationException("Для глобального чата не указывайте documentId.");
            if (chat.DocumentId.HasValue)
                throw new InvalidOperationException("Несогласованность чата: ожидался глобальный чат.");
            sessionDocumentId = null;
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AgentSourceConstants.AllowedExtensions.Contains(ext))
            throw new InvalidOperationException(
                "Недопустимый тип файла. Разрешены: PDF, TXT, MD, PNG, JPEG, WebP, GIF.");

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        var sessionId = Guid.NewGuid();
        var bucket = UserBucket(userId);
        await _minioService.EnsureBucketExistsAsync(bucket);

        var originalPath = $"agent-sources/{sessionId}/original{ext}";
        await using (var origUpload = new MemoryStream(bytes))
        {
            var origMime = string.IsNullOrWhiteSpace(file.ContentType)
                ? GuessMimeFromExtension(ext)
                : file.ContentType;
            await _minioService.UploadFileAsync(bucket, originalPath, origUpload, origMime);
        }

        var session = new AgentSourceSession
        {
            Id = sessionId,
            UserId = userId,
            DocumentId = sessionDocumentId,
            ChatSessionId = chatSessionId,
            OriginalFileName = Path.GetFileName(file.FileName),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow + AgentSourceConstants.SessionTtl,
            OriginalStoragePath = originalPath,
            OriginalContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? GuessMimeFromExtension(ext)
                : file.ContentType,
            OriginalSize = bytes.Length
        };

        var parts = new List<AgentSourcePart>();
        string? notes = null;
        var partIndex = 0;

        if (string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            var text = ExtractPdfText(bytes);
            if (string.IsNullOrWhiteSpace(text))
                notes = "Текст из PDF не извлечён (возможно, скан без OCR). Изображения страниц из PDF не извлекаются.";
            else
                notes = "Изображения, встроенные в PDF, на этом этапе не извлекаются; доступен только извлечённый текст.";

            partIndex = await AddTextPartAsync(parts, sessionId, bucket, partIndex, text ?? string.Empty, "Текст PDF", "application/pdf", cancellationToken);
        }
        else if (IsTextExtension(ext))
        {
            var text = ReadTextFile(bytes);
            partIndex = await AddTextPartAsync(parts, sessionId, bucket, partIndex, text, $"Текстовый файл ({ext.TrimStart('.')})", "text/plain", cancellationToken);
        }
        else if (IsImageExtension(ext))
        {
            if (bytes.Length > AgentSourceConstants.MaxImageBytes)
                throw new InvalidOperationException("Изображение слишком большое.");

            var mime = GuessImageMime(ext);
            var storagePath = $"agent-sources/{sessionId}/{partIndex}{ext}";
            await using var upload = new MemoryStream(bytes);
            await _minioService.UploadFileAsync(bucket, storagePath, upload, mime);

            parts.Add(new AgentSourcePart
            {
                SessionId = sessionId,
                PartIndex = partIndex++,
                Kind = nameof(AgentSourcePartKind.Image),
                MimeType = mime,
                Label = $"Изображение ({ext.TrimStart('.')})",
                StoragePath = storagePath
            });
        }
        else
            throw new InvalidOperationException("Неподдерживаемый формат.");

        if (parts.Count == 0)
            throw new InvalidOperationException("Не удалось подготовить содержимое файла.");

        session.Parts = parts;
        session.IngestNotes = notes;
        _context.AgentSourceSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return new AgentSourceIngestResponseDTO
        {
            SourceSessionId = sessionId,
            OriginalFileName = session.OriginalFileName,
            Notes = notes,
            Parts = parts
                .OrderBy(p => p.PartIndex)
                .Select(p => new AgentSourcePartSummaryDTO
                {
                    Index = p.PartIndex,
                    Kind = p.Kind,
                    Label = p.Label
                })
                .ToList()
        };
    }

    private async Task<int> AddTextPartAsync(
        List<AgentSourcePart> parts,
        Guid sessionId,
        string bucket,
        int partIndex,
        string text,
        string label,
        string logicalMime,
        CancellationToken cancellationToken)
    {
        var normalized = text.Replace("\r\n", "\n");
        if (normalized.Length <= AgentSourceConstants.MaxInlineTextChars)
        {
            parts.Add(new AgentSourcePart
            {
                SessionId = sessionId,
                PartIndex = partIndex,
                Kind = nameof(AgentSourcePartKind.Text),
                MimeType = logicalMime,
                Label = label,
                InlineText = normalized
            });
            return partIndex + 1;
        }

        var idx = partIndex;
        var storagePath = $"agent-sources/{sessionId}/{idx}.txt";
        var utf8 = Encoding.UTF8.GetBytes(normalized);
        await using var upload = new MemoryStream(utf8);
        await _minioService.UploadFileAsync(bucket, storagePath, upload, "text/plain; charset=utf-8");

        parts.Add(new AgentSourcePart
        {
            SessionId = sessionId,
            PartIndex = idx,
            Kind = nameof(AgentSourcePartKind.Text),
            MimeType = logicalMime,
            Label = $"{label} (большой файл)",
            StoragePath = storagePath
        });
        return partIndex + 1;
    }

    public async Task<AgentSourceSession?> GetValidatedSessionAsync(
        Guid userId,
        Guid sessionId,
        Guid chatSessionId,
        Guid? documentIdForContext,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var session = await _context.AgentSourceSessions
            .Include(s => s.Parts)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == sessionId
                     && s.UserId == userId
                     && s.ChatSessionId == chatSessionId
                     && s.ExpiresAt > now,
                cancellationToken);

        if (session == null)
            return null;

        if (documentIdForContext.HasValue)
        {
            if (session.DocumentId != documentIdForContext.Value)
                return null;
        }
        else
        {
            if (session.DocumentId != null)
                return null;
        }

        return session;
    }

    public async Task<string?> BuildAttachmentsJsonAsync(
        Guid userId,
        Guid chatSessionId,
        Guid? requestDocumentId,
        Guid sourceSessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await GetValidatedSessionAsync(userId, sourceSessionId, chatSessionId, requestDocumentId, cancellationToken);
        if (session == null)
            return null;

        var items = new[]
        {
            new
            {
                sourceSessionId = session.Id.ToString(),
                fileName = session.OriginalFileName,
                contentType = session.OriginalContentType ?? "application/octet-stream"
            }
        };
        return JsonSerializer.Serialize(items, AttachmentsJsonOptions);
    }

    public async Task<(Stream Stream, string FileName, string ContentType)?> GetOriginalFileAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var session = await _context.AgentSourceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == sessionId && s.UserId == userId && s.ExpiresAt > now,
                cancellationToken);

        if (session == null || string.IsNullOrEmpty(session.OriginalStoragePath))
            return null;

        var stream = await _minioService.DownloadFileAsync(UserBucket(userId), session.OriginalStoragePath);
        var fileName = string.IsNullOrEmpty(session.OriginalFileName) ? "download" : session.OriginalFileName;
        var ct = string.IsNullOrEmpty(session.OriginalContentType) ? "application/octet-stream" : session.OriginalContentType;
        return (stream, fileName, ct);
    }

    public string BuildCatalog(AgentSourceSession session, string? notes = null)
    {
        var note = notes ?? session.IngestNotes;
        var sb = new StringBuilder();
        sb.AppendLine("[Контекст вложений для текущего запроса]");
        sb.AppendLine($"Исходный файл: {session.OriginalFileName}");
        sb.AppendLine("Доступные части (для текста — query_attachment_text, для изображений — query_attachment_image):");
        foreach (var p in session.Parts.OrderBy(x => x.PartIndex))
            sb.AppendLine($"- индекс {p.PartIndex} ({p.Kind}): {p.Label}");
        if (!string.IsNullOrWhiteSpace(note))
            sb.AppendLine($"Примечание: {note}");
        return sb.ToString().TrimEnd();
    }

    public async Task<string> LoadPartTextAsync(AgentSourcePart part, Guid userId, CancellationToken cancellationToken = default)
    {
        if (part.KindEnum != AgentSourcePartKind.Text)
            throw new InvalidOperationException("Часть не является текстовой.");

        if (!string.IsNullOrEmpty(part.InlineText))
            return part.InlineText;

        if (string.IsNullOrEmpty(part.StoragePath))
            throw new InvalidOperationException("Текст части недоступен.");

        await using var s = await _minioService.DownloadFileAsync(UserBucket(userId), part.StoragePath);
        using var reader = new StreamReader(s, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public async Task<byte[]> LoadPartImageAsync(AgentSourcePart part, Guid userId, CancellationToken cancellationToken = default)
    {
        if (part.KindEnum != AgentSourcePartKind.Image)
            throw new InvalidOperationException("Часть не является изображением.");

        if (string.IsNullOrEmpty(part.StoragePath))
            throw new InvalidOperationException("Файл изображения недоступен.");

        await using var s = await _minioService.DownloadFileAsync(UserBucket(userId), part.StoragePath);
        await using var ms = new MemoryStream();
        await s.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    private string ExtractPdfText(byte[] bytes)
    {
        try
        {
            using var pdfMs = new MemoryStream(bytes);
            using var doc = PdfDocument.Open(pdfMs);
            var sb = new StringBuilder();
            foreach (var page in doc.GetPages())
            {
                sb.AppendLine(page.Text);
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PdfPig failed to extract text");
            return string.Empty;
        }
    }

    private static string ReadTextFile(byte[] bytes)
    {
        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private static bool IsTextExtension(string ext) =>
        ext.Equals(".txt", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".md", StringComparison.OrdinalIgnoreCase);

    private static bool IsImageExtension(string ext) =>
        ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".webp", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".gif", StringComparison.OrdinalIgnoreCase);

    private static string GuessImageMime(string ext) => ext.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };

    private static string GuessMimeFromExtension(string ext) => ext.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".txt" or ".md" => "text/plain; charset=utf-8",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };
}
