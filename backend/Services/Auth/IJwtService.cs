using System.Security.Claims;

namespace RusalProject.Services.Auth;

public interface IJwtService
{
    /// <summary>
    /// Генерирует Access Token (JWT)
    /// </summary>
    string GenerateAccessToken(Guid userId, string email, string role);

    /// <summary>
    /// Генерирует Refresh Token (GUID)
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Валидирует JWT токен и возвращает claims
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Получает UserId из токена
    /// </summary>
    Guid? GetUserIdFromToken(string token);

    /// <summary>
    /// Получает JTI (JWT ID) из токена
    /// </summary>
    string? GetJtiFromToken(string token);

    /// <summary>
    /// Получает время истечения токена
    /// </summary>
    DateTime GetTokenExpiration();

    /// <summary>
    /// Получает время истечения Refresh Token
    /// </summary>
    DateTime GetRefreshTokenExpiration();
}

