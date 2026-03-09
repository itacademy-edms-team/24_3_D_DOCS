using RusalProject.Services.AgentSources;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

public class OllamaAttachmentQueryService : IOllamaAttachmentQueryService
{
    private readonly IOllamaSimpleChatService _ollama;
    private readonly IConfiguration _configuration;

    public OllamaAttachmentQueryService(IOllamaSimpleChatService ollama, IConfiguration configuration)
    {
        _ollama = ollama;
        _configuration = configuration;
    }

    public Task<string> AnswerTextQuestionAsync(Guid userId, string text, string question, CancellationToken cancellationToken = default)
    {
        var model = _configuration["Ollama:AttachmentTextModel"];
        if (string.IsNullOrWhiteSpace(model))
            model = _configuration["Ollama:DefaultModel"] ?? "gpt-oss:120b";

        var max = AgentSourceConstants.MaxTextCharsForAttachmentLlm;
        var body = text.Length > max
            ? text[..max] + "\n\n...[текст обрезан для модели]"
            : text;

        var userContent = $"""
Текст вложения:
---
{body}
---

Вопрос: {question}
""";

        return _ollama.CompleteTextAsync(
            userId,
            model,
            "Отвечай только по приведённому тексту вложения. Если данных для ответа нет — скажи об этом кратко. Ответ на русском языке.",
            userContent,
            cancellationToken);
    }

    public Task<string> AnswerImageQuestionAsync(Guid userId, byte[] imageBytes, string question, CancellationToken cancellationToken = default)
    {
        var model = _configuration["Ollama:VisionModel"];
        if (string.IsNullOrWhiteSpace(model))
            model = _configuration["Ollama:DefaultModel"] ?? "gpt-oss:120b";

        return _ollama.CompleteVisionAsync(
            userId,
            model,
            "Ответь на вопрос пользователя по изображению. Будь точным и кратким. Ответ на русском языке.",
            question,
            imageBytes,
            cancellationToken);
    }
}
