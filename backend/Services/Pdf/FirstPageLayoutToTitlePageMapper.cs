using RusalProject.Models.DTOs.Pdf;
using RusalProject.Models.Types;

namespace RusalProject.Services.Pdf;

/// <summary>
/// Преобразует разбор первой страницы PDF в данные титульника редактора (мм, верхний левый угол).
/// Текст склеивается по базовой линии; внутри линии разрыв по большому зазору по X (колонки подписей), затем по жирный/курсив.
/// Вертикальные линии пропускаются.
/// </summary>
public static class FirstPageLayoutToTitlePageMapper
{
    private const double PtToMm = 25.4 / 72.0;

    private const double DefaultLineThicknessMm = 0.25;

    private const double MinFontSizePt = 4.0;

    private const double MaxFontSizePt = 72.0;

    /// <summary>
    /// Слова с разницей базовой линии (PDF, pt) не больше этого значения считаются одной типографской строкой.
    /// </summary>
    private const double BaselineClusterEpsPt = 1.25;

    /// <summary>
    /// Доля кегля от базовой линии до визуального верха строки (аппроксимация ascent для TNR/кириллицы).
    /// </summary>
    private const double AscentRatio = 0.72;

    /// <summary>
    /// Если зазор между словами по X больше порога (pt), не склеивать — отдельные колонки (подписи, ФИО).
    /// ~6–8 мм; между обычными словами пробел меньше.
    /// </summary>
    private const double MaxHorizontalGapPt = 21.0;

    public static TitlePageData Map(FirstPageLayoutResult layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var elements = new List<TitlePageElement>();

        foreach (var seg in layout.Lines)
        {
            if (seg.IsHorizontal)
            {
                var leftPt = Math.Min(seg.X1PtFromLeft, seg.X2PtFromLeft);
                var rightPt = Math.Max(seg.X1PtFromLeft, seg.X2PtFromLeft);
                var lengthMm = (rightPt - leftPt) * PtToMm;
                if (lengthMm <= 0.01)
                    continue;

                var yTopPt = (seg.Y1PtFromTop + seg.Y2PtFromTop) / 2.0;
                var thicknessMm = seg.LineWidthPt is > 0
                    ? seg.LineWidthPt.Value * PtToMm
                    : DefaultLineThicknessMm;

                elements.Add(new TitlePageElement
                {
                    Type = "line",
                    X = leftPt * PtToMm,
                    Y = yTopPt * PtToMm,
                    Length = lengthMm,
                    Vertical = false,
                    Thickness = Math.Max(0.05, thicknessMm),
                    Stroke = "#000000",
                    LineStyle = "solid",
                });
            }
            else if (seg.IsVertical)
            {
                var lengthMm = Math.Abs(seg.Y1Pt - seg.Y2Pt) * PtToMm;
                if (lengthMm <= 0.01)
                    continue;

                var topFromTopPt = Math.Min(seg.Y1PtFromTop, seg.Y2PtFromTop);
                var thicknessMm = seg.LineWidthPt is > 0
                    ? seg.LineWidthPt.Value * PtToMm
                    : DefaultLineThicknessMm;

                elements.Add(new TitlePageElement
                {
                    Type = "line",
                    X = seg.X1PtFromLeft * PtToMm,
                    Y = topFromTopPt * PtToMm,
                    Length = lengthMm,
                    Vertical = true,
                    Thickness = Math.Max(0.05, thicknessMm),
                    Stroke = "#000000",
                    LineStyle = "solid",
                });
            }
        }

        foreach (var cluster in GetBaselineClusters(layout.Words))
        {
            var lineWords = cluster
                .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                .OrderBy(w => w.XPtFromLeft)
                .ToList();

            if (lineWords.Count == 0)
                continue;

            foreach (var hRun in SplitHorizontalRuns(lineWords))
            {
                foreach (var styleRun in SplitStyleRuns(hRun))
                {
                    if (styleRun.Count == 0)
                        continue;

                    var metrics = styleRun
                        .Select(EffectiveFontMetricPt)
                        .Where(h => h > 0.2)
                        .ToList();

                    var lineFontPt = metrics.Count == 0
                        ? MinFontSizePt
                        : Clamp(metrics.Max(), MinFontSizePt, MaxFontSizePt);

                    var joinedText = string.Join(' ',
                        styleRun.Select(w => w.Text.Trim()).Where(t => t.Length > 0));

                    if (joinedText.Length == 0)
                        continue;

                    var xMm = styleRun[0].XMmFromLeft;
                    var yMm = ComputeLineTopMm(layout.PageHeightPt, styleRun, lineFontPt);
                    var bold = styleRun[0].IsBold;
                    var italic = styleRun[0].IsItalic;

                    elements.Add(new TitlePageElement
                    {
                        Type = "text",
                        X = xMm,
                        Y = yMm,
                        Width = null,
                        Height = null,
                        Text = joinedText,
                        FontFamily = "Times New Roman",
                        FontSize = lineFontPt,
                        FontWeight = bold ? "bold" : "normal",
                        FontStyle = italic ? "italic" : "normal",
                        TextAlign = "left",
                        LineHeight = 1.2,
                    });
                }
            }
        }

        return new TitlePageData { Elements = elements };
    }

    /// <summary>
    /// Y верха строки в мм от верха страницы: из базовой линии PDF минус ascent, иначе min(YMmFromTop).
    /// </summary>
    private static double ComputeLineTopMm(double pageHeightPt, List<FirstPageTextItem> lineWords, double lineFontPt)
    {
        var avgBottom = lineWords.Average(w => w.BottomPt);

        // Синтетические тесты / отсутствие BottomPt в данных
        if (avgBottom <= 0.01 || pageHeightPt <= 0.01)
            return lineWords.Min(w => w.YMmFromTop);

        var baselineFromTopPt = pageHeightPt - avgBottom;
        var ascentPt = AscentRatio * lineFontPt;
        var topFromTopPt = baselineFromTopPt - ascentPt;
        return topFromTopPt * PtToMm;
    }

    /// <summary>Разбить отсортированный по X ряд слов на подстрочки по большому горизонтальному зазору.</summary>
    private static List<List<FirstPageTextItem>> SplitHorizontalRuns(List<FirstPageTextItem> sortedByX)
    {
        if (sortedByX.Count == 0)
            return [];

        var runs = new List<List<FirstPageTextItem>>();
        var current = new List<FirstPageTextItem> { sortedByX[0] };
        for (var k = 1; k < sortedByX.Count; k++)
        {
            var prev = sortedByX[k - 1];
            var next = sortedByX[k];
            var gap = next.LeftPt - (prev.LeftPt + prev.WidthPt);
            if (gap < 0)
                gap = 0;

            if (gap > MaxHorizontalGapPt)
            {
                runs.Add(current);
                current = new List<FirstPageTextItem>();
            }

            current.Add(next);
        }

        runs.Add(current);
        return runs;
    }

    /// <summary>Подряд идущие слова с одним (жирный, курсив) склеиваются; при смене — новый элемент.</summary>
    private static List<List<FirstPageTextItem>> SplitStyleRuns(List<FirstPageTextItem> run)
    {
        if (run.Count == 0)
            return [];

        var result = new List<List<FirstPageTextItem>>();
        var current = new List<FirstPageTextItem> { run[0] };
        for (var i = 1; i < run.Count; i++)
        {
            var prev = run[i - 1];
            var w = run[i];
            if (w.IsBold != prev.IsBold || w.IsItalic != prev.IsItalic)
            {
                result.Add(current);
                current = new List<FirstPageTextItem>();
            }

            current.Add(w);
        }

        result.Add(current);
        return result;
    }

    private static IEnumerable<List<FirstPageTextItem>> GetBaselineClusters(IReadOnlyList<FirstPageTextItem> words)
    {
        var items = words
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .OrderBy(w => w.BottomPt)
            .ThenBy(w => w.XPtFromLeft)
            .ToList();

        if (items.Count == 0)
            yield break;

        var i = 0;
        while (i < items.Count)
        {
            var baseline = items[i].BottomPt;
            var j = i + 1;
            while (j < items.Count && Math.Abs(items[j].BottomPt - baseline) <= BaselineClusterEpsPt)
                j++;

            yield return items.Skip(i).Take(j - i).ToList();
            i = j;
        }
    }

    private static double EffectiveFontMetricPt(FirstPageTextItem w)
    {
        if (w.NominalFontSizePt is { } n && n > 0.01)
            return n;
        return w.HeightPt;
    }

    private static double Clamp(double v, double min, double max) =>
        v < min ? min : v > max ? max : v;
}
