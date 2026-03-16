using Microsoft.EntityFrameworkCore;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Ollama;

public class UserOllamaModelResolutionService : IUserOllamaModelResolutionService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public UserOllamaModelResolutionService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<string> GetAgentModelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var prefs = await GetPrefsAsync(userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(prefs?.AgentModelName))
            return prefs.AgentModelName.Trim();

        var fromConfig = _configuration["Ollama:DefaultModel"];
        return string.IsNullOrWhiteSpace(fromConfig) ? "gpt-oss:120b" : fromConfig.Trim();
    }

    public async Task<string> GetAttachmentTextModelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var prefs = await GetPrefsAsync(userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(prefs?.AttachmentTextModelName))
            return prefs.AttachmentTextModelName.Trim();

        var fromConfig = _configuration["Ollama:AttachmentTextModel"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig.Trim();

        return await GetAgentModelAsync(userId, cancellationToken);
    }

    public async Task<string> GetVisionModelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var prefs = await GetPrefsAsync(userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(prefs?.VisionModelName))
            return prefs.VisionModelName.Trim();

        var fromConfig = _configuration["Ollama:VisionModel"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig.Trim();

        return await GetAgentModelAsync(userId, cancellationToken);
    }

    private Task<UserOllamaModelPreferences?> GetPrefsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _context.UserOllamaModelPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}
