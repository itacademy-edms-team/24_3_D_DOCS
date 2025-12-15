using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace RusalProject.Services.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateVerificationCode()
    {
        // Используем криптографически безопасный генератор
        return System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    public async Task SendVerificationCodeAsync(string toEmail, string code)
    {
        var emailSettings = _configuration.GetSection("Email");
        var enableEmail = emailSettings.GetValue<bool>("EnableEmailSending");

        // Для разработки - просто логируем код
        if (!enableEmail)
        {
            _logger.LogWarning("=== EMAIL VERIFICATION CODE ===");
            _logger.LogWarning($"To: {toEmail}");
            _logger.LogWarning($"Code: {code}");
            _logger.LogWarning("===============================");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["FromName"], 
                emailSettings["FromAddress"]
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Код подтверждения регистрации - Rusal Project";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                <h2 style='color: #667eea;'>Добро пожаловать в D_DOCS Project!</h2>
                                <p>Ваш код подтверждения регистрации:</p>
                                <div style='background: #f5f7fa; padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0;'>
                                    <h1 style='color: #667eea; font-size: 36px; margin: 0; letter-spacing: 8px;'>{code}</h1>
                                </div>
                                <p>Код действителен в течение 10 минут.</p>
                                <p style='color: #999; font-size: 12px; margin-top: 30px;'>
                                    Если вы не регистрировались на нашем сайте, проигнорируйте это письмо.
                                </p>
                            </div>
                        </body>
                    </html>",
                TextBody = $"Ваш код подтверждения: {code}\nКод действителен в течение 10 минут."
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            await client.ConnectAsync(
                emailSettings["SmtpHost"], 
                emailSettings.GetValue<int>("SmtpPort"), 
                SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(
                emailSettings["SmtpUsername"], 
                emailSettings["SmtpPassword"]
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Verification code sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send email to {toEmail}: {ex.Message}");
            throw new Exception("Не удалось отправить email. Попробуйте позже.");
        }
    }
}

