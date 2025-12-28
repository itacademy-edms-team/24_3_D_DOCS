using RusalProject.Models.Types;

namespace RusalProject.Models.DTOs.Profile;

public class ProfileDTO
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProfileWithDataDTO : ProfileDTO
{
    public ProfileData Data { get; set; } = new();
}

public class CreateProfileDTO
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
    public ProfileData? Data { get; set; }
}

public class UpdateProfileDTO
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsPublic { get; set; }
    public ProfileData? Data { get; set; }
}
