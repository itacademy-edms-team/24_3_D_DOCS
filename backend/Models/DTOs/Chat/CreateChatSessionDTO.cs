using RusalProject.Models.Types;

namespace RusalProject.Models.DTOs.Chat;

public class CreateChatSessionDTO
{
    public ChatScope Scope { get; set; } = ChatScope.Document;
    public Guid? DocumentId { get; set; }
    public string? Title { get; set; }
}
