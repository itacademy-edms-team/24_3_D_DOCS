namespace RusalProject.Services.Auth;

public interface IPasswordHasher
{
    /// <summary>
    /// Хеширует пароль с использованием BCrypt
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Проверяет соответствие пароля хешу
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}

