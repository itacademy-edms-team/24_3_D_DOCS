using System.Text.Json;
using System.Text.Json.Serialization;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

public sealed class ProposeDeleteTool : AgentToolBase<ProposeDeleteTool.Args>
{
    private readonly IDocumentService _documentService;

    public ProposeDeleteTool(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public override string Name => "propose_delete";
    public override string Description => "Предлагает удаление диапазона строк в документе.";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            start_line = new { type = "integer", description = "Начальная строка диапазона (1-based)." },
            end_line = new { type = "integer", description = "Конечная строка диапазона (1-based)." }
        },
        required = new[] { "start_line" }
    };

    protected override async Task<AgentToolExecutionResult> ExecuteTypedAsync(
        Args arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (!context.DocumentId.HasValue)
            throw new InvalidOperationException("propose_delete доступен только в контексте документа.");

        var document = await _documentService.GetDocumentWithContentAsync(context.DocumentId.Value, context.UserId);
        if (document == null)
            throw new InvalidOperationException("Документ не найден.");

        var (lines, totalLines) = DocumentProposalToolHelper.GetLines(document.Content ?? string.Empty);
        if (totalLines == 0)
            throw new InvalidOperationException("Нельзя удалить строки из пустого документа.");

        var (startLine, endLine) = DocumentProposalToolHelper.NormalizeRange(arguments.StartLine, arguments.EndLine ?? arguments.StartLine, totalLines);
        var contentToDelete = string.Join("\n", lines.GetRange(startLine - 1, endLine - startLine + 1));
        var changeId = Guid.NewGuid().ToString("N")[..12];
        var documentChanges = DocumentProposalToolHelper.SplitIntoEntityChanges(changeId, "delete", startLine, contentToDelete, endLine);
        await _documentService.AddPendingDocumentChangesAsync(context.DocumentId.Value, context.UserId, documentChanges);

        return new AgentToolExecutionResult
        {
            ResultMessage = JsonSerializer.Serialize(new
            {
                changeId,
                deleted = true,
                startLine,
                endLine
            }),
            DocumentChanges = documentChanges
        };
    }

    public sealed class Args
    {
        [JsonPropertyName("start_line")]
        public int StartLine { get; init; }

        [JsonPropertyName("end_line")]
        public int? EndLine { get; init; }
    }
}
