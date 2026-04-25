using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.Notification;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Notification;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<NotificationListItemDto>> ListForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        var list = new List<NotificationListItemDto>();
        foreach (var n in rows)
        {
            object? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<JsonElement>(n.PayloadJson);
            }
            catch
            {
                payload = null;
            }

            list.Add(new NotificationListItemDto
            {
                Id = n.Id,
                Type = n.Type,
                Payload = payload,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt,
            });
        }

        return list;
    }

    public async Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var n = await _db.UserNotifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && x.UserId == userId,
            ct);
        if (n == null)
            return;
        n.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ClearAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        await _db.UserNotifications
            .Where(n => n.UserId == userId)
            .ExecuteDeleteAsync(ct);
    }
}
