using Microsoft.EntityFrameworkCore;
using RusalProject.Provider.Database;
using RusalProject.Models.Entities;
using RusalProject.Services.Storage;
using AttachmentEntity = RusalProject.Models.Entities.Attachment;

namespace RusalProject.Services.Attachment;

public class AttachmentService : IAttachmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(
        ApplicationDbContext context,
        IMinioService minioService,
        ILogger<AttachmentService> logger)
    {
        _context = context;
        _minioService = minioService;
        _logger = logger;
    }

    private string GetUserBucket(Guid userId) => $"user-{userId}";

    public async Task<AttachmentEntity?> GetAttachmentAsync(Guid id, Guid userId)
    {
        return await _context.Attachments
            .FirstOrDefaultAsync(a => a.Id == id && a.CreatorId == userId && a.DeletedAt == null);
    }

    public async Task<List<AttachmentEntity>> ListAttachmentsAsync(Guid userId, string? type = null, string sort = "modified_desc")
    {
        var query = _context.Attachments
            .Where(a => a.CreatorId == userId && a.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (type == "image")
            {
                query = query.Where(a => a.ContentType.StartsWith("image/"));
            }
            else if (type == "pdf")
            {
                query = query.Where(a => a.ContentType == "application/pdf");
            }
        }

        query = sort switch
        {
            "modified_asc" => query.OrderBy(a => a.UpdatedAt),
            _ => query.OrderByDescending(a => a.UpdatedAt),
        };

        return await query.ToListAsync();
    }

    public async Task<Stream> DownloadAttachmentAsync(Guid id, Guid userId)
    {
        var attachment = await GetAttachmentAsync(id, userId);
        if (attachment == null)
            throw new FileNotFoundException($"Attachment {id} not found");

        var bucket = GetUserBucket(userId);
        var stream = await _minioService.DownloadFileAsync(bucket, attachment.StoragePath);
        return stream;
    }

    public async Task RenameAttachmentAsync(Guid id, Guid userId, string newName)
    {
        var attachment = await GetAttachmentAsync(id, userId);
        if (attachment == null)
            throw new FileNotFoundException($"Attachment {id} not found");

        attachment.FileName = newName;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAttachmentAsync(Guid id, Guid userId)
    {
        var attachment = await _context.Attachments
            .FirstOrDefaultAsync(a => a.Id == id && a.CreatorId == userId && a.DeletedAt == null);

        if (attachment == null)
            throw new FileNotFoundException($"Attachment {id} not found");

        // Soft delete
        attachment.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<string> GetPresignedUrlAsync(Guid id, Guid userId, int expirySeconds = 3600)
    {
        var attachment = await GetAttachmentAsync(id, userId);
        if (attachment == null)
            throw new FileNotFoundException($"Attachment {id} not found");

        var bucket = GetUserBucket(userId);
        return await _minioService.GetPresignedUrlAsync(bucket, attachment.StoragePath, expirySeconds);
    }
}
