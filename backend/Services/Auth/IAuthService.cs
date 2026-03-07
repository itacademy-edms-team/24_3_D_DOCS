using RusalProject.Models.DTOs;
using RusalProject.Models.DTOs.Auth;

using System.Collections.Generic;

namespace RusalProject.Services.Auth;

public interface IAuthService
{
    /// <summary>
    /// Отправка кода верификации на email
    /// </summary>
    Task SendVerificationCodeAsync(SendVerificationCodeRequestDTO request);

    /// <summary>
    /// Регистрация нового пользователя с проверкой кода
    /// </summary>
    Task<LoginResponseDTO> RegisterAsync(VerifyEmailRequestDTO request);

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

    /// <summary>
    /// Удаление текущего пользователя: Redis (сессии и регистрация), MinIO-бакет, строка в БД.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Пользователь не найден в БД.</exception>
    Task DeleteCurrentUserAsync(Guid userId);
}

