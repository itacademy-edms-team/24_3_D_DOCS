using RusalProject.Models.DTOs.TitlePages;

namespace RusalProject.Services.TitlePages;

public interface ITitlePageService
{
    Task<List<TitlePageMetaDTO>> GetAllTitlePagesAsync(Guid userId);
    Task<TitlePageDTO?> GetTitlePageByIdAsync(Guid id, Guid userId);
    Task<TitlePageDTO> CreateTitlePageAsync(CreateTitlePageDTO dto, Guid userId);
    Task<TitlePageDTO?> UpdateTitlePageAsync(Guid id, UpdateTitlePageDTO dto, Guid userId);
    Task<bool> DeleteTitlePageAsync(Guid id, Guid userId);
}
