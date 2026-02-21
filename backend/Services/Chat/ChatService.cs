using System.Linq;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Models.Entities;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Chat;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChatService> _logger;

    public ChatService(ApplicationDbContext context, ILogger<ChatService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ChatSessionDTO>> GetChatsByDocumentAsync(Guid documentId, Guid userId, bool includeArchived = false)
    {
        try
        {
            _logger.LogDebug("Getting chats for document {DocumentId}, user {UserId}, includeArchived={IncludeArchived}", 
                documentId, userId, includeArchived);

            var query = _context.ChatSessions
                .Where(c => c.Scope == ChatScope.Document && c.DocumentId == documentId && c.UserId == userId && c.DeletedAt == null);

            if (!includeArchived)
            {
                query = query.Where(c => !c.IsArchived);
            }

            var chats = await query
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            _logger.LogDebug("Found {Count} chats", chats.Count);

            // Если чатов нет, возвращаем пустой список
            if (!chats.Any())
            {
                return new List<ChatSessionDTO>();
            }

            // Получаем количество сообщений для каждого чата
            var chatIds = chats.Select(c => c.Id).ToList();
            Dictionary<Guid, int> messageCounts;
            
            try
            {
                messageCounts = await _context.ChatMessages
                    .Where(m => chatIds.Contains(m.ChatSessionId))
                    .GroupBy(m => m.ChatSessionId)
                    .Select(g => new { ChatSessionId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ChatSessionId, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting message counts, using empty dictionary");
                messageCounts = new Dictionary<Guid, int>();
            }

            var result = chats.Select(c => new ChatSessionDTO
            {
                Id = c.Id,
                Scope = c.Scope,
                DocumentId = c.DocumentId,
                Title = c.Title,
                IsArchived = c.IsArchived,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                MessageCount = messageCounts.GetValueOrDefault(c.Id, 0)
            }).ToList();

            _logger.LogDebug("Returning {Count} chat DTOs", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetChatsByDocumentAsync: {Error}", ex.Message);
            throw;
        }
    }

    public async Task<ChatSessionWithMessagesDTO?> GetChatByIdAsync(Guid chatId, Guid userId)
    {
        var chat = await _context.ChatSessions
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId && c.DeletedAt == null);

        if (chat == null)
        {
            return null;
        }

        return new ChatSessionWithMessagesDTO
        {
            Id = chat.Id,
            Scope = chat.Scope,
            DocumentId = chat.DocumentId,
            Title = chat.Title,
            IsArchived = chat.IsArchived,
            CreatedAt = chat.CreatedAt,
            UpdatedAt = chat.UpdatedAt,
            Messages = chat.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageDTO
                {
                    Id = m.Id,
                    Role = m.Role,
                    Content = m.Content,
                    StepNumber = m.StepNumber,
                    ToolCalls = m.ToolCalls,
                    CreatedAt = m.CreatedAt
                }).ToList()
        };
    }

    public async Task<ChatSessionDTO> CreateChatAsync(CreateChatSessionDTO dto, Guid userId)
    {
        var scope = dto.Scope;

        if (scope == ChatScope.Document)
        {
            if (!dto.DocumentId.HasValue)
                throw new ArgumentException("DocumentId обязателен при scope=Document");

            var documentExists = await _context.DocumentLinks
                .AnyAsync(d => d.Id == dto.DocumentId && d.CreatorId == userId && d.DeletedAt == null);
            if (!documentExists)
                throw new UnauthorizedAccessException("Document not found or access denied");
        }
        else if (scope == ChatScope.Global)
        {
            if (dto.DocumentId.HasValue)
                throw new ArgumentException("DocumentId должен быть null при scope=Global");
        }

        var title = dto.Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            if (scope == ChatScope.Document)
            {
                var chatCount = await _context.ChatSessions
                    .CountAsync(c => c.Scope == ChatScope.Document && c.DocumentId == dto.DocumentId && c.UserId == userId && c.DeletedAt == null);
                title = chatCount == 0 ? "Новый чат" : $"Chat {chatCount + 1}";
            }
            else
            {
                var chatCount = await _context.ChatSessions
                    .CountAsync(c => c.Scope == ChatScope.Global && c.UserId == userId && c.DeletedAt == null);
                title = chatCount == 0 ? "Главный чат" : $"Чат {chatCount + 1}";
            }
        }

        var chat = new ChatSession
        {
            Scope = scope,
            DocumentId = dto.DocumentId,
            UserId = userId,
            Title = title,
            IsArchived = false
        };

        _context.ChatSessions.Add(chat);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Chat created: ChatId={ChatId}, Scope={Scope}, DocumentId={DocumentId}, UserId={UserId}", 
            chat.Id, scope, dto.DocumentId, userId);

        return new ChatSessionDTO
        {
            Id = chat.Id,
            Scope = chat.Scope,
            DocumentId = chat.DocumentId,
            Title = chat.Title,
            IsArchived = chat.IsArchived,
            CreatedAt = chat.CreatedAt,
            UpdatedAt = chat.UpdatedAt,
            MessageCount = 0
        };
    }

    public async Task<ChatSessionDTO> UpdateChatAsync(Guid chatId, UpdateChatSessionDTO dto, Guid userId)
    {
        var chat = await _context.ChatSessions
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId && c.DeletedAt == null);

        if (chat == null)
        {
            throw new UnauthorizedAccessException("Chat not found or access denied");
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            chat.Title = dto.Title;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Chat updated: ChatId={ChatId}, Title={Title}", chatId, chat.Title);

        return new ChatSessionDTO
        {
            Id = chat.Id,
            Scope = chat.Scope,
            DocumentId = chat.DocumentId,
            Title = chat.Title,
            IsArchived = chat.IsArchived,
            CreatedAt = chat.CreatedAt,
            UpdatedAt = chat.UpdatedAt,
            MessageCount = await _context.ChatMessages.CountAsync(m => m.ChatSessionId == chatId)
        };
    }

    public async Task<List<ChatSessionDTO>> GetChatsByScopeAsync(Guid userId, ChatScope scope, Guid? documentId, bool includeArchived = false)
    {
        var query = _context.ChatSessions
            .Where(c => c.Scope == scope && c.UserId == userId && c.DeletedAt == null);

        if (scope == ChatScope.Document && documentId.HasValue)
            query = query.Where(c => c.DocumentId == documentId);

        if (scope == ChatScope.Document && !documentId.HasValue)
            return new List<ChatSessionDTO>();

        if (!includeArchived)
            query = query.Where(c => !c.IsArchived);

        var chats = await query.OrderByDescending(c => c.UpdatedAt).ToListAsync();
        var chatIds = chats.Select(c => c.Id).ToList();

        Dictionary<Guid, int> messageCounts;
        try
        {
            messageCounts = await _context.ChatMessages
                .Where(m => chatIds.Contains(m.ChatSessionId))
                .GroupBy(m => m.ChatSessionId)
                .Select(g => new { ChatSessionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ChatSessionId, x => x.Count);
        }
        catch
        {
            messageCounts = new Dictionary<Guid, int>();
        }

        return chats.Select(c => new ChatSessionDTO
        {
            Id = c.Id,
            Scope = c.Scope,
            DocumentId = c.DocumentId,
            Title = c.Title,
            IsArchived = c.IsArchived,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            MessageCount = messageCounts.GetValueOrDefault(c.Id, 0)
        }).ToList();
    }

    public async Task ArchiveChatAsync(Guid chatId, Guid userId)
    {
        var chat = await _context.ChatSessions
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId && c.DeletedAt == null);

        if (chat == null)
        {
            throw new UnauthorizedAccessException("Chat not found or access denied");
        }

        chat.IsArchived = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Chat archived: ChatId={ChatId}", chatId);
    }

    public async Task RestoreChatAsync(Guid chatId, Guid userId)
    {
        var chat = await _context.ChatSessions
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId && c.DeletedAt == null);

        if (chat == null)
        {
            throw new UnauthorizedAccessException("Chat not found or access denied");
        }

        chat.IsArchived = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Chat restored: ChatId={ChatId}", chatId);
    }

    public async Task DeleteChatPermanentlyAsync(Guid chatId, Guid userId)
    {
        var chat = await _context.ChatSessions
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);

        if (chat == null)
        {
            throw new UnauthorizedAccessException("Chat not found or access denied");
        }

        _context.ChatSessions.Remove(chat);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Chat deleted permanently: ChatId={ChatId}", chatId);
    }

    public async Task AddMessageAsync(Guid chatId, ChatMessageDTO message, Guid userId)
    {
        var chat = await _context.ChatSessions
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId && c.DeletedAt == null);

        if (chat == null)
        {
            throw new UnauthorizedAccessException("Chat not found or access denied");
        }

        var chatMessage = new ChatMessage
        {
            ChatSessionId = chatId,
            Role = message.Role,
            Content = message.Content,
            StepNumber = message.StepNumber,
            ToolCalls = message.ToolCalls
        };

        _context.ChatMessages.Add(chatMessage);
        // Keep chat ordering stable: every new message should bump chat activity timestamp.
        chat.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogDebug("Message added to chat: ChatId={ChatId}, Role={Role}, ContentLength={ContentLength}", 
            chatId, message.Role, message.Content.Length);
    }
}
