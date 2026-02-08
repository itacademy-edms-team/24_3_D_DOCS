using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

namespace RusalProject.Services.Auth;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        _issuer = _configuration["Jwt:Issuer"] ?? "RusalProject";
        _audience = _configuration["Jwt:Audience"] ?? "RusalProject-Client";
        _accessTokenExpirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "30");
        _refreshTokenExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");
    }

    public string GenerateAccessToken(Guid userId, string email, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        try
        {
            // Try to read token without validation first to get claims for logging
            var unvalidatedToken = tokenHandler.ReadJwtToken(token);
            var expClaim = unvalidatedToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            var issClaim = unvalidatedToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iss)?.Value;
            var audClaim = unvalidatedToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Aud)?.Value;
            
            if (expClaim != null && long.TryParse(expClaim, out var expUnix))
            {
                var expTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).DateTime;
                var now = DateTime.UtcNow;
                _logger.LogInformation("ValidateToken: Token exp={Exp}, now={Now}, expired={Expired}, issuer={Issuer}, audience={Audience}", 
                    expTime, now, expTime < now, issClaim, audClaim);
            }

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            _logger.LogInformation("ValidateToken: Token validated successfully");
            return principal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "ValidateToken: Token expired");
            return null;
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogWarning(ex, "ValidateToken: Invalid issuer. Expected: {ExpectedIssuer}, Got: {ActualIssuer}", _issuer, ex.InvalidIssuer);
            return null;
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            var invalidAudiences = ex.InvalidAudience != null ? string.Join(", ", ex.InvalidAudience) : "null";
            _logger.LogWarning(ex, "ValidateToken: Invalid audience. Expected: {ExpectedAudience}, Got: {ActualAudience}", _audience, invalidAudiences);
            return null;
        }
        catch (SecurityTokenSignatureKeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "ValidateToken: Signature key not found");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "ValidateToken: Invalid signature");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ValidateToken: Validation failed with unknown error: {ErrorType}", ex.GetType().Name);
            return null;
        }
    }

    public Guid? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        
        if (principal == null)
        {
            _logger.LogWarning("GetUserIdFromToken: ValidateToken returned null");
            return null;
        }
        
        // Log all claims for debugging
        var allClaims = principal.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
        _logger.LogInformation("GetUserIdFromToken: Found {ClaimCount} claims: {Claims}", allClaims.Count, string.Join(", ", allClaims));
        
        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        
        if (userIdClaim == null)
        {
            // Try alternative claim names
            userIdClaim = principal.FindFirst("sub")?.Value 
                       ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("GetUserIdFromToken: Sub claim not found, trying alternatives. Found: {UserIdClaim}", userIdClaim ?? "null");
        }
        
        if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogInformation("GetUserIdFromToken: Successfully extracted userId: {UserId}", userId);
            return userId;
        }

        _logger.LogWarning("GetUserIdFromToken: Could not parse userId from claim value: {UserIdClaim}", userIdClaim ?? "null");
        return null;
    }

    public string? GetJtiFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    }

    public DateTime GetTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);
    }

    public DateTime GetRefreshTokenExpiration()
    {
        return DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);
    }
}

