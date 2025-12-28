namespace RusalProject.Models.DTOs.Chat;

public class CreateChatSessionDTO
{
    public Guid DocumentId { get; set; }
    public string? Title { get; set; }
}
