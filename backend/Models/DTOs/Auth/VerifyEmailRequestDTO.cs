using System.ComponentModel.DataAnnotations;

namespace RusalProject.Models.DTOs.Auth;

public class VerifyEmailRequestDTO
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Код подтверждения обязателен")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Код должен состоять из 6 символов")]
    public string Code { get; set; } = string.Empty;
}

