using System.Text.Json;
using System.Text.Json.Serialization;
using RusalProject.Models.DTOs.Document;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.CRUDdocTools;

public sealed class RenameDocumentTool : AgentToolBase<RenameDocumentTool.Args>
{
    private readonly IDocumentService _documentService;

    public RenameDocumentTool(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public override string Name => "rename_document";
    public override string Description => "Переименовывает документ.";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            document_id = new { type = "string", description = "Id документа" },
            name = new { type = "string", description = "Новое название документа" }
        },
        required = new[] { "document_id", "name" }
    };

    protected override async Task<AgentToolExecutionResult> ExecuteTypedAsync(
        Args arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(arguments.DocumentId, out var documentId))
            throw new InvalidOperationException("document_id должен быть корректным Guid.");
        if (string.IsNullOrWhiteSpace(arguments.Name))
            throw new InvalidOperationException("name обязателен для rename_document.");

        var updated = await _documentService.UpdateDocumentAsync(documentId, context.UserId, new UpdateDocumentDTO
        {
            Name = arguments.Name.Trim()
        });

        return new AgentToolExecutionResult
        {
            ResultMessage = JsonSerializer.Serialize(new
            {
                id = updated.Id,
                name = updated.Name,
                renamed = true
            })
        };
    }

    public sealed class Args
    {
        [JsonPropertyName("document_id")]
        public string DocumentId { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
    }
}
