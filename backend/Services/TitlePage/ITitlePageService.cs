using RusalProject.Models.DTOs.TitlePage;

namespace RusalProject.Services.TitlePage;

public interface ITitlePageService
{
    Task<TitlePageDTO> CreateTitlePageAsync(Guid userId, CreateTitlePageDTO dto);
    Task<TitlePageDTO?> GetTitlePageByIdAsync(Guid titlePageId, Guid userId);
    Task<TitlePageWithDataDTO?> GetTitlePageWithDataAsync(Guid titlePageId, Guid userId);
    Task<List<TitlePageDTO>> GetTitlePagesAsync(Guid userId);
    Task<TitlePageDTO> UpdateTitlePageAsync(Guid titlePageId, Guid userId, UpdateTitlePageDTO dto);
    Task DeleteTitlePageAsync(Guid titlePageId, Guid userId);
}
