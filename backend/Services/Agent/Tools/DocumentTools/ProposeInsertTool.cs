using System.Text.Json;
using System.Text.Json.Serialization;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

public sealed class ProposeInsertTool : AgentToolBase<ProposeInsertTool.Args>
{
    private readonly IDocumentService _documentService;

    public ProposeInsertTool(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public override string Name => "propose_insert";
    public override string Description => "Предлагает вставку нового markdown-контента в документ.";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            start_line = new { type = "integer", description = "Строка, после которой вставляется контент. 0 = начало документа." },
            content = new { type = "string", description = "Markdown-контент для вставки." }
        },
        required = new[] { "start_line", "content" }
    };

    protected override async Task<AgentToolExecutionResult> ExecuteTypedAsync(
        Args arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (!context.DocumentId.HasValue)
            throw new InvalidOperationException("propose_insert доступен только в контексте документа.");
        if (string.IsNullOrWhiteSpace(arguments.Content))
            throw new InvalidOperationException("content обязателен для propose_insert.");

        var document = await _documentService.GetDocumentWithContentAsync(context.DocumentId.Value, context.UserId);
        if (document == null)
            throw new InvalidOperationException("Документ не найден.");

        var (_, totalLines) = DocumentProposalToolHelper.GetLines(document.Content ?? string.Empty);
        var changeId = Guid.NewGuid().ToString("N")[..12];
        var insertAfterLine = Math.Clamp(arguments.StartLine, 0, totalLines);
        var normalizedContent = DocumentProposalToolHelper.NormalizeContent(arguments.Content).Trim();
        var documentChanges = DocumentProposalToolHelper.SplitIntoEntityChanges(changeId, "insert", insertAfterLine, normalizedContent);
        await _documentService.AddPendingDocumentChangesAsync(context.DocumentId.Value, context.UserId, documentChanges);

        return new AgentToolExecutionResult
        {
            ResultMessage = JsonSerializer.Serialize(new
            {
                changeId,
                inserted = true,
                startLine = insertAfterLine,
                content = normalizedContent
            }),
            DocumentChanges = documentChanges
        };
    }

    public sealed class Args
    {
        [JsonPropertyName("start_line")]
        public int StartLine { get; init; }

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;
    }
}
