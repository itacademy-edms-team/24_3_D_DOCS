using RusalProject.Services.AgentSources;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

public class OllamaAttachmentQueryService : IOllamaAttachmentQueryService
{
    private readonly IOllamaSimpleChatService _ollama;
    private readonly IUserOllamaModelResolutionService _modelResolution;

    public OllamaAttachmentQueryService(
        IOllamaSimpleChatService ollama,
        IUserOllamaModelResolutionService modelResolution)
    {
        _ollama = ollama;
        _modelResolution = modelResolution;
    }

    public async Task<string> AnswerTextQuestionAsync(Guid userId, string text, string question, CancellationToken cancellationToken = default)
    {
        var model = await _modelResolution.GetAttachmentTextModelAsync(userId, cancellationToken);

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

        return await _ollama.CompleteTextAsync(
            userId,
            model,
            "Отвечай только по приведённому тексту вложения. Если данных для ответа нет — скажи об этом кратко. Ответ на русском языке.",
            userContent,
            cancellationToken);
    }

    public async Task<string> AnswerImageQuestionAsync(Guid userId, byte[] imageBytes, string question, CancellationToken cancellationToken = default)
    {
        var model = await _modelResolution.GetVisionModelAsync(userId, cancellationToken);

        return await _ollama.CompleteVisionAsync(
            userId,
            model,
            "Ответь на вопрос пользователя по изображению. Будь точным и кратким. Ответ на русском языке.",
            question,
            imageBytes,
            cancellationToken);
    }
}
