using RusalProject.Models.DTOs.Profile;

namespace RusalProject.Services.Profiles;

public interface IProfileService
{
    Task<List<ProfileDTO>> GetProfilesAsync(Guid userId, bool includePublic = true);
    Task<ProfileWithDataDTO?> GetProfileWithDataAsync(Guid id, Guid userId);
    Task<ProfileDTO> CreateProfileAsync(Guid userId, CreateProfileDTO dto);
    Task<ProfileDTO> UpdateProfileAsync(Guid id, Guid userId, UpdateProfileDTO dto);
    Task DeleteProfileAsync(Guid id, Guid userId);
    Task<ProfileDTO> DuplicateProfileAsync(Guid id, Guid userId, string? newName = null);
}
