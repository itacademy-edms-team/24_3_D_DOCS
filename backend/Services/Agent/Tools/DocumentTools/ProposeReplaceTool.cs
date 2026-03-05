using System.Text.Json;
using System.Text.Json.Serialization;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

public sealed class ProposeReplaceTool : AgentToolBase<ProposeReplaceTool.Args>
{
    private readonly IDocumentService _documentService;

    public ProposeReplaceTool(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public override string Name => "propose_replace";
    public override string Description => "Предлагает замену диапазона строк новым markdown-контентом.";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            start_line = new { type = "integer", description = "Начальная строка диапазона (1-based)." },
            end_line = new { type = "integer", description = "Конечная строка диапазона (1-based)." },
            content = new { type = "string", description = "Новый markdown-контент." }
        },
        required = new[] { "start_line", "content" }
    };

    protected override async Task<AgentToolExecutionResult> ExecuteTypedAsync(
        Args arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (!context.DocumentId.HasValue)
            throw new InvalidOperationException("propose_replace доступен только в контексте документа.");
        if (string.IsNullOrWhiteSpace(arguments.Content))
            throw new InvalidOperationException("content обязателен для propose_replace.");

        var document = await _documentService.GetDocumentWithContentAsync(context.DocumentId.Value, context.UserId);
        if (document == null)
            throw new InvalidOperationException("Документ не найден.");

        var (lines, totalLines) = DocumentProposalToolHelper.GetLines(document.Content ?? string.Empty);
        if (totalLines == 0)
            throw new InvalidOperationException("Нельзя заменить строки в пустом документе.");

        var (startLine, endLine) = DocumentProposalToolHelper.NormalizeRange(arguments.StartLine, arguments.EndLine ?? arguments.StartLine, totalLines);
        var oldContent = string.Join("\n", lines.GetRange(startLine - 1, endLine - startLine + 1));
        var newContent = DocumentProposalToolHelper.NormalizeContent(arguments.Content).Trim();
        var deleteChangeId = Guid.NewGuid().ToString("N")[..12];
        var insertChangeId = Guid.NewGuid().ToString("N")[..12];
        var groupId = Guid.NewGuid().ToString("N")[..12];

        var changes = new List<DocumentEntityChangeDTO>();
        changes.AddRange(DocumentProposalToolHelper.SplitIntoEntityChanges(deleteChangeId, "delete", startLine, oldContent, endLine, groupId, 0));
        changes.AddRange(DocumentProposalToolHelper.SplitIntoEntityChanges(insertChangeId, "insert", startLine - 1, newContent, null, groupId, changes.Count));
        await _documentService.AddPendingDocumentChangesAsync(context.DocumentId.Value, context.UserId, changes);

        return new AgentToolExecutionResult
        {
            ResultMessage = JsonSerializer.Serialize(new
            {
                replaced = true,
                startLine,
                endLine,
                deleteChangeId,
                insertChangeId,
                groupId
            }),
            DocumentChanges = changes
        };
    }

    public sealed class Args
    {
        [JsonPropertyName("start_line")]
        public int StartLine { get; init; }

        [JsonPropertyName("end_line")]
        public int? EndLine { get; init; }

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;
    }
}
