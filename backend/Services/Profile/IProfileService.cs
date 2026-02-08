using RusalProject.Models.DTOs.Profile;

namespace RusalProject.Services.Profile;

public interface IProfileService
{
    Task<ProfileDTO> CreateProfileAsync(Guid userId, CreateProfileDTO dto);
    Task<ProfileDTO?> GetProfileByIdAsync(Guid profileId, Guid userId);
    Task<ProfileWithDataDTO?> GetProfileWithDataAsync(Guid profileId, Guid userId);
    Task<List<ProfileDTO>> GetProfilesAsync(Guid userId, bool includePublic = true);
    Task<ProfileDTO> UpdateProfileAsync(Guid profileId, Guid userId, UpdateProfileDTO dto);
    Task DeleteProfileAsync(Guid profileId, Guid userId);
    Task<ProfileDTO> DuplicateProfileAsync(Guid profileId, Guid userId, string? newName = null);
    Task<bool> ProfileExistsAsync(Guid profileId, Guid userId);
}
