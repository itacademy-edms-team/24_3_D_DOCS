namespace RusalProject.Services.Agent;

public interface IOllamaAttachmentQueryService
{
    Task<string> AnswerTextQuestionAsync(Guid userId, string text, string question, CancellationToken cancellationToken = default);

    Task<string> AnswerImageQuestionAsync(Guid userId, byte[] imageBytes, string question, CancellationToken cancellationToken = default);
}
