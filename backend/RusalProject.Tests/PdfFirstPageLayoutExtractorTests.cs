using RusalProject.Services.Pdf;
using Xunit;

namespace RusalProject.Tests;

public class PdfFirstPageLayoutExtractorTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "ПР3.pdf");

    [Fact]
    public void Extract_EmptyBytes_Throws()
    {
        Assert.Throws<ArgumentException>(() => PdfFirstPageLayoutExtractor.Extract([]));
    }

    [SkippableFact]
    public void ExtractFirstPage_FromLocalПР3_HasExpectedTextAndSeveralHorizontalLines()
    {
        Skip.IfNot(File.Exists(FixturePath), $"Положите ПР3.pdf в RusalProject.Tests/Fixtures/ (см. README). Ожидался путь: {FixturePath}");

        var bytes = File.ReadAllBytes(FixturePath);
        var result = PdfFirstPageLayoutExtractor.Extract(bytes);

        Assert.Equal(1, result.PageNumber);
        Assert.True(result.PageWidthPt > 0 && result.PageHeightPt > 0);
        Assert.NotEmpty(result.Words);

        var blob = string.Join(" ", result.Words.Select(w => w.Text));
        Assert.Contains("СИБИРСКИЙ", blob, StringComparison.Ordinal);
        Assert.Contains("ОТЧЕТ", blob, StringComparison.Ordinal);

        var horiz = result.Lines.Where(l => l.IsHorizontal).ToList();
        Assert.True(horiz.Count >= 6, $"Ожидалось не меньше 6 горизонтальных линий на титульнике, получено {horiz.Count}.");
    }
}
