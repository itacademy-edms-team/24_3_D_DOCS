using RusalProject.Models.DTOs.Pdf;
using RusalProject.Services.Pdf;
using Xunit;

namespace RusalProject.Tests;

public class FirstPageLayoutToTitlePageMapperTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "ПР3.pdf");

    [Fact]
    public void Map_EmptyLayout_ProducesEmptyElements()
    {
        var layout = new FirstPageLayoutResult
        {
            PageNumber = 1,
            PageWidthPt = 595,
            PageHeightPt = 842,
            Words = [],
            Lines = [],
        };

        var data = FirstPageLayoutToTitlePageMapper.Map(layout);

        Assert.Empty(data.Elements);
    }

    [Fact]
    public void Map_HorizontalLineAndWord_ProducesLineThenText()
    {
        var layout = new FirstPageLayoutResult
        {
            PageNumber = 1,
            PageWidthPt = 100,
            PageHeightPt = 100,
            Words =
            [
                new FirstPageTextItem
                {
                    Text = "Hello",
                    XMmFromLeft = 10,
                    YMmFromTop = 20,
                    WidthMm = 30,
                    HeightMm = 5,
                    HeightPt = 12,
                },
            ],
            Lines =
            [
                new FirstPageLineSegment
                {
                    X1PtFromLeft = 72,
                    Y1PtFromTop = 36,
                    X2PtFromLeft = 144,
                    Y2PtFromTop = 36,
                    IsHorizontal = true,
                    IsVertical = false,
                    LineWidthPt = 1,
                },
            ],
        };

        var data = FirstPageLayoutToTitlePageMapper.Map(layout);

        Assert.Equal(2, data.Elements.Count);
        Assert.Equal("line", data.Elements[0].Type);
        Assert.Equal("text", data.Elements[1].Type);
        Assert.Equal("Hello", data.Elements[1].Text);
        Assert.True(data.Elements[0].Length > 0);
        Assert.Equal("Times New Roman", data.Elements[1].FontFamily);
        Assert.Null(data.Elements[1].Width);
    }

    [Fact]
    public void Map_PrefersNominalFontSizePt_OverBoundingBoxHeight()
    {
        var layout = new FirstPageLayoutResult
        {
            PageNumber = 1,
            PageWidthPt = 100,
            PageHeightPt = 100,
            Words =
            [
                new FirstPageTextItem
                {
                    Text = "Документ",
                    BottomPt = 0,
                    XPtFromLeft = 0,
                    XMmFromLeft = 0,
                    YMmFromTop = 50,
                    WidthMm = 40,
                    HeightPt = 9.7,
                    NominalFontSizePt = 12.0,
                },
            ],
            Lines = [],
        };

        var data = FirstPageLayoutToTitlePageMapper.Map(layout);
        var text = data.Elements.Single(e => e.Type == "text");

        Assert.Equal(12.0, text.FontSize);
    }

    [Fact]
    public void Map_SameBaselineWords_MergeIntoSingleLine_UnifiedFontSize()
    {
        var layout = new FirstPageLayoutResult
        {
            PageNumber = 1,
            PageWidthPt = 200,
            PageHeightPt = 200,
            Words =
            [
                new FirstPageTextItem
                {
                    Text = "и",
                    BottomPt = 100.0,
                    LeftPt = 10,
                    WidthPt = 4,
                    XPtFromLeft = 10,
                    XMmFromLeft = 3,
                    YMmFromTop = 40,
                    WidthMm = 2,
                    HeightMm = 2,
                    HeightPt = 4.5,
                },
                new FirstPageTextItem
                {
                    Text = "ПРАКТИЧЕСКОЙ",
                    BottomPt = 100.4,
                    LeftPt = 20,
                    WidthPt = 80,
                    XPtFromLeft = 20,
                    XMmFromLeft = 10,
                    YMmFromTop = 38,
                    WidthMm = 40,
                    HeightMm = 6,
                    HeightPt = 14.0,
                },
            ],
            Lines = [],
        };

        var data = FirstPageLayoutToTitlePageMapper.Map(layout);

        var texts = data.Elements.Where(e => e.Type == "text").ToList();
        Assert.Single(texts);
        Assert.Equal("и ПРАКТИЧЕСКОЙ", texts[0].Text);
        Assert.Equal(14.0, texts[0].FontSize);
        Assert.Null(texts[0].Width);
        Assert.Null(texts[0].Height);
    }

    [Fact]
    public void Map_ThreeWordsSameBaseline_OneElement_OrderedByX()
    {
        const double ptToMm = 25.4 / 72.0;
        var layout = new FirstPageLayoutResult
        {
            PageNumber = 1,
            PageWidthPt = 400,
            PageHeightPt = 400,
            Words =
            [
                new FirstPageTextItem
                {
                    Text = "gamma",
                    BottomPt = 50,
                    LeftPt = 90,
                    WidthPt = 25,
                    XPtFromLeft = 90,
                    XMmFromLeft = 90 * ptToMm,
                    YMmFromTop = 10,
                    HeightPt = 11,
                    NominalFontSizePt = 11,
                },
                new FirstPageTextItem
                {
                    Text = "alpha",
                    BottomPt = 50.5,
                    LeftPt = 10,
                    WidthPt = 28,
                    XPtFromLeft = 10,
                    XMmFromLeft = 10 * ptToMm,
                    YMmFromTop = 10,
                    HeightPt = 11,
                    NominalFontSizePt = 11,
                },
                new FirstPageTextItem
                {
                    Text = "beta",
                    BottomPt = 50.2,
                    LeftPt = 45,
                    WidthPt = 38,
                    XPtFromLeft = 45,
                    XMmFromLeft = 45 * ptToMm,
                    YMmFromTop = 10,
                    HeightPt = 11,
                    NominalFontSizePt = 11,
                },
            ],
            Lines = [],
        };

        var data = FirstPageLayoutToTitlePageMapper.Map(layout);
        var text = data.Elements.Single(e => e.Type == "text");
        Assert.Equal("alpha beta gamma", text.Text);
        Assert.Equal(10 * ptToMm, text.X, 4);
    }

    [Fact]
    public void Map_LargeHorizontalGap_SameBaseline_TwoTextElements()
    {
        var layout = new FirstPageLayoutResult
        {
            PageNumber = 1,
            PageWidthPt = 300,
            PageHeightPt = 300,
            Words =
            [
                new FirstPageTextItem
                {
                    Text = "LeftCol",
                    BottomPt = 80,
                    LeftPt = 10,
                    WidthPt = 40,
                    XPtFromLeft = 10,
                    XMmFromLeft = 3,
                    YMmFromTop = 5,
                    HeightPt = 12,
                },
                new FirstPageTextItem
                {
                    Text = "RightCol",
                    BottomPt = 80.2,
                    LeftPt = 100,
                    WidthPt = 50,
                    XPtFromLeft = 100,
                    XMmFromLeft = 35,
                    YMmFromTop = 5,
                    HeightPt = 12,
                },
            ],
            Lines = [],
        };

        var data = FirstPageLayoutToTitlePageMapper.Map(layout);
        var texts = data.Elements.Where(e => e.Type == "text").ToList();
        Assert.Equal(2, texts.Count);
        Assert.Equal("LeftCol", texts[0].Text);
        Assert.Equal("RightCol", texts[1].Text);
    }

    [Fact]
    public void Map_BoldThenNormal_SmallGap_TwoElementsWithStyles()
    {
        var layout = new FirstPageLayoutResult
        {
            PageNumber = 1,
            PageWidthPt = 300,
            PageHeightPt = 300,
            Words =
            [
                new FirstPageTextItem
                {
                    Text = "Жирный",
                    BottomPt = 120,
                    LeftPt = 10,
                    WidthPt = 44,
                    XPtFromLeft = 10,
                    XMmFromLeft = 2,
                    YMmFromTop = 1,
                    HeightPt = 12,
                    IsBold = true,
                },
                new FirstPageTextItem
                {
                    Text = "обычный",
                    BottomPt = 120.1,
                    LeftPt = 60,
                    WidthPt = 50,
                    XPtFromLeft = 60,
                    XMmFromLeft = 20,
                    YMmFromTop = 1,
                    HeightPt = 12,
                    IsBold = false,
                },
            ],
            Lines = [],
        };

        var data = FirstPageLayoutToTitlePageMapper.Map(layout);
        var texts = data.Elements.Where(e => e.Type == "text").OrderBy(t => t.X).ToList();
        Assert.Equal(2, texts.Count);
        Assert.Equal("Жирный", texts[0].Text);
        Assert.Equal("bold", texts[0].FontWeight);
        Assert.Equal("normal", texts[0].FontStyle);
        Assert.Equal("обычный", texts[1].Text);
        Assert.Equal("normal", texts[1].FontWeight);
    }

    [Fact]
    public void Map_ItalicWord_ItalicFontStyle()
    {
        var layout = new FirstPageLayoutResult
        {
            PageNumber = 1,
            PageWidthPt = 100,
            PageHeightPt = 100,
            Words =
            [
                new FirstPageTextItem
                {
                    Text = "Курсив",
                    BottomPt = 0,
                    XPtFromLeft = 0,
                    XMmFromLeft = 0,
                    YMmFromTop = 10,
                    HeightPt = 12,
                    IsItalic = true,
                },
            ],
            Lines = [],
        };

        var data = FirstPageLayoutToTitlePageMapper.Map(layout);
        var text = data.Elements.Single(e => e.Type == "text");
        Assert.Equal("italic", text.FontStyle);
        Assert.Equal("normal", text.FontWeight);
    }

    [Fact]
    public void Map_VerticalLine_ProducesVerticalElement()
    {
        var layout = new FirstPageLayoutResult
        {
            PageNumber = 1,
            PageWidthPt = 100,
            PageHeightPt = 100,
            Words = [],
            Lines =
            [
                new FirstPageLineSegment
                {
                    X1Pt = 50,
                    Y1Pt = 50,
                    X2Pt = 50,
                    Y2Pt = 10,
                    X1PtFromLeft = 50,
                    Y1PtFromTop = 50,
                    X2PtFromLeft = 50,
                    Y2PtFromTop = 90,
                    LineWidthPt = 1,
                    IsHorizontal = false,
                    IsVertical = true,
                },
            ],
        };

        var data = FirstPageLayoutToTitlePageMapper.Map(layout);

        var line = Assert.Single(data.Elements);
        Assert.Equal("line", line.Type);
        Assert.True(line.Vertical);
        const double PtToMm = 25.4 / 72.0;
        Assert.Equal(40 * PtToMm, line.Length!.Value, 3);
        Assert.Equal(50 * PtToMm, line.Y, 3);
    }

    [SkippableFact]
    public void Map_FromПР3Fixture_HasLinesAndTextElements()
    {
        Skip.IfNot(File.Exists(FixturePath), $"Положите ПР3.pdf в RusalProject.Tests/Fixtures/. Ожидался путь: {FixturePath}");

        var bytes = File.ReadAllBytes(FixturePath);
        var extracted = PdfFirstPageLayoutExtractor.Extract(bytes);
        var data = FirstPageLayoutToTitlePageMapper.Map(extracted);

        var lines = data.Elements.Count(e => e.Type == "line");
        var texts = data.Elements.Count(e => e.Type == "text");
        var wordCount = extracted.Words.Count;

        Assert.True(lines >= 6, $"Ожидалось не меньше 6 линий, получено {lines}.");
        Assert.True(texts >= 5, $"Ожидалось не меньше 5 текстовых строк, получено {texts}.");
        Assert.True(texts < wordCount, $"Строк после склейки должно быть меньше числа слов PdfPig: texts={texts}, words={wordCount}.");
    }
}
