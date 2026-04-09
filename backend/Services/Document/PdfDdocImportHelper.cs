using System.Formats.Tar;
using RusalProject.Services.Pdf;
using UglyToad.PdfPig;

namespace RusalProject.Services.Document;

public static class PdfDdocImportHelper
{
    public const string PrimaryEmbeddedName = PdfDdocAttachmentService.EmbeddedDdocFileName;
    private const string DocumentEntryName = "document.md";
    private const string ProfileEntryName = "profile.json";
    private const string TitlePageEntryName = "titlepage.json";

    public static IReadOnlyList<EmbeddedFileInfo> ListEmbeddedFiles(byte[] pdfBytes)
    {
        var list = new List<EmbeddedFileInfo>();
        using var ms = new MemoryStream(pdfBytes, false);
        using var document = PdfDocument.Open(ms);
        if (document.Advanced == null)
            return list;
        if (!document.Advanced.TryGetEmbeddedFiles(out var embedded) || embedded == null)
            return list;
        foreach (var f in embedded)
        {
            var name = f.Name ?? "unnamed";
            var bytes = f.Bytes?.ToArray() ?? Array.Empty<byte>();
            var isDdoc = name.EndsWith(".ddoc", StringComparison.OrdinalIgnoreCase)
                || name.Equals(PrimaryEmbeddedName, StringComparison.OrdinalIgnoreCase);
            list.Add(new EmbeddedFileInfo(
                name,
                bytes.Length,
                isDdoc ? "ddoc" : "other"));
        }
        return list;
    }

    public static byte[]? GetEmbeddedFileBytes(byte[] pdfBytes, string fileName)
    {
        using var ms = new MemoryStream(pdfBytes, false);
        using var document = PdfDocument.Open(ms);
        if (document.Advanced == null)
            return null;
        if (!document.Advanced.TryGetEmbeddedFiles(out var embedded) || embedded == null)
            return null;
        foreach (var f in embedded)
        {
            if (f.Name != null && string.Equals(f.Name, fileName, StringComparison.OrdinalIgnoreCase))
            {
                return f.Bytes?.ToArray() ?? Array.Empty<byte>();
            }
        }
        return null;
    }

    public static DdocBundlePartsPreview InspectDdocBundle(byte[] ddocBytes)
    {
        var parts = new DdocBundlePartsPreview();
        using var input = new MemoryStream(ddocBytes, false);
        using var tarReader = new TarReader(input, leaveOpen: false);
        TarEntry? entry;
        while ((entry = tarReader.GetNextEntry()) != null)
        {
            if (entry.EntryType != TarEntryType.RegularFile || entry.DataStream == null)
                continue;

            var entryName = entry.Name ?? string.Empty;
            var normalized = entryName.Replace('\\', '/').TrimStart('/');
            long size = 0;
            try
            {
                size = entry.DataStream.CanSeek ? entry.DataStream.Length : 0;
            }
            catch
            {
                size = 0;
            }

            if (normalized.Equals(DocumentEntryName, StringComparison.OrdinalIgnoreCase))
            {
                parts.HasDocument = true;
                parts.DocumentSize = size;
            }
            else if (normalized.Equals(ProfileEntryName, StringComparison.OrdinalIgnoreCase))
            {
                parts.HasStyleProfile = true;
                parts.StyleProfileSize = size;
            }
            else if (normalized.Equals(TitlePageEntryName, StringComparison.OrdinalIgnoreCase))
            {
                parts.HasTitlePage = true;
                parts.TitlePageSize = size;
            }
        }

        return parts;
    }

    public static byte[] FilterDdocBundle(
        byte[] ddocBytes,
        bool includeDocument,
        bool includeStyleProfile,
        bool includeTitlePage)
    {
        if (!includeDocument)
            throw new InvalidOperationException("Document part is required for import.");

        using var input = new MemoryStream(ddocBytes, false);
        using var tarReader = new TarReader(input, leaveOpen: false);
        using var output = new MemoryStream();
        using var tarWriter = new TarWriter(output, leaveOpen: true);
        TarEntry? entry;

        while ((entry = tarReader.GetNextEntry()) != null)
        {
            if (entry.EntryType != TarEntryType.RegularFile || entry.DataStream == null)
                continue;

            var entryName = entry.Name ?? string.Empty;
            var normalized = entryName.Replace('\\', '/').TrimStart('/');
            var isProfile = normalized.Equals(ProfileEntryName, StringComparison.OrdinalIgnoreCase);
            var isTitle = normalized.Equals(TitlePageEntryName, StringComparison.OrdinalIgnoreCase);
            var shouldSkip = (isProfile && !includeStyleProfile) || (isTitle && !includeTitlePage);
            if (shouldSkip)
                continue;

            using var cloned = new MemoryStream();
            entry.DataStream.CopyTo(cloned);
            cloned.Position = 0;
            DdocTarUtil.WriteFileToTarCoreAsync(tarWriter, normalized, cloned, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        output.Position = 0;
        return output.ToArray();
    }
}

public sealed record EmbeddedFileInfo(string Name, long Size, string Kind);
public sealed class DdocBundlePartsPreview
{
    public bool HasDocument { get; set; }
    public long DocumentSize { get; set; }
    public bool HasStyleProfile { get; set; }
    public long StyleProfileSize { get; set; }
    public bool HasTitlePage { get; set; }
    public long TitlePageSize { get; set; }
}
