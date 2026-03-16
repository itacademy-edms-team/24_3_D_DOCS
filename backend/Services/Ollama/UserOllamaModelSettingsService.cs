using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.AI;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Ollama;

public class UserOllamaModelSettingsService : IUserOllamaModelSettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserOllamaModelResolutionService _resolution;

    public UserOllamaModelSettingsService(
        ApplicationDbContext context,
        IUserOllamaModelResolutionService resolution)
    {
        _context = context;
        _resolution = resolution;
    }

    public async Task<IReadOnlyList<LlmModelOptionDTO>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LlmModels.AsNoTracking()
            .OrderBy(m => m.ModelName)
            .Select(m => new LlmModelOptionDTO
            {
                ModelName = m.ModelName,
                HasView = m.HasView
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OllamaModelPreferencesResponseDTO> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var prefs = await _context.UserOllamaModelPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        var effectiveAgent = await _resolution.GetAgentModelAsync(userId, cancellationToken);
        var effectiveAttachment = await _resolution.GetAttachmentTextModelAsync(userId, cancellationToken);
        var effectiveVision = await _resolution.GetVisionModelAsync(userId, cancellationToken);

        return new OllamaModelPreferencesResponseDTO
        {
            AgentModelName = prefs?.AgentModelName,
            AttachmentTextModelName = prefs?.AttachmentTextModelName,
            VisionModelName = prefs?.VisionModelName,
            EffectiveAgentModel = effectiveAgent,
            EffectiveAttachmentTextModel = effectiveAttachment,
            EffectiveVisionModel = effectiveVision
        };
    }

    public async Task SavePreferencesAsync(
        Guid userId,
        SetOllamaModelPreferencesDTO dto,
        CancellationToken cancellationToken = default)
    {
        static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        var agent = Normalize(dto.AgentModelName);
        var attachment = Normalize(dto.AttachmentTextModelName);
        var vision = Normalize(dto.VisionModelName);

        if (agent is not null)
            await EnsureModelExistsAsync(agent, requireView: false, cancellationToken);
        if (attachment is not null)
            await EnsureModelExistsAsync(attachment, requireView: false, cancellationToken);
        if (vision is not null)
            await EnsureModelExistsAsync(vision, requireView: true, cancellationToken);

        var entity = await _context.UserOllamaModelPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (agent is null && attachment is null && vision is null)
        {
            if (entity != null)
                _context.UserOllamaModelPreferences.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (entity == null)
        {
            entity = new UserOllamaModelPreferences { UserId = userId };
            _context.UserOllamaModelPreferences.Add(entity);
        }

        entity.AgentModelName = agent;
        entity.AttachmentTextModelName = attachment;
        entity.VisionModelName = vision;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureModelExistsAsync(string modelName, bool requireView, CancellationToken cancellationToken)
    {
        var row = await _context.LlmModels.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ModelName == modelName, cancellationToken);

        if (row == null)
            throw new InvalidOperationException($"Неизвестная модель: {modelName}.");

        if (requireView && !row.HasView)
            throw new InvalidOperationException($"Модель «{modelName}» не поддерживает работу с изображениями.");
    }
}
