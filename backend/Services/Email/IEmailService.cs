namespace RusalProject.Services.Email;

public interface IEmailService
{
    Task SendVerificationCodeAsync(string toEmail, string code);
    string GenerateVerificationCode();
}

