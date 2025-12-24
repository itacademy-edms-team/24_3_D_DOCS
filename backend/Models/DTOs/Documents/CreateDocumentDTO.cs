namespace RusalProject.Models.DTOs.Documents;

public class CreateDocumentDTO
{
    public string Name { get; set; } = string.Empty;
    public Guid? ProfileId { get; set; }
    public Guid? TitlePageId { get; set; }
}
