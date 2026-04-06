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
                                <h2 style='color: #2563eb;'>Добро пожаловать в D_DOCS Project!</h2>
                                <p>Ваш код подтверждения регистрации:</p>
                                <div style='background: #f5f7fa; padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0;'>
                                    <h1 style='color: #2563eb; font-size: 36px; margin: 0; letter-spacing: 8px;'>{code}</h1>
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
            
            // Настраиваем таймауты для SMTP клиента
            client.Timeout = 20000; // 20 секунд
            
            // Для тестирования: отключаем проверку сертификата (только для разработки!)
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            
            // Отключаем проверку имени сертификата для Gmail
            client.CheckCertificateRevocation = false;
            
            var smtpHost = emailSettings["SmtpHost"];
            var smtpPort = emailSettings.GetValue<int>("SmtpPort");
            var smtpUsername = emailSettings["SmtpUsername"];

            // Проверяем доступность хоста перед подключением
            try
            {
                var hostEntry = await System.Net.Dns.GetHostEntryAsync(smtpHost ?? throw new ArgumentNullException(nameof(smtpHost)));
                
                // Тестируем TCP соединение перед SSL handshake
                try
                {
                    using var tcpClient = new System.Net.Sockets.TcpClient();
                    var connectTask = tcpClient.ConnectAsync(hostEntry.AddressList[0], smtpPort);
                    var timeoutTask = Task.Delay(5000);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        throw new Exception($"Не удалось установить TCP соединение с {smtpHost}:{smtpPort} - таймаут");
                    }
                    
                    await connectTask;
                    tcpClient.Close();
                }
                catch (Exception tcpEx)
                {
                    throw new Exception($"Не удалось установить TCP соединение с {smtpHost}:{smtpPort}: {tcpEx.Message}");
                }
            }
            catch (Exception dnsEx)
            {
                throw new Exception($"Не удалось разрешить DNS для {smtpHost}: {dnsEx.Message}");
            }

            // Для SendGrid: порт 587 использует StartTLS, порт 465 использует SSL
            var secureOptions = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            try
            {
                await client.ConnectAsync(smtpHost, smtpPort, secureOptions);
            }
            catch (Exception)
            {
                throw;
            }

            await client.AuthenticateAsync(smtpUsername, emailSettings["SmtpPassword"]);

            await client.SendAsync(message);

            await client.DisconnectAsync(true);

            _logger.LogInformation($"Verification code sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}. Error: {ErrorMessage}", toEmail, ex.Message);
            _logger.LogError("SMTP Host: {Host}, Port: {Port}", 
                emailSettings["SmtpHost"], 
                emailSettings.GetValue<int>("SmtpPort"));
            throw new Exception($"Не удалось отправить email: {ex.Message}");
        }
    }
}

