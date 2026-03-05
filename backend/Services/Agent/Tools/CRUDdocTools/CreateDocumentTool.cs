using System.Text.Json;
using System.Text.Json.Serialization;
using RusalProject.Models.DTOs.Document;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.CRUDdocTools;

public sealed class CreateDocumentTool : AgentToolBase<CreateDocumentTool.Args>
{
    private readonly IDocumentService _documentService;

    public CreateDocumentTool(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public override string Name => "create_document";
    public override string Description => "Создаёт новый документ.";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            name = new { type = "string", description = "Название нового документа" },
            description = new { type = "string", description = "Описание документа" },
            initial_content = new { type = "string", description = "Начальный markdown-текст документа" }
        },
        required = new[] { "name" }
    };

    protected override async Task<AgentToolExecutionResult> ExecuteTypedAsync(
        Args arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(arguments.Name))
            throw new InvalidOperationException("name обязателен для create_document.");

        var document = await _documentService.CreateDocumentAsync(context.UserId, new CreateDocumentDTO
        {
            Name = arguments.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(arguments.Description) ? null : arguments.Description.Trim(),
            InitialContent = arguments.InitialContent
        });

        return new AgentToolExecutionResult
        {
            ResultMessage = JsonSerializer.Serialize(new
            {
                id = document.Id,
                name = document.Name,
                created = true
            })
        };
    }

    public sealed class Args
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("initial_content")]
        public string? InitialContent { get; init; }
    }
}
