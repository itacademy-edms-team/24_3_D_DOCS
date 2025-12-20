using RusalProject.Models.DTOs.Profiles;

namespace RusalProject.Services.Profiles;

public interface IProfileService
{
    Task<List<ProfileMetaDTO>> GetAllProfilesAsync(Guid userId);
    Task<ProfileDTO?> GetProfileByIdAsync(Guid id, Guid userId);
    Task<ProfileDTO> CreateProfileAsync(CreateProfileDTO dto, Guid userId);
    Task<ProfileDTO?> UpdateProfileAsync(Guid id, UpdateProfileDTO dto, Guid userId);
    Task<bool> DeleteProfileAsync(Guid id, Guid userId);
}
