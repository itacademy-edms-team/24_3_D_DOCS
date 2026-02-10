using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Ollama;

public class UserOllamaApiKeyService : IUserOllamaApiKeyService
{
    private const string ProtectorPurpose = "Rusal.OllamaApiKey.v1";

    private readonly ApplicationDbContext _context;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UserOllamaApiKeyService> _logger;
    private readonly string _ollamaBaseUrl;

    public UserOllamaApiKeyService(
        ApplicationDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<UserOllamaApiKeyService> logger)
    {
        _context = context;
        _dataProtectionProvider = dataProtectionProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "https://ollama.com";
    }

    public async Task SetApiKeyAsync(Guid userId, string plainApiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plainApiKey))
        {
            throw new ArgumentException("API key cannot be empty", nameof(plainApiKey));
        }

        await ValidateApiKeyAsync(plainApiKey, cancellationToken);

        var protector = _dataProtectionProvider.CreateProtector(ProtectorPurpose);
        var encryptedKey = protector.Protect(plainApiKey);

        var existing = await _context.UserOllamaApiKeys
            .FirstOrDefaultAsync(k => k.UserId == userId, cancellationToken);

        if (existing != null)
        {
            existing.EncryptedKey = encryptedKey;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.UserOllamaApiKeys.Add(new Models.Entities.UserOllamaApiKey
            {
                UserId = userId,
                EncryptedKey = encryptedKey
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Ollama API key set for user {UserId}", userId);
    }

    public async Task<string?> GetDecryptedApiKeyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.UserOllamaApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.UserId == userId, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        try
        {
            var protector = _dataProtectionProvider.CreateProtector(ProtectorPurpose);
            return protector.Unprotect(entity.EncryptedKey);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            _logger.LogWarning("Failed to decrypt Ollama API key for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> HasApiKeyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOllamaApiKeys
            .AnyAsync(k => k.UserId == userId, cancellationToken);
    }

    public async Task RemoveApiKeyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.UserOllamaApiKeys
            .FirstOrDefaultAsync(k => k.UserId == userId, cancellationToken);

        if (entity != null)
        {
            _context.UserOllamaApiKeys.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Ollama API key removed for user {UserId}", userId);
        }
    }

    private async Task ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_ollamaBaseUrl.TrimEnd('/')}/api/chat";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(
                """{"model":"gpt-oss:120b","messages":[{"role":"user","content":"hi"}],"stream":false}""",
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Ollama API key validation failed: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException("API ключ недействителен. Проверьте ключ на ollama.com/settings/keys.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama API key validation failed: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException("Не удалось проверить API ключ. Попробуйте позже.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama API key validation error");
            throw new InvalidOperationException("Ошибка при проверке API ключа. Проверьте подключение к интернету.");
        }
    }
}
