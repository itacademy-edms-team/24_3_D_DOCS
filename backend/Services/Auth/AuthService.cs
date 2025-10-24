using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RusalProject.Models.DTOs;
using RusalProject.Models.DTOs.Auth;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;
using RusalProject.Provider.Redis;
using RusalProject.Services.Email;

namespace RusalProject.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRedisService _redisService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IRedisService redisService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _redisService = redisService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(SendVerificationCodeRequestDTO request)
    {
        // Проверка существования пользователя
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            throw new InvalidOperationException("Пользователь с таким email уже существует");
        }

        // Генерируем код
        var code = _emailService.GenerateVerificationCode();
        
        // Хешируем пароль и сохраняем данные в Redis (на 10 минут)
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        await _redisService.SavePendingUserAsync(request.Email, passwordHash, request.Name, TimeSpan.FromMinutes(10));
        
        // Сохраняем код в Redis (на 10 минут)
        await _redisService.SaveVerificationCodeAsync(request.Email, code, TimeSpan.FromMinutes(10));
        
        // Отправляем email асинхронно (fire-and-forget) чтобы не блокировать ответ клиенту
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendVerificationCodeAsync(request.Email, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", request.Email);
            }
        });
    }

    public async Task<LoginResponseDTO> RegisterAsync(VerifyEmailRequestDTO request)
    {
        // Получаем сохраненный код
        var savedCode = await _redisService.GetVerificationCodeAsync(request.Email);
        
        if (savedCode == null)
        {
            throw new InvalidOperationException("Код верификации истек или не найден. Запросите новый код.");
        }

        if (savedCode != request.Code)
        {
            throw new InvalidOperationException("Неверный код верификации");
        }

        // Получаем данные пользователя из Redis
        var (passwordHash, name) = await _redisService.GetPendingUserAsync(request.Email);
        
        if (passwordHash == null || name == null)
        {
            throw new InvalidOperationException("Данные регистрации истекли. Начните регистрацию заново.");
        }

        // Проверяем еще раз, не зарегистрировался ли кто-то с этим email
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
            Name = name,
            PasswordHash = passwordHash,
            Role = "User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Удаляем данные из Redis
        await _redisService.DeleteVerificationCodeAsync(request.Email);
        await _redisService.DeletePendingUserAsync(request.Email);

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

        // Получаем userId по refresh token из mapping
        var userId = await _redisService.GetUserIdByRefreshTokenAsync(refreshToken);
        
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Невалидный или истекший refresh token");
        }

        // Проверяем, что refresh token существует в основном хранилище
        var tokenData = await _redisService.GetRefreshTokenAsync(userId.Value, refreshToken);
        
        if (tokenData == null)
        {
            throw new UnauthorizedAccessException("Невалидный или истекший refresh token");
        }

        // Получаем пользователя из БД
        var user = await _context.Users.FindAsync(userId.Value);
        
        if (user == null)
        {
            throw new UnauthorizedAccessException("Пользователь не найден");
        }

        // Удаляем старый refresh token
        await _redisService.DeleteRefreshTokenAsync(userId.Value, refreshToken);
        await _redisService.DeleteRefreshTokenMappingAsync(refreshToken);

        // Генерируем новые токены
        var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        
        var accessTokenExpiration = _jwtService.GetTokenExpiration();
        var refreshTokenExpiration = _jwtService.GetRefreshTokenExpiration();
        var refreshTokenTtl = refreshTokenExpiration - DateTime.UtcNow;

        // Сохраняем новый refresh token
        var metadata = $"{{\"userId\":\"{user.Id}\",\"createdAt\":\"{DateTime.UtcNow:O}\",\"rotated\":true}}";
        
        await _redisService.SaveRefreshTokenAsync(
            user.Id,
            newRefreshToken,
            refreshTokenTtl,
            metadata
        );
        
        await _redisService.SaveRefreshTokenMappingAsync(
            newRefreshToken,
            user.Id,
            refreshTokenTtl
        );

        return new TokenResponseDTO
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiration = accessTokenExpiration,
            RefreshTokenExpiration = refreshTokenExpiration
        };
    }

    public async Task LogoutAsync(string accessToken, string refreshToken)
    {
        // Получаем userId из access token
        // Примечание: если токен expired, logout всё равно должен работать
        Guid? userId;
        try
        {
            userId = _jwtService.GetUserIdFromToken(accessToken);
        }
        catch
        {
            // Если токен expired, пытаемся извлечь userId напрямую без валидации
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(accessToken);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            userId = Guid.TryParse(userIdClaim, out var id) ? id : null;
        }

        if (userId == null)
        {
            throw new UnauthorizedAccessException("Невалидный токен");
        }

        // Удаляем refresh token из Redis
        await _redisService.DeleteRefreshTokenAsync(userId.Value, refreshToken);
        await _redisService.DeleteRefreshTokenMappingAsync(refreshToken);
    }

    public async Task LogoutAllAsync(Guid userId)
    {
        // Получаем все refresh токены пользователя
        var refreshTokens = await _redisService.GetAllRefreshTokensByUserIdAsync(userId);
        
        // Удаляем все refresh токены и их mapping'и
        await _redisService.DeleteAllUserRefreshTokensAsync(userId);
        
        foreach (var token in refreshTokens)
        {
            await _redisService.DeleteRefreshTokenMappingAsync(token);
        }
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
        
        // Сохранение mapping refresh_token → userId для быстрого поиска
        await _redisService.SaveRefreshTokenMappingAsync(
            refreshToken,
            user.Id,
            refreshTokenTtl
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

