using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;
using RusalProject.Services.Chat;
using RusalProject.Services.Storage;

namespace RusalProject.Services.ChatContext;

public class ChatContextFileService : IChatContextFileService
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly ApplicationDbContext _context;
    private readonly IChatService _chatService;
    private readonly IMinioService _minioService;
    private readonly TextFileParser _textParser;
    private readonly ImageParser _imageParser;
    private readonly ILogger<ChatContextFileService> _logger;

    public ChatContextFileService(
        ApplicationDbContext context,
        IChatService chatService,
        IMinioService minioService,
        TextFileParser textParser,
        ImageParser imageParser,
        ILogger<ChatContextFileService> logger)
    {
        _context = context;
        _chatService = chatService;
        _minioService = minioService;
        _textParser = textParser;
        _imageParser = imageParser;
        _logger = logger;
    }

    private static string GetUserBucket(Guid userId) => $"user-{userId}";

    public async Task<ChatContextFileDTO> UploadAsync(Guid chatId, Guid userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var chat = await _chatService.GetChatByIdAsync(chatId, userId);
        if (chat == null)
            throw new UnauthorizedAccessException("Чат не найден или доступ запрещён.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"Файл слишком большой. Максимум {MaxFileSizeBytes / 1024 / 1024} МБ.");

        var fileName = file.FileName;
        var ext = Path.GetExtension(fileName);
        if (!_textParser.CanParse(fileName) && !_imageParser.CanParse(fileName))
            throw new InvalidOperationException($"Неподдерживаемый тип файла: {ext}. Поддерживаются: txt, md, csv, log, pdf, jpg, png, gif, webp.");

        var fileId = Guid.NewGuid();
        var storagePath = $"chats/{chatId}/context/{fileId}{ext}";
        var bucket = GetUserBucket(userId);

        await _minioService.EnsureBucketExistsAsync(bucket);
        await using var uploadStream = file.OpenReadStream();
        await _minioService.UploadFileAsync(bucket, storagePath, uploadStream, file.ContentType);

        var entity = new ChatContextFile
        {
            ChatSessionId = chatId,
            UserId = userId,
            FileName = fileName,
            ContentType = file.ContentType,
            StoragePath = storagePath
        };
        _context.ChatContextFiles.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        string parsed;
        await using (var parseStream = file.OpenReadStream())
        {
            if (_textParser.CanParse(fileName))
                parsed = await _textParser.ParseAsync(parseStream, fileName, cancellationToken);
            else
                parsed = await _imageParser.ParseAsync(parseStream, userId, cancellationToken);
        }

        // PostgreSQL text не допускает \0 — убираем null-байты из вывода парсеров (часто в PDF)
        entity.ProcessedContent = parsed.Replace("\0", "");
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("ChatContextFile uploaded: {FileId}, chat={ChatId}", entity.Id, chatId);

        return MapToDto(entity);
    }

    public async Task<List<ChatContextFileDTO>> ListAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        var chat = await _chatService.GetChatByIdAsync(chatId, userId);
        if (chat == null)
            throw new UnauthorizedAccessException("Чат не найден или доступ запрещён.");

        var files = await _context.ChatContextFiles
            .Where(f => f.ChatSessionId == chatId && f.UserId == userId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        return files.Select(MapToDto).ToList();
    }

    public async Task DeleteAsync(Guid fileId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ChatContextFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId, cancellationToken);
        if (entity == null)
            throw new UnauthorizedAccessException("Файл не найден или доступ запрещён.");

        var bucket = GetUserBucket(userId);
        try
        {
            await _minioService.DeleteFileAsync(bucket, entity.StoragePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file from MinIO: {Path}", entity.StoragePath);
        }

        _context.ChatContextFiles.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GetContextForPromptAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        var allFilesForChat = await _context.ChatContextFiles
            .Where(f => f.ChatSessionId == chatId && f.UserId == userId)
            .ToListAsync(cancellationToken);

        var files = allFilesForChat
            .Where(f => !string.IsNullOrWhiteSpace(f.ProcessedContent))
            .OrderBy(f => f.CreatedAt)
            .ToList();

        var withoutContent = allFilesForChat.Count - files.Count;
        _logger.LogInformation(
            "ChatContextFile: GetContextForPrompt chatId={ChatId}, userId={UserId}, filesWithContent={Count}, filesWithoutContent={WithoutContent}, totalLength={Length}",
            chatId, userId, files.Count, withoutContent,
            files.Sum(f => f.ProcessedContent?.Length ?? 0));

        if (withoutContent > 0)
            _logger.LogWarning("ChatContextFile: {Count} file(s) in chat have empty ProcessedContent (parsing may have failed)", withoutContent);

        if (files.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var f in files)
        {
            sb.AppendLine($"--- Файл: {f.FileName} ---");
            sb.AppendLine(f.ProcessedContent);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private static ChatContextFileDTO MapToDto(ChatContextFile e) => new()
    {
        Id = e.Id,
        ChatSessionId = e.ChatSessionId,
        FileName = e.FileName,
        ContentType = e.ContentType,
        CreatedAt = e.CreatedAt
    };
}
