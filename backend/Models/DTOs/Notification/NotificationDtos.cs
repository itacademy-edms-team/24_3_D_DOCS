namespace RusalProject.Models.DTOs.Notification;

public class NotificationListItemDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public object? Payload { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
