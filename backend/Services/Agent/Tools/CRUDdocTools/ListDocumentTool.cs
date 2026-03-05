using System.Text.Json;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.CRUDdocTools;

public sealed class ListDocumentTool : AgentToolBase<ListDocumentTool.Args>
{
    private readonly IDocumentService _documentService;

    public ListDocumentTool(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public override string Name => "list_documents";
    public override string Description => "Получает список документов пользователя с id, названием и датой изменения.";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            search = new
            {
                type = "string",
                description = "Подстрока для поиска по названию документа"
            }
        }
    };

    protected override async Task<AgentToolExecutionResult> ExecuteTypedAsync(
        Args arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        var documents = await _documentService.GetDocumentsAsync(context.UserId, null, arguments.Search);
        var payload = documents.Select(d => new
        {
            id = d.Id,
            name = d.Name,
            updatedAt = d.UpdatedAt
        });

        return new AgentToolExecutionResult
        {
            ResultMessage = JsonSerializer.Serialize(payload)
        };
    }

    public sealed class Args
    {
        public string? Search { get; init; }
    }
}
