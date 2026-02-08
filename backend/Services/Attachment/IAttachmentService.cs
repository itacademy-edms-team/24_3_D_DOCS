using RusalProject.Models.Entities;
using AttachmentEntity = RusalProject.Models.Entities.Attachment;

namespace RusalProject.Services.Attachment;

public interface IAttachmentService
{
    Task<AttachmentEntity?> GetAttachmentAsync(Guid id, Guid userId);
    Task<List<AttachmentEntity>> ListAttachmentsAsync(Guid userId, string? type = null, string sort = "modified_desc");
    Task<Stream> DownloadAttachmentAsync(Guid id, Guid userId);
    Task RenameAttachmentAsync(Guid id, Guid userId, string newName);
    Task DeleteAttachmentAsync(Guid id, Guid userId);
    Task<string> GetPresignedUrlAsync(Guid id, Guid userId, int expirySeconds = 3600);
}

