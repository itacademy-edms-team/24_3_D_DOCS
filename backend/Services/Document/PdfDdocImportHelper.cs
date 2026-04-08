using RusalProject.Services.Pdf;
using UglyToad.PdfPig;

namespace RusalProject.Services.Document;

public static class PdfDdocImportHelper
{
    public const string PrimaryEmbeddedName = PdfDdocAttachmentService.EmbeddedDdocFileName;

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
}

public sealed record EmbeddedFileInfo(string Name, long Size, string Kind);
