namespace RusalProject.Models.DTOs.Profiles;

public class UpdateProfileDTO
{
    public string? Name { get; set; }
    public ProfilePageDTO? Page { get; set; }
    public Dictionary<string, object>? Entities { get; set; }
}
