namespace RusalProject.Models.DTOs.Documents;

public class DocumentDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ProfileId { get; set; }
    public Guid? TitlePageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Overrides { get; set; } = new();
    public Dictionary<string, string> Variables { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
