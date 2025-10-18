using System.ComponentModel.DataAnnotations;

namespace RusalProject.Models.DTOs.Auth;

public class RegisterRequestDTO
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль должен быть минимум 6 символов")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имя обязательно")]
    [MinLength(2, ErrorMessage = "Имя должно быть минимум 2 символа")]
    [MaxLength(255, ErrorMessage = "Имя не должно превышать 255 символов")]
    public string Name { get; set; } = string.Empty;
}

