using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Filespec;

namespace RusalProject.Services.Pdf;

/// <summary>
/// Вкладывает TAR (.ddoc) в PDF стандартным вложением (портативные коллекции / Names).
/// </summary>
public static class PdfDdocAttachmentService
{
    public const string EmbeddedDdocFileName = "export.ddoc";

    /// <summary>
    /// Возвращает новый PDF с вложенным <paramref name="ddocBytes" />.
    /// </summary>
    public static byte[] EmbedDdocInPdf(byte[] pdfBytes, byte[] ddocBytes)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
            throw new ArgumentException("PDF bytes are empty.", nameof(pdfBytes));
        if (ddocBytes == null || ddocBytes.Length == 0)
            throw new ArgumentException("DDOC bytes are empty.", nameof(ddocBytes));

        using var input = new MemoryStream(pdfBytes, false);
        using var output = new MemoryStream();
        var reader = new PdfReader(input);
        var writer = new PdfWriter(output);
        using (var pdfDoc = new PdfDocument(reader, writer))
        {
            var fileSpec = PdfFileSpec.CreateEmbeddedFileSpec(
                pdfDoc,
                ddocBytes,
                "D-DOCS document bundle",
                EmbeddedDdocFileName,
                new PdfName("application/x-tar"),
                (iText.Kernel.Pdf.PdfDictionary?)null,
                (PdfName?)null);
            pdfDoc.AddFileAttachment(EmbeddedDdocFileName, fileSpec);
        }

        return output.ToArray();
    }
}
