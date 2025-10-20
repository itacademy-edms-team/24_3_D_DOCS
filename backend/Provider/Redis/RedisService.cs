using StackExchange.Redis;

namespace RusalProject.Provider.Redis;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public RedisService(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _database = _redis.GetDatabase();
    }

    #region Basic Operations

    public async Task<string?> GetAsync(string key)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        return await _database.StringSetAsync(key, value, expiry);
    }

    public async Task<bool> DeleteAsync(string key)
    {
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _database.KeyExistsAsync(key);
    }

    #endregion

    #region Refresh Token Operations

    public async Task<bool> SaveRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan expiry, string? metadata = null)
    {
        var key = $"refresh_token:{userId}:{refreshToken}";
        var value = metadata ?? DateTime.UtcNow.ToString("O");
        return await _database.StringSetAsync(key, value, expiry);
    }

    public async Task<string?> GetRefreshTokenAsync(Guid userId, string refreshToken)
    {
        var key = $"refresh_token:{userId}:{refreshToken}";
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<bool> DeleteRefreshTokenAsync(Guid userId, string refreshToken)
    {
        var key = $"refresh_token:{userId}:{refreshToken}";
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> DeleteAllUserRefreshTokensAsync(Guid userId)
    {
        var pattern = $"refresh_token:{userId}:*";
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern).ToArray();
        
        if (keys.Length == 0)
            return false;

        await _database.KeyDeleteAsync(keys);
        return true;
    }

    #endregion

    #region Refresh Token Mapping Operations

    public async Task<bool> SaveRefreshTokenMappingAsync(string refreshToken, Guid userId, TimeSpan expiry)
    {
        var key = $"refresh_token_mapping:{refreshToken}";
        return await _database.StringSetAsync(key, userId.ToString(), expiry);
    }

    public async Task<Guid?> GetUserIdByRefreshTokenAsync(string refreshToken)
    {
        var key = $"refresh_token_mapping:{refreshToken}";
        var value = await _database.StringGetAsync(key);
        
        if (value.HasValue && Guid.TryParse(value.ToString(), out var userId))
        {
            return userId;
        }
        
        return null;
    }

    public async Task<bool> DeleteRefreshTokenMappingAsync(string refreshToken)
    {
        var key = $"refresh_token_mapping:{refreshToken}";
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<List<string>> GetAllRefreshTokensByUserIdAsync(Guid userId)
    {
        var pattern = $"refresh_token:{userId}:*";
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern).ToArray();
        
        var tokens = new List<string>();
        foreach (var key in keys)
        {
            // Извлекаем токен из ключа: refresh_token:{userId}:{token}
            var keyString = key.ToString();
            var parts = keyString.Split(':');
            if (parts.Length == 3)
            {
                tokens.Add(parts[2]);
            }
        }
        
        return tokens;
    }

    #endregion

    #region Blacklist Operations

    public async Task<bool> AddToBlacklistAsync(string jti, TimeSpan expiry)
    {
        var key = $"blacklist:{jti}";
        return await _database.StringSetAsync(key, "true", expiry);
    }

    public async Task<bool> IsBlacklistedAsync(string jti)
    {
        var key = $"blacklist:{jti}";
        return await _database.KeyExistsAsync(key);
    }

    #endregion

    #region Rate Limiting Operations

    public async Task<long> IncrementLoginAttemptsAsync(string identifier, TimeSpan expiry)
    {
        var key = $"login_attempts:{identifier}";
        var count = await _database.StringIncrementAsync(key);
        
        if (count == 1)
        {
            await _database.KeyExpireAsync(key, expiry);
        }
        
        return count;
    }

    public async Task<long> GetLoginAttemptsAsync(string identifier)
    {
        var key = $"login_attempts:{identifier}";
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? (long)value : 0;
    }

    public async Task<bool> ResetLoginAttemptsAsync(string identifier)
    {
        var key = $"login_attempts:{identifier}";
        return await _database.KeyDeleteAsync(key);
    }

    #endregion

    #region Email Verification Operations

    public async Task<bool> SaveVerificationCodeAsync(string email, string code, TimeSpan expiry)
    {
        var key = $"verification:{email.ToLower()}";
        return await _database.StringSetAsync(key, code, expiry);
    }

    public async Task<string?> GetVerificationCodeAsync(string email)
    {
        var key = $"verification:{email.ToLower()}";
        return await _database.StringGetAsync(key);
    }

    public async Task<bool> DeleteVerificationCodeAsync(string email)
    {
        var key = $"verification:{email.ToLower()}";
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> SavePendingUserAsync(string email, string passwordHash, string name, TimeSpan expiry)
    {
        var key = $"pending_user:{email.ToLower()}";
        var data = $"{passwordHash}|{name}";
        return await _database.StringSetAsync(key, data, expiry);
    }

    public async Task<(string? passwordHash, string? name)> GetPendingUserAsync(string email)
    {
        var key = $"pending_user:{email.ToLower()}";
        var data = await _database.StringGetAsync(key);
        
        if (data.IsNullOrEmpty)
            return (null, null);
        
        var parts = data.ToString().Split('|');
        if (parts.Length != 2)
            return (null, null);
        
        return (parts[0], parts[1]);
    }

    public async Task<bool> DeletePendingUserAsync(string email)
    {
        var key = $"pending_user:{email.ToLower()}";
        return await _database.KeyDeleteAsync(key);
    }

    #endregion
}

