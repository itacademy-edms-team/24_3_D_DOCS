using RusalProject.Services.Pdf;
using UglyToad.PdfPig;
using Xunit;

namespace RusalProject.Tests;

/// <summary>
/// Интеграционная проверка импорта реального отчёта (файл в Fixtures/, не в git).
/// </summary>
public class PdfWeeklyReportFixtureTests
{
    private static string WeeklyReportPath =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Еженедельный отчёт 1 весна.pdf");

    [SkippableFact]
    public void Extract_WeeklyReportFixture_ProducesLinesAndStyledText()
    {
        Skip.IfNot(File.Exists(WeeklyReportPath), $"Ожидался PDF: {WeeklyReportPath}");

        var bytes = File.ReadAllBytes(WeeklyReportPath);
        var layout = PdfFirstPageLayoutExtractor.Extract(bytes);

        var paths = CountPaths(WeeklyReportPath);
        Assert.True(
            layout.Lines.Count > 0 || paths.stroked > 0 || paths.filled > 0,
            $"Нет линий после экстракции; paths total={paths.total}, stroked={paths.stroked}, filled={paths.filled}, words={layout.Words.Count}");

        Assert.True(
            layout.Lines.Count >= 1,
            $"После фильтров линий должно быть ≥1, получено {layout.Lines.Count} (paths stroked={paths.stroked}).");

        var boldWords = layout.Words.Count(w => w.IsBold);
        var italicWords = layout.Words.Count(w => w.IsItalic);
        Assert.True(
            boldWords > 0 || italicWords > 0,
            $"Ожидалось хотя бы одно слово с bold/italic по метаданным/имени шрифта; bold={boldWords}, italic={italicWords}, words={layout.Words.Count}");
    }

    private static (int total, int stroked, int filled) CountPaths(string pdfPath)
    {
        using var ms = new MemoryStream(File.ReadAllBytes(pdfPath));
        using var document = PdfDocument.Open(ms);
        var page = document.GetPages().First();
        var total = 0;
        var stroked = 0;
        var filled = 0;
        foreach (var path in page.ExperimentalAccess.Paths)
        {
            total++;
            if (path.IsStroked)
                stroked++;
            if (path.IsFilled)
                filled++;
        }

        return (total, stroked, filled);
    }
}
