using RusalProject.Models.DTOs;

namespace RusalProject.Services.EditorHotkeys;

public interface IUserEditorHotkeysService
{
	Task<EditorHotkeysResponseDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

	Task SaveAsync(Guid userId, SetEditorHotkeysDto dto, CancellationToken cancellationToken = default);
}
