using System.ComponentModel.DataAnnotations;

namespace RusalProject.Models.DTOs.Auth;

public class RefreshTokenRequestDTO
{
    [Required(ErrorMessage = "Refresh token обязателен")]
    public string RefreshToken { get; set; } = string.Empty;
}

