using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text.Json;
using System.Linq;

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

    private void WriteDebugLog(string location, string message, object? data = null, string hypothesisId = "")
    {
        try
        {
            var logEntry = new
            {
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString("N")[..8]}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location = location,
                message = message,
                data = data,
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = hypothesisId
            };
            var json = JsonSerializer.Serialize(logEntry);
            // Write to mounted volume path
            var logPath = "/app/.cursor/debug.log";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
            System.IO.File.AppendAllText(logPath, json + "\n");
            // Also log to standard logger for verification
            _logger.LogInformation("[DEBUG] {Location} - {Message} - Hypothesis: {HypothesisId}", location, message, hypothesisId);
        }
        catch (Exception ex)
        {
            // Fallback: also log to standard logger if file write fails
            _logger.LogWarning("Failed to write debug log: {Error}", ex.Message);
            _logger.LogWarning("Debug log exception details: {Type} - {StackTrace}", ex.GetType().Name, ex.StackTrace);
        }
    }

    public async Task SendVerificationCodeAsync(string toEmail, string code)
    {
        // #region agent log
        WriteDebugLog("EmailService.cs:24", "SendVerificationCodeAsync entry", new { toEmail, codeLength = code?.Length }, "A");
        // #endregion

        var emailSettings = _configuration.GetSection("Email");
        var enableEmail = emailSettings.GetValue<bool>("EnableEmailSending");

        // #region agent log
        WriteDebugLog("EmailService.cs:28", "Email settings loaded", new { enableEmail, smtpHost = emailSettings["SmtpHost"], smtpPort = emailSettings.GetValue<int>("SmtpPort") }, "B");
        // #endregion

        // Для разработки - просто логируем код
        if (!enableEmail)
        {
            _logger.LogWarning("=== EMAIL VERIFICATION CODE ===");
            _logger.LogWarning($"To: {toEmail}");
            _logger.LogWarning($"Code: {code}");
            _logger.LogWarning("===============================");
            // #region agent log
            WriteDebugLog("EmailService.cs:36", "Email sending disabled, logging code only", new { toEmail, code }, "A");
            // #endregion
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

            // #region agent log
            WriteDebugLog("EmailService.cs:72", "MimeMessage created, before SMTP connect", new { from = emailSettings["FromAddress"], to = toEmail }, "C");
            // #endregion

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
            var hasPassword = !string.IsNullOrEmpty(emailSettings["SmtpPassword"]);

            // #region agent log
            WriteDebugLog("EmailService.cs:81", "Before SMTP ConnectAsync", new { smtpHost, smtpPort, smtpUsername, hasPassword, secureSocketOptions = smtpPort == 465 ? "SslOnConnect" : "StartTls", timeout = 30000 }, "C");
            // #endregion

            // Проверяем доступность хоста перед подключением
            try
            {
                // #region agent log
                WriteDebugLog("EmailService.cs:86", "Testing DNS resolution", new { smtpHost }, "C");
                // #endregion
                var hostEntry = await System.Net.Dns.GetHostEntryAsync(smtpHost ?? throw new ArgumentNullException(nameof(smtpHost)));
                // #region agent log
                WriteDebugLog("EmailService.cs:89", "DNS resolution success", new { addresses = hostEntry.AddressList.Select(a => a.ToString()).ToArray() }, "C");
                // #endregion
                
                // Тестируем TCP соединение перед SSL handshake
                try
                {
                    // #region agent log
                    WriteDebugLog("EmailService.cs:95", "Testing TCP connection", new { smtpHost, smtpPort }, "C");
                    // #endregion
                    using var tcpClient = new System.Net.Sockets.TcpClient();
                    var connectTask = tcpClient.ConnectAsync(hostEntry.AddressList[0], smtpPort);
                    var timeoutTask = Task.Delay(5000);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        // #region agent log
                        WriteDebugLog("EmailService.cs:103", "TCP connection timeout", new { smtpHost, smtpPort }, "C");
                        // #endregion
                        throw new Exception($"Не удалось установить TCP соединение с {smtpHost}:{smtpPort} - таймаут");
                    }
                    
                    await connectTask;
                    // #region agent log
                    WriteDebugLog("EmailService.cs:109", "TCP connection successful", new { smtpHost, smtpPort, connected = tcpClient.Connected }, "C");
                    // #endregion
                    tcpClient.Close();
                }
                catch (Exception tcpEx)
                {
                    // #region agent log
                    WriteDebugLog("EmailService.cs:115", "TCP connection failed", new { error = tcpEx.Message, errorType = tcpEx.GetType().Name }, "C");
                    // #endregion
                    throw new Exception($"Не удалось установить TCP соединение с {smtpHost}:{smtpPort}: {tcpEx.Message}");
                }
            }
            catch (Exception dnsEx)
            {
                // #region agent log
                WriteDebugLog("EmailService.cs:122", "DNS resolution failed", new { error = dnsEx.Message }, "C");
                // #endregion
                throw new Exception($"Не удалось разрешить DNS для {smtpHost}: {dnsEx.Message}");
            }

            // Для SendGrid: порт 587 использует StartTLS, порт 465 использует SSL
            var secureOptions = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            try
            {
                // #region agent log
                WriteDebugLog("EmailService.cs:153", "Attempting SMTP ConnectAsync", new { smtpHost, smtpPort, secureSocketOptions = secureOptions.ToString() }, "C");
                // #endregion
                
                await client.ConnectAsync(smtpHost, smtpPort, secureOptions);

                // #region agent log
                WriteDebugLog("EmailService.cs:159", "After SMTP ConnectAsync", new { isConnected = client.IsConnected, isAuthenticated = client.IsAuthenticated }, "C");
                // #endregion
            }
            catch (Exception connectEx)
            {
                // #region agent log
                WriteDebugLog("EmailService.cs:165", "SMTP ConnectAsync failed", new { 
                    errorType = connectEx.GetType().Name, 
                    errorMessage = connectEx.Message,
                    innerException = connectEx.InnerException?.Message
                }, "C");
                // #endregion
                throw;
            }

            // #region agent log
            WriteDebugLog("EmailService.cs:87", "Before SMTP AuthenticateAsync", new { smtpUsername, passwordLength = emailSettings["SmtpPassword"]?.Length ?? 0 }, "B");
            // #endregion

            await client.AuthenticateAsync(smtpUsername, emailSettings["SmtpPassword"]);

            // #region agent log
            WriteDebugLog("EmailService.cs:91", "After SMTP AuthenticateAsync", new { isAuthenticated = client.IsAuthenticated }, "B");
            // #endregion

            // #region agent log
            WriteDebugLog("EmailService.cs:93", "Before SMTP SendAsync", new { messageSubject = message.Subject }, "D");
            // #endregion

            await client.SendAsync(message);

            // #region agent log
            WriteDebugLog("EmailService.cs:95", "After SMTP SendAsync", new { success = true }, "D");
            // #endregion

            await client.DisconnectAsync(true);

            // #region agent log
            WriteDebugLog("EmailService.cs:99", "SendVerificationCodeAsync success", new { toEmail }, "A");
            // #endregion

            _logger.LogInformation($"Verification code sent to {toEmail}");
        }
        catch (Exception ex)
        {
            // #region agent log
            WriteDebugLog("EmailService.cs:105", "SendVerificationCodeAsync exception", new { 
                errorType = ex.GetType().Name, 
                errorMessage = ex.Message, 
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace?.Split('\n').Take(5).ToArray()
            }, "E");
            // #endregion

            _logger.LogError(ex, "Failed to send email to {Email}. Error: {ErrorMessage}", toEmail, ex.Message);
            _logger.LogError("SMTP Host: {Host}, Port: {Port}", 
                emailSettings["SmtpHost"], 
                emailSettings.GetValue<int>("SmtpPort"));
            throw new Exception($"Не удалось отправить email: {ex.Message}");
        }
    }
}

