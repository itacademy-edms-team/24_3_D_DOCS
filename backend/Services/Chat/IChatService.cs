using RusalProject.Models.DTOs.Chat;
using RusalProject.Models.Types;

namespace RusalProject.Services.Chat;

public interface IChatService
{
    Task<List<ChatSessionDTO>> GetChatsByDocumentAsync(Guid documentId, Guid userId, bool includeArchived = false);
    Task<List<ChatSessionDTO>> GetChatsByScopeAsync(Guid userId, ChatScope scope, Guid? documentId, bool includeArchived = false);
    Task<ChatSessionWithMessagesDTO?> GetChatByIdAsync(Guid chatId, Guid userId);
    Task<ChatSessionDTO> CreateChatAsync(CreateChatSessionDTO dto, Guid userId);
    Task<ChatSessionDTO> UpdateChatAsync(Guid chatId, UpdateChatSessionDTO dto, Guid userId);
    Task ArchiveChatAsync(Guid chatId, Guid userId);
    Task RestoreChatAsync(Guid chatId, Guid userId);
    Task DeleteChatPermanentlyAsync(Guid chatId, Guid userId);
    Task AddMessageAsync(Guid chatId, ChatMessageDTO message, Guid userId);
}
