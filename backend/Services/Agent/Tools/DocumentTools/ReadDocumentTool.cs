using System.Text;
using System.Text.Json.Serialization;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Document;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

public sealed class ReadDocumentTool : AgentToolBase<ReadDocumentTool.Args>
{
    private const int MaxReturnedLinesWithoutRange = 400;
    private const int MaxPendingContentChars = 2000;

    private readonly IDocumentService _documentService;

    public ReadDocumentTool(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public override string Name => "read_document";
    public override string Description =>
        "Читает документ целиком или по диапазону строк. В конце ответа перечисляет непринятые предложения правок (pending_ai_changes).";

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

        var pendingChanges = await _documentService.GetPendingDocumentChangesAsync(context.DocumentId.Value, context.UserId);
        var pendingSection = FormatPendingAiChangesSection(pendingChanges);

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
                ResultMessage =
                    $"document_name: {document.Name}\ntotal_lines: 0\ncontent:\n(пустой документ)\n\n{pendingSection}"
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

        sb.AppendLine();
        sb.AppendLine(pendingSection);

        return new AgentToolExecutionResult
        {
            ResultMessage = sb.ToString().TrimEnd()
        };
    }

    private static string TruncateForAgent(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "(пусто)";
        if (text.Length <= MaxPendingContentChars)
            return text;
        return text[..MaxPendingContentChars] + "\n… (усечено)";
    }

    private static IEnumerable<string> IndentBlock(string text)
    {
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
            yield return "    " + line;
    }

    /// <summary>Группы в порядке первого появления в списке от API (CreatedAt, строка, Order).</summary>
    private static string FormatPendingAiChangesSection(IReadOnlyList<DocumentEntityChangeDTO> pending)
    {
        if (pending.Count == 0)
            return "pending_ai_changes:\n(нет ожидающих правок)";

        var groups = new List<List<DocumentEntityChangeDTO>>();
        var groupKeyIndex = new Dictionary<string, int>();

        foreach (var c in pending)
        {
            var key = c.GroupId ?? c.ChangeId;
            if (!groupKeyIndex.TryGetValue(key, out var idx))
            {
                idx = groups.Count;
                groupKeyIndex[key] = idx;
                groups.Add([]);
            }

            groups[idx].Add(c);
        }

        var sb = new StringBuilder();
        sb.AppendLine("pending_ai_changes:");

        for (var g = 0; g < groups.Count; g++)
        {
            var sorted = groups[g]
                .OrderBy(x => x.Order ?? 0)
                .ThenBy(x => x.StartLine)
                .ToList();
            var deletes = sorted
                .Where(x => string.Equals(x.ChangeType, "delete", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var inserts = sorted
                .Where(x => string.Equals(x.ChangeType, "insert", StringComparison.OrdinalIgnoreCase))
                .ToList();

            sb.Append($"{g + 1}. ");

            if (deletes.Count > 0 && inserts.Count > 0)
            {
                var start = deletes.Min(d => d.StartLine);
                var end = deletes.Max(d => d.EndLine ?? d.StartLine);
                var groupId = deletes[0].GroupId ?? inserts[0].GroupId ?? "—";
                sb.AppendLine(
                    $"замена строк {start}-{end} (group_id: {groupId}, change_id удаления: {deletes[0].ChangeId}, вставки: {inserts[0].ChangeId})");
                var oldText = string.Join("\n", deletes.Select(d => d.Content));
                var newText = string.Join("\n", inserts.Select(i => i.Content));
                sb.AppendLine("  было:");
                foreach (var line in IndentBlock(TruncateForAgent(oldText)))
                    sb.AppendLine(line);
                sb.AppendLine("  стало:");
                foreach (var line in IndentBlock(TruncateForAgent(newText)))
                    sb.AppendLine(line);
            }
            else if (deletes.Count > 0)
            {
                var start = deletes.Min(d => d.StartLine);
                var end = deletes.Max(d => d.EndLine ?? d.StartLine);
                sb.AppendLine($"удаление строк {start}-{end} (change_id: {deletes[0].ChangeId})");
                var delText = string.Join("\n", deletes.Select(d => d.Content));
                sb.AppendLine("  удаляемый текст:");
                foreach (var line in IndentBlock(TruncateForAgent(delText)))
                    sb.AppendLine(line);
            }
            else if (inserts.Count > 0)
            {
                var afterLine = inserts.Min(i => i.StartLine);
                sb.AppendLine(
                    $"вставка после строки {afterLine} (0 = начало документа) (change_id: {inserts[0].ChangeId})");
                var insText = string.Join("\n", inserts.Select(i => i.Content));
                sb.AppendLine("  текст:");
                foreach (var line in IndentBlock(TruncateForAgent(insText)))
                    sb.AppendLine(line);
            }
            else
            {
                sb.AppendLine(
                    $"неизвестные типы: {string.Join(", ", sorted.Select(x => x.ChangeType).Distinct())}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    public sealed class Args
    {
        [JsonPropertyName("start_line")]
        public int? StartLine { get; init; }

        [JsonPropertyName("end_line")]
        public int? EndLine { get; init; }
    }
}
