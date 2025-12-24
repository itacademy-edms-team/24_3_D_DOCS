namespace RusalProject.Models.DTOs.TitlePages;

public class TitlePageDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<TitlePageElementDTO> Elements { get; set; } = new();
    public Dictionary<string, string> Variables { get; set; } = new();
}
