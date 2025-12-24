namespace RusalProject.Models.DTOs.TitlePages;

public class UpdateTitlePageDTO
{
    public string? Name { get; set; }
    public List<TitlePageElementDTO>? Elements { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
}
