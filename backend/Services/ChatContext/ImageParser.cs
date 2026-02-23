using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.ChatContext;

/// <summary>
/// Parses images via LLM (qwen3.5:cloud) — returns text description.
/// </summary>
public class ImageParser
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly IUserOllamaApiKeyService _keyService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImageParser> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ImageParser(
        IUserOllamaApiKeyService keyService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ImageParser> logger)
    {
        _keyService = keyService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public bool CanParse(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return ImageExtensions.Contains(ext);
    }

    public async Task<string> ParseAsync(Stream stream, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await _keyService.HasApiKeyAsync(userId, cancellationToken))
            throw new InvalidOperationException("Настройте Ollama API ключ для описания изображений.");

        var apiKey = await _keyService.GetDecryptedApiKeyAsync(userId, cancellationToken);
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Не удалось получить API ключ.");

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();
        var base64 = Convert.ToBase64String(bytes);

        var model = _configuration["Ollama:ImageParserModel"] ?? "qwen3.5:cloud";
        var baseUrl = _configuration["Ollama:BaseUrl"] ?? "https://ollama.com";
        var url = $"{baseUrl.TrimEnd('/')}/api/chat";

        var requestBody = new
        {
            model,
            stream = false,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = "Опиши это изображение текстом: что на нём изображено, какие объекты, текст (если есть), структура. Отвечай на русском.",
                    images = new[] { base64 }
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("API ключ недействителен.");
        if (response.StatusCode == (System.Net.HttpStatusCode)429)
            throw new InvalidOperationException("Провайдер временно ограничил запросы.");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama vision API returned {StatusCode}", response.StatusCode);
            throw new InvalidOperationException("Ошибка при описании изображения. Попробуйте позже.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .TryGetProperty("message", out var msg)
            && msg.TryGetProperty("content", out var c)
            ? c.GetString() ?? ""
            : "";

        return content.Trim();
    }
}
