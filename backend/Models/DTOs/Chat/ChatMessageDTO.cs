namespace RusalProject.Models.DTOs.Chat;

public class ChatMessageDTO
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? StepNumber { get; set; }
    public string? ToolCalls { get; set; }
    public DateTime CreatedAt { get; set; }
}
