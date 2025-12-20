namespace RusalProject.Models.DTOs.Documents;

public class DocumentMetaDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ProfileId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
