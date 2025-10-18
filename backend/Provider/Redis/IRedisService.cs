namespace RusalProject.Provider.Redis;

public interface IRedisService
{
    // Basic operations
    Task<string?> GetAsync(string key);
    Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);

    // Refresh Token operations
    Task<bool> SaveRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan expiry, string? metadata = null);
    Task<string?> GetRefreshTokenAsync(Guid userId, string refreshToken);
    Task<bool> DeleteRefreshTokenAsync(Guid userId, string refreshToken);
    Task<bool> DeleteAllUserRefreshTokensAsync(Guid userId);
    
    // Refresh Token Mapping operations (token → userId для быстрого поиска)
    Task<bool> SaveRefreshTokenMappingAsync(string refreshToken, Guid userId, TimeSpan expiry);
    Task<Guid?> GetUserIdByRefreshTokenAsync(string refreshToken);
    Task<bool> DeleteRefreshTokenMappingAsync(string refreshToken);
    Task<List<string>> GetAllRefreshTokensByUserIdAsync(Guid userId);

    // Blacklist operations (для Access Token)
    Task<bool> AddToBlacklistAsync(string jti, TimeSpan expiry);
    Task<bool> IsBlacklistedAsync(string jti);

    // Rate limiting operations
    Task<long> IncrementLoginAttemptsAsync(string identifier, TimeSpan expiry);
    Task<long> GetLoginAttemptsAsync(string identifier);
    Task<bool> ResetLoginAttemptsAsync(string identifier);
}

