namespace RusalProject.Models.DTOs.Auth;

public class LoginResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiration { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
    public UserDTO User { get; set; } = null!;
}

