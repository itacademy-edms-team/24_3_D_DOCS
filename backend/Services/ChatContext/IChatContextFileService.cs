using RusalProject.Models.DTOs.Chat;

namespace RusalProject.Services.ChatContext;

public interface IChatContextFileService
{
    Task<ChatContextFileDTO> UploadAsync(Guid chatId, Guid userId, IFormFile file, CancellationToken cancellationToken = default);
    Task<List<ChatContextFileDTO>> ListAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid fileId, Guid userId, CancellationToken cancellationToken = default);
    Task<string> GetContextForPromptAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
}
