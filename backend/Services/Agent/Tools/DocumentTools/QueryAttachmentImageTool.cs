using System.Text.Json.Serialization;
using RusalProject.Models.Types;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.AgentSources;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

public sealed class QueryAttachmentImageTool : AgentToolBase<QueryAttachmentImageTool.Args>
{
    private readonly IAgentSourceService _agentSourceService;
    private readonly IOllamaAttachmentQueryService _attachmentQuery;

    public QueryAttachmentImageTool(IAgentSourceService agentSourceService, IOllamaAttachmentQueryService attachmentQuery)
    {
        _agentSourceService = agentSourceService;
        _attachmentQuery = attachmentQuery;
    }

    public override string Name => "query_attachment_image";
    public override string Description =>
        "Задаёт вопрос по изображению из вложения через vision-модель. Используй индекс из каталога (Kind=Image).";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            part_index = new { type = "integer", description = "Индекс части (изображение) из каталога вложений" },
            question = new { type = "string", description = "Вопрос по изображению" }
        },
        required = new[] { "part_index", "question" }
    };

    protected override async Task<AgentToolExecutionResult> ExecuteTypedAsync(
        Args arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (!context.SourceSessionId.HasValue || !context.ChatSessionId.HasValue)
            throw new InvalidOperationException("Нет активной сессии вложения для этого чата.");

        var session = await _agentSourceService.GetValidatedSessionAsync(
            context.UserId,
            context.SourceSessionId.Value,
            context.ChatSessionId.Value,
            context.DocumentId,
            cancellationToken);

        if (session == null)
            throw new InvalidOperationException("Сессия вложения недействительна или истекла.");

        var part = session.Parts.FirstOrDefault(p => p.PartIndex == arguments.PartIndex)
            ?? throw new InvalidOperationException($"Часть с индексом {arguments.PartIndex} не найдена.");

        if (part.KindEnum != AgentSourcePartKind.Image)
            throw new InvalidOperationException($"Часть {arguments.PartIndex} не является изображением; используй query_attachment_text.");

        if (string.IsNullOrWhiteSpace(arguments.Question))
            throw new InvalidOperationException("Параметр question обязателен.");

        var bytes = await _agentSourceService.LoadPartImageAsync(part, context.UserId, cancellationToken);
        if (bytes.Length > AgentSourceConstants.MaxImageBytes)
            throw new InvalidOperationException("Изображение слишком большое для обработки.");

        var answer = await _attachmentQuery.AnswerImageQuestionAsync(
            context.UserId,
            bytes,
            arguments.Question.Trim(),
            cancellationToken);

        return new AgentToolExecutionResult
        {
            ResultMessage = answer
        };
    }

    public sealed class Args
    {
        [JsonPropertyName("part_index")]
        public int PartIndex { get; init; }

        [JsonPropertyName("question")]
        public string Question { get; init; } = string.Empty;
    }
}
