using RusalProject.Models.DTOs.Agent;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

internal static class DocumentProposalToolHelper
{
    public static string NormalizeContent(string? content) => (content ?? string.Empty).Replace("\r\n", "\n");

    public static (List<string> Lines, int TotalLines) GetLines(string content)
    {
        var normalized = NormalizeContent(content);
        var lines = normalized.Length == 0 ? new List<string>() : normalized.Split('\n').ToList();
        return (lines, lines.Count);
    }

    public static (int Start, int End) NormalizeRange(int start, int end, int totalLines)
    {
        if (totalLines <= 0)
            return (1, 1);

        start = Math.Clamp(start, 1, totalLines);
        end = Math.Clamp(end, 1, totalLines);
        if (start > end)
            (start, end) = (end, start);
        return (start, end);
    }

    /// <summary>Одна запись на вызов инструмента — без разбиения по параграфам, чтобы номера строк и merge на фронте оставались предсказуемыми.</summary>
    public static List<DocumentEntityChangeDTO> SplitIntoEntityChanges(
        string changeId,
        string changeType,
        int startLine,
        string content,
        int? endLine = null,
        string? groupId = null,
        int startOrder = 0)
    {
        return new List<DocumentEntityChangeDTO>
        {
            new()
            {
                ChangeId = changeId,
                ChangeType = changeType,
                EntityType = "markdown",
                StartLine = startLine,
                EndLine = endLine,
                Content = NormalizeContent(content),
                GroupId = groupId,
                Order = startOrder
            }
        };
    }
}
