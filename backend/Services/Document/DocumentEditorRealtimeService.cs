using Microsoft.AspNetCore.SignalR;
using RusalProject.Hubs;

namespace RusalProject.Services.Document;

public class DocumentEditorRealtimeService : IDocumentEditorRealtimeService
{
    private readonly IHubContext<DocumentEditorHub> _hubContext;
    private readonly ILogger<DocumentEditorRealtimeService> _logger;

    public DocumentEditorRealtimeService(
        IHubContext<DocumentEditorHub> hubContext,
        ILogger<DocumentEditorRealtimeService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyDocumentContentChangedAsync(
        Guid documentId,
        Guid editorUserId,
        string editorDisplayName,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients
                .Group(DocumentEditorHub.DocumentGroupName(documentId))
                .SendAsync(
                    "documentContentChanged",
                    new
                    {
                        documentId = documentId.ToString(),
                        editorUserId = editorUserId.ToString(),
                        editorDisplayName,
                        updatedAt = updatedAtUtc.ToString("o"),
                    },
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR: не удалось разослать обновление документа {DocumentId}", documentId);
        }
    }
}
