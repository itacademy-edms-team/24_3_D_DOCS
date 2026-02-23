using System.Text;
using UglyToad.PdfPig;

namespace RusalProject.Services.ChatContext;

/// <summary>
/// Parses text files (.txt, .md, .csv, .log) and PDFs. No LLM.
/// </summary>
public class TextFileParser
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".txt", ".md", ".csv", ".log" };
    private static readonly HashSet<string> PdfExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf" };

    public bool CanParse(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return TextExtensions.Contains(ext) || PdfExtensions.Contains(ext);
    }

    public async Task<string> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName);
        if (PdfExtensions.Contains(ext))
            return ParsePdf(stream);
        return await ParseTextAsync(stream, cancellationToken);
    }

    private static async Task<string> ParseTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string ParsePdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            var words = page.GetWords();
            var line = string.Join(" ", words.Select(w => w.Text));
            sb.AppendLine(line);
        }
        return sb.ToString().TrimEnd();
    }
}
