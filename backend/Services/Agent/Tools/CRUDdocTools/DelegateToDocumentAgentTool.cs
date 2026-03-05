using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using RusalProject.Services.Agent.Core;

namespace RusalProject.Services.Agent.Tools.CRUDdocTools;

public sealed class DelegateToDocumentAgentTool : AgentToolBase<DelegateToDocumentAgentTool.Args>
{
    public override string Name => "delegate_to_document_agent";
    public override string Description => "Передаёт задачу агенту документа для работы с содержимым файла.";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            document_id = new { type = "string", description = "Id документа" },
            task = new { type = "string", description = "Подробная задача для агента документа" }
        },
        required = new[] { "document_id", "task" }
    };

    protected override Task<AgentToolExecutionResult> ExecuteTypedAsync(
        Args arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        var rawDocumentId = arguments.GetDocumentId();
        if (!TryParseGuidLenient(rawDocumentId, out var documentId))
            throw new InvalidOperationException("document_id должен быть корректным Guid.");
        if (string.IsNullOrWhiteSpace(arguments.Task))
            throw new InvalidOperationException("task обязателен для delegate_to_document_agent.");

        return Task.FromResult(new AgentToolExecutionResult
        {
            ResultMessage = JsonSerializer.Serialize(new
            {
                delegated = true,
                documentId,
                task = arguments.Task.Trim()
            }),
            Delegation = new AgentDelegationRequest
            {
                DocumentId = documentId,
                Task = arguments.Task.Trim()
            }
        });
    }

    private static bool TryParseGuidLenient(string? rawValue, out Guid documentId)
    {
        if (!string.IsNullOrWhiteSpace(rawValue) && Guid.TryParse(rawValue.Trim(), out documentId))
            return true;

        var match = Regex.Match(rawValue ?? string.Empty,
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        if (match.Success && Guid.TryParse(match.Value, out documentId))
            return true;

        documentId = Guid.Empty;
        return false;
    }

    public sealed class Args
    {
        [JsonPropertyName("document_id")]
        public string? DocumentId { get; init; }

        [JsonPropertyName("documentId")]
        public string? DocumentIdCamel { get; init; }

        [JsonPropertyName("task")]
        public string Task { get; init; } = string.Empty;

        public string? GetDocumentId() => !string.IsNullOrWhiteSpace(DocumentId) ? DocumentId : DocumentIdCamel;
    }
}
