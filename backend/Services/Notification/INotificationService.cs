using RusalProject.Models.DTOs.Notification;

namespace RusalProject.Services.Notification;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationListItemDto>> ListForUserAsync(Guid userId, CancellationToken ct = default);
    Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task ClearAllForUserAsync(Guid userId, CancellationToken ct = default);
}
