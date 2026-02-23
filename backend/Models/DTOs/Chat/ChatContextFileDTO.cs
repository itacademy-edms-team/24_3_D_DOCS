namespace RusalProject.Models.DTOs.Chat;

public class ChatContextFileDTO
{
    public Guid Id { get; set; }
    public Guid ChatSessionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
