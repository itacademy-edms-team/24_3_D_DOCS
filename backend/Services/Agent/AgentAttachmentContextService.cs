using System.Text.Json;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Services.AgentSources;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

public sealed class AgentAttachmentContextService : IAgentAttachmentContextService
{
    private readonly IAgentSourceService _agentSourceService;

    public AgentAttachmentContextService(IAgentSourceService agentSourceService)
    {
        _agentSourceService = agentSourceService;
    }

    public async Task<Guid?> ResolveAndInjectCatalogAsync(
        Guid userId,
        Guid chatSessionId,
        AgentAttachmentContextScope scope,
        Guid? documentId,
        Guid? requestSourceSessionId,
        IReadOnlyList<ChatMessageDTO> messagesOrderedByCreatedAt,
        List<OllamaMessageInput> history,
        CancellationToken cancellationToken = default)
    {
        var documentIdForValidation = scope == AgentAttachmentContextScope.Document ? documentId : null;
        if (scope == AgentAttachmentContextScope.Document && (!documentId.HasValue || documentId == Guid.Empty))
            throw new InvalidOperationException("DocumentId обязателен для контекста вложения документа.");

        if (requestSourceSessionId.HasValue)
        {
            var session = await _agentSourceService.GetValidatedSessionAsync(
                userId,
                requestSourceSessionId.Value,
                chatSessionId,
                documentIdForValidation,
                cancellationToken);

            if (session == null)
            {
                throw new InvalidOperationException(
                    scope == AgentAttachmentContextScope.Global
                        ? "Сессия вложения недействительна, истекла или не относится к этому глобальному чату."
                        : "Сессия вложения недействительна, истекла или не относится к этому чату и документу.");
            }

            InsertCatalog(history, _agentSourceService.BuildCatalog(session));
            return session.Id;
        }

        foreach (var sessionId in EnumerateAttachmentSessionIdsNewestFirst(messagesOrderedByCreatedAt))
        {
            var session = await _agentSourceService.GetValidatedSessionAsync(
                userId,
                sessionId,
                chatSessionId,
                documentIdForValidation,
                cancellationToken);

            if (session == null)
                continue;

            InsertCatalog(history, _agentSourceService.BuildCatalog(session));
            return session.Id;
        }

        return null;
    }

    internal static IEnumerable<Guid> EnumerateAttachmentSessionIdsNewestFirst(
        IReadOnlyList<ChatMessageDTO> messagesOrderedByCreatedAt)
    {
        foreach (var msg in messagesOrderedByCreatedAt
                     .Where(m => m.Role == "user" && !string.IsNullOrWhiteSpace(m.AttachmentsJson))
                     .OrderByDescending(m => m.CreatedAt))
        {
            foreach (var id in ParseAttachmentIdsNewestFirst(msg.AttachmentsJson!))
                yield return id;
        }
    }

    internal static IEnumerable<Guid> ParseAttachmentIdsNewestFirst(string attachmentsJson)
    {
        using var doc = JsonDocument.Parse(attachmentsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            yield break;

        var arr = doc.RootElement;
        var len = arr.GetArrayLength();
        for (var i = len - 1; i >= 0; i--)
        {
            var el = arr[i];
            if (el.ValueKind != JsonValueKind.Object)
                continue;
            if (!el.TryGetProperty("sourceSessionId", out var prop))
                continue;
            var s = prop.GetString();
            if (Guid.TryParse(s, out var g))
                yield return g;
        }
    }

    private static void InsertCatalog(List<OllamaMessageInput> history, string catalog)
    {
        var lastUserIdx = history.FindLastIndex(m => m.Role == "user");
        if (lastUserIdx >= 0)
        {
            history.Insert(
                lastUserIdx,
                new OllamaMessageInput { Role = "user", Content = catalog });
        }
        else
        {
            history.Add(new OllamaMessageInput { Role = "user", Content = catalog });
        }
    }
}
