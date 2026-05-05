namespace RusalProject.Services.Document;

/// <summary>
/// Уведомляет клиентов редактора (SignalR), что содержимое документа на сервере изменилось.
/// </summary>
public interface IDocumentEditorRealtimeService
{
    Task NotifyDocumentContentChangedAsync(
        Guid documentId,
        Guid editorUserId,
        string editorDisplayName,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default);
}
