using System.Text.Json.Serialization;
using RusalProject.Models.Types;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.AgentSources;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

public sealed class QueryAttachmentTextTool : AgentToolBase<QueryAttachmentTextTool.Args>
{
    private readonly IAgentSourceService _agentSourceService;
    private readonly IOllamaAttachmentQueryService _attachmentQuery;

    public QueryAttachmentTextTool(IAgentSourceService agentSourceService, IOllamaAttachmentQueryService attachmentQuery)
    {
        _agentSourceService = agentSourceService;
        _attachmentQuery = attachmentQuery;
    }

    public override string Name => "query_attachment_text";
    public override string Description =>
        "Задаёт вопрос по текстовой части вложения пользователя через отдельную языковую модель. Используй индекс из каталога вложений (Kind=Text).";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            part_index = new { type = "integer", description = "Индекс части (текст) из каталога вложений" },
            question = new { type = "string", description = "Вопрос по содержимому текста" }
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

        if (part.KindEnum != AgentSourcePartKind.Text)
            throw new InvalidOperationException($"Часть {arguments.PartIndex} не является текстом; используй query_attachment_image.");

        if (string.IsNullOrWhiteSpace(arguments.Question))
            throw new InvalidOperationException("Параметр question обязателен.");

        var text = await _agentSourceService.LoadPartTextAsync(part, context.UserId, cancellationToken);
        var answer = await _attachmentQuery.AnswerTextQuestionAsync(context.UserId, text, arguments.Question.Trim(), cancellationToken);

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
