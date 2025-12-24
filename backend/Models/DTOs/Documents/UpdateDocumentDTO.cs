namespace RusalProject.Models.DTOs.Documents;

public class UpdateDocumentDTO
{
    public string? Name { get; set; }
    public Guid? ProfileId { get; set; }
    public Guid? TitlePageId { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, object>? Overrides { get; set; }
}
