using System.Text.Json;
using System.Text.Json.Serialization;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.CRUDdocTools;

public sealed class DeleteDocumentTool : AgentToolBase<DeleteDocumentTool.Args>
{
    private readonly IDocumentService _documentService;

    public DeleteDocumentTool(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public override string Name => "delete_document";
    public override string Description => "Удаляет документ по id.";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            document_id = new { type = "string", description = "Id документа для удаления" }
        },
        required = new[] { "document_id" }
    };

    protected override async Task<AgentToolExecutionResult> ExecuteTypedAsync(
        Args arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(arguments.DocumentId, out var documentId))
            throw new InvalidOperationException("document_id должен быть корректным Guid.");

        await _documentService.DeleteDocumentAsync(documentId, context.UserId);

        return new AgentToolExecutionResult
        {
            ResultMessage = JsonSerializer.Serialize(new
            {
                id = documentId,
                deleted = true
            })
        };
    }

    public sealed class Args
    {
        [JsonPropertyName("document_id")]
        public string DocumentId { get; init; } = string.Empty;
    }
}
