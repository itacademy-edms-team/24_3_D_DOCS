using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs;
using RusalProject.Models.DTOs.Auth;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;
using RusalProject.Provider.Redis;

namespace RusalProject.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRedisService _redisService;

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IRedisService redisService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _redisService = redisService;
    }

    public async Task<LoginResponseDTO> RegisterAsync(RegisterRequestDTO request)
    {
        // Проверка существования пользователя
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            throw new InvalidOperationException("Пользователь с таким email уже существует");
        }

        // Создание нового пользователя
        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = "User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Генерация токенов
        return await GenerateTokensForUser(user);
    }

    public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request)
    {
        // Поиск пользователя
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        // Проверка пароля
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        // Генерация токенов
        return await GenerateTokensForUser(user);
    }

    public async Task<TokenResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO request)
    {
        var refreshToken = request.RefreshToken;

        // Поиск refresh token в Redis
        // Нужно получить userId из токена, но у нас только сам токен
        // Поэтому мы ищем по паттерну (не оптимально, но работает)
        // В production лучше хранить mapping токена на userId
        
        // Альтернативный подход: проверяем все пользователей (не оптимально!)
        // Лучше хранить отдельный индекс token -> userId в Redis
        
        // Для простоты, предположим что мы можем найти по паттерну
        // В реальности стоит добавить отдельное хранилище для маппинга
        
        throw new NotImplementedException("RefreshToken требует доработки архитектуры хранения");
        
        // TODO: Реализовать правильное хранение refresh токенов
        // Вариант 1: Хранить mapping refresh_token:userId отдельно
        // Вариант 2: Хранить refresh токены в БД
    }

    public async Task LogoutAsync(string accessToken, string refreshToken)
    {
        // Получаем userId и jti из access token
        var userId = _jwtService.GetUserIdFromToken(accessToken);
        var jti = _jwtService.GetJtiFromToken(accessToken);

        if (userId == null || jti == null)
        {
            throw new UnauthorizedAccessException("Невалидный токен");
        }

        // Добавляем access token в blacklist
        var tokenExpiration = _jwtService.GetTokenExpiration();
        var ttl = tokenExpiration - DateTime.UtcNow;
        
        if (ttl > TimeSpan.Zero)
        {
            await _redisService.AddToBlacklistAsync(jti, ttl);
        }

        // Удаляем refresh token из Redis
        await _redisService.DeleteRefreshTokenAsync(userId.Value, refreshToken);
    }

    public async Task LogoutAllAsync(Guid userId)
    {
        // Удаляем все refresh токены пользователя
        await _redisService.DeleteAllUserRefreshTokensAsync(userId);
    }

    // Private helper methods

    private async Task<LoginResponseDTO> GenerateTokensForUser(User user)
    {
        // Генерация Access Token
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var accessTokenExpiration = _jwtService.GetTokenExpiration();

        // Генерация Refresh Token
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiration = _jwtService.GetRefreshTokenExpiration();

        // Сохранение Refresh Token в Redis
        var refreshTokenTtl = refreshTokenExpiration - DateTime.UtcNow;
        var metadata = $"{{\"userId\":\"{user.Id}\",\"createdAt\":\"{DateTime.UtcNow:O}\"}}";
        
        await _redisService.SaveRefreshTokenAsync(
            user.Id,
            refreshToken,
            refreshTokenTtl,
            metadata
        );

        // Формирование ответа
        return new LoginResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiration = accessTokenExpiration,
            RefreshTokenExpiration = refreshTokenExpiration,
            User = new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            }
        };
    }
}

