namespace RusalProject.Models.DTOs.Chat;

public class ChatSessionWithMessagesDTO
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ChatMessageDTO> Messages { get; set; } = new();
}
