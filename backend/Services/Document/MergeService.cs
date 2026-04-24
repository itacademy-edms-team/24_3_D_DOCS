using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.Model;

namespace RusalProject.Services.Document;

/// <summary>
/// Three-way line merge: base = snapshot when opened, current = server (MinIO), incoming = client save.
/// </summary>
public static class MergeService
{
    private static readonly IDiffer LineDiffer = global::DiffPlex.Differ.Instance;

    public static (string MergedContent, bool HasConflicts) MergeMarkdown(
        string? baseContent,
        string currentContent,
        string newContent)
    {
        if (string.IsNullOrEmpty(baseContent))
            return (newContent, false);

        var b = baseContent;
        if (b == currentContent)
            return (newContent, false);
        if (b == newContent)
            return (currentContent, false);
        if (currentContent == newContent)
            return (currentContent, false);

        var chunker = LineChunker.Instance;
        var diffCur = LineDiffer.CreateDiffs(b, currentContent, false, false, chunker);
        var diffNew = LineDiffer.CreateDiffs(b, newContent, false, false, chunker);

        var hunks = new List<MergeHunk>();
        foreach (var block in diffCur.DiffBlocks)
            hunks.Add(MergeHunk.FromDiffBlock(diffCur, block, fromCurrent: true));
        foreach (var block in diffNew.DiffBlocks)
            hunks.Add(MergeHunk.FromDiffBlock(diffNew, block, fromCurrent: false));

        for (var i = 0; i < hunks.Count; i++)
        {
            for (var j = i + 1; j < hunks.Count; j++)
            {
                if (hunks[i].FromCurrent == hunks[j].FromCurrent)
                    continue;
                if (MergeHunk.RangesOverlap(hunks[i], hunks[j]))
                    return (BuildConflictFile(newContent, currentContent), true);
            }
        }

        // Line indices match DiffPlex PiecesOld (chunks of base)
        var lines = diffCur.PiecesOld.ToList();
        hunks.Sort((a, c) => c.BaseStart.CompareTo(a.BaseStart));
        foreach (var h in hunks)
        {
            lines.RemoveRange(h.BaseStart, h.BaseDeleteCount);
            lines.InsertRange(h.BaseStart, h.InsertedLines);
        }

        return (string.Concat(lines), false);
    }

    private static string BuildConflictFile(string incoming, string current) =>
        "<<<<<<< Ваша версия (сохраняемая)\n"
        + incoming
        + "\n=======\n"
        + current
        + "\n>>>>>>> Версия на сервере\n";

    private sealed class MergeHunk
    {
        public required int BaseStart { get; init; }
        public required int BaseDeleteCount { get; init; }
        public required List<string> InsertedLines { get; init; }
        public required bool FromCurrent { get; init; }

        public static MergeHunk FromDiffBlock(DiffResult r, DiffBlock block, bool fromCurrent)
        {
            var inserted = new List<string>();
            for (var i = 0; i < block.InsertCountB; i++)
                inserted.Add(r.PiecesNew[block.InsertStartB + i]);
            return new MergeHunk
            {
                BaseStart = block.DeleteStartA,
                BaseDeleteCount = block.DeleteCountA,
                InsertedLines = inserted,
                FromCurrent = fromCurrent,
            };
        }

        /// <summary>Half-open [BaseStart, BaseEndExclusive) on line indices; insert-only occupies one slot.</summary>
        public int BaseEndExclusive =>
            BaseDeleteCount > 0
                ? BaseStart + BaseDeleteCount
                : (InsertedLines.Count > 0 ? BaseStart + 1 : BaseStart);

        public static bool RangesOverlap(MergeHunk a, MergeHunk b) =>
            Math.Max(a.BaseStart, b.BaseStart) < Math.Min(a.BaseEndExclusive, b.BaseEndExclusive);
    }
}
