using RusalProject.Models.DTOs.TitlePage;
using RusalProject.Models.Types;

namespace RusalProject.Services.TitlePage;

public interface ITitlePageService
{
    Task<TitlePageDTO> CreateTitlePageAsync(Guid userId, CreateTitlePageDTO dto);
    Task<TitlePageDTO?> GetTitlePageByIdAsync(Guid titlePageId, Guid userId);
    Task<TitlePageWithDataDTO?> GetTitlePageWithDataAsync(Guid titlePageId, Guid userId);
    Task<List<TitlePageDTO>> GetTitlePagesAsync(Guid userId);
    Task<TitlePageDTO> UpdateTitlePageAsync(Guid titlePageId, Guid userId, UpdateTitlePageDTO dto);
    Task DeleteTitlePageAsync(Guid titlePageId, Guid userId);

    /// <summary>Меняет тип элемента text → variable в JSON титульника (MinIO), стили и координаты сохраняются.</summary>
    Task<ConvertTitlePageElementToVariableResponse> ConvertTextElementToVariableAsync(
        Guid titlePageId,
        Guid userId,
        string elementId,
        ConvertTitlePageElementToVariableRequest? request);
    Task<Stream> ExportAsDdocAsync(Guid titlePageId, Guid userId);
}
