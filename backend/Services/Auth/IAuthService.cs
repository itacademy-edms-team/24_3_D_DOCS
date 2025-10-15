using RusalProject.Models.DTOs;
using RusalProject.Models.DTOs.Auth;

namespace RusalProject.Services.Auth;

public interface IAuthService
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    Task<LoginResponseDTO> RegisterAsync(RegisterRequestDTO request);

    /// <summary>
    /// Вход пользователя
    /// </summary>
    Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request);

    /// <summary>
    /// Обновление токенов через Refresh Token
    /// </summary>
    Task<TokenResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO request);

    /// <summary>
    /// Выход пользователя (отзыв токенов)
    /// </summary>
    Task LogoutAsync(string accessToken, string refreshToken);

    /// <summary>
    /// Выход со всех устройств (удаление всех refresh токенов пользователя)
    /// </summary>
    Task LogoutAllAsync(Guid userId);
}

