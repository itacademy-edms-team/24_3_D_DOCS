using System.Text;
using System.Text.Json.Serialization;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

public sealed class ReadDocumentTool : AgentToolBase<ReadDocumentTool.Args>
{
    private const int MaxReturnedLinesWithoutRange = 400;

    private readonly IDocumentService _documentService;

    public ReadDocumentTool(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public override string Name => "read_document";
    public override string Description => "Читает документ целиком или по диапазону строк.";

    public override object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            start_line = new { type = "integer", description = "Начальная строка (1-based)" },
            end_line = new { type = "integer", description = "Конечная строка (1-based)" }
        }
    };

    protected override async Task<AgentToolExecutionResult> ExecuteTypedAsync(
        Args arguments,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (!context.DocumentId.HasValue)
            throw new InvalidOperationException("read_document доступен только в контексте документа.");

        var document = await _documentService.GetDocumentWithContentAsync(context.DocumentId.Value, context.UserId);
        if (document == null)
            throw new InvalidOperationException("Документ не найден.");

        var content = (document.Content ?? string.Empty).Replace("\r\n", "\n");
        var lines = content.Length == 0 ? Array.Empty<string>() : content.Split('\n');
        var totalLines = lines.Length;

        var startLine = arguments.StartLine ?? 1;
        var endLine = arguments.EndLine ?? totalLines;

        if (!arguments.StartLine.HasValue && !arguments.EndLine.HasValue && totalLines > MaxReturnedLinesWithoutRange)
            endLine = MaxReturnedLinesWithoutRange;

        if (totalLines == 0)
        {
            return new AgentToolExecutionResult
            {
                ResultMessage = $"document_name: {document.Name}\ntotal_lines: 0\ncontent:\n(пустой документ)"
            };
        }

        startLine = Math.Clamp(startLine, 1, totalLines);
        endLine = Math.Clamp(endLine, 1, totalLines);
        if (startLine > endLine)
            (startLine, endLine) = (endLine, startLine);

        var sb = new StringBuilder();
        sb.AppendLine($"document_name: {document.Name}");
        sb.AppendLine($"total_lines: {totalLines}");
        sb.AppendLine($"range: {startLine}-{endLine}");
        if (!arguments.StartLine.HasValue && !arguments.EndLine.HasValue && totalLines > MaxReturnedLinesWithoutRange)
            sb.AppendLine($"note: документ длинный, возвращены первые {MaxReturnedLinesWithoutRange} строк.");
        sb.AppendLine("content:");

        for (var i = startLine; i <= endLine; i++)
            sb.AppendLine($"{i}: {lines[i - 1]}");

        return new AgentToolExecutionResult
        {
            ResultMessage = sb.ToString().TrimEnd()
        };
    }

    public sealed class Args
    {
        [JsonPropertyName("start_line")]
        public int? StartLine { get; init; }

        [JsonPropertyName("end_line")]
        public int? EndLine { get; init; }
    }
}
