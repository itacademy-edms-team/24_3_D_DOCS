using RusalProject.Models.DTOs.Chat;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

public interface IAgentAttachmentContextService
{
    /// <summary>
    /// Определяет активную сессию вложения: сначала <paramref name="requestSourceSessionId"/>, иначе последняя валидная из истории сообщений.
    /// При успехе вставляет каталог в <paramref name="history"/> перед последним сообщением пользователя.
    /// </summary>
    /// <returns>Идентификатор сессии для контекста инструментов вложения или null.</returns>
    /// <exception cref="InvalidOperationException">Явный <paramref name="requestSourceSessionId"/> не прошёл валидацию.</exception>
    Task<Guid?> ResolveAndInjectCatalogAsync(
        Guid userId,
        Guid chatSessionId,
        AgentAttachmentContextScope scope,
        Guid? documentId,
        Guid? requestSourceSessionId,
        IReadOnlyList<ChatMessageDTO> messagesOrderedByCreatedAt,
        List<OllamaMessageInput> history,
        CancellationToken cancellationToken = default);
}
