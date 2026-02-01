using RusalProject.Models.DTOs.TitlePage;

namespace RusalProject.Services.TitlePages;

public interface ITitlePageService
{
    Task<List<TitlePageDTO>> GetTitlePagesAsync(Guid userId);
    Task<TitlePageWithDataDTO?> GetTitlePageWithDataAsync(Guid id, Guid userId);
    Task<TitlePageDTO?> GetTitlePageByIdAsync(Guid id, Guid userId);
    Task<TitlePageDTO> CreateTitlePageAsync(Guid userId, CreateTitlePageDTO dto);
    Task<TitlePageDTO> UpdateTitlePageAsync(Guid id, Guid userId, UpdateTitlePageDTO dto);
    Task DeleteTitlePageAsync(Guid id, Guid userId);
}
