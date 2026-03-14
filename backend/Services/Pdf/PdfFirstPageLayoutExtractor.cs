using RusalProject.Models.DTOs.Pdf;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;

namespace RusalProject.Services.Pdf;

/// <summary>
/// Извлекает с первой страницы PDF слова (текстовый слой) и почти горизонтальные/вертикальные отрезки из path.
/// Без OCR. Остальные страницы не читаются.
/// </summary>
public static class PdfFirstPageLayoutExtractor
{
    private const double PtToMm = 25.4 / 72.0;

    private const double MinStrokeLineLengthPt = 8.0;

    private const double AxisEpsPt = 0.75;

    private const double ThinFilledRectMaxHeightPt = 3.0;

    private const double DedupEpsPt = 0.35;

    public static FirstPageLayoutResult Extract(byte[] pdfBytes)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        if (pdfBytes.Length == 0)
            throw new ArgumentException("PDF payload is empty.", nameof(pdfBytes));

        using var ms = new MemoryStream(pdfBytes, writable: false);
        using var document = PdfDocument.Open(ms);
        var page = document.GetPages().FirstOrDefault()
                   ?? throw new InvalidOperationException("PDF contains no pages.");

        var pageHeight = page.Height;
        var pageWidth = page.Width;

        var words = new List<FirstPageTextItem>();
        foreach (var word in page.GetWords())
        {
            var box = word.BoundingBox;
            if (box.Rotation != 0)
                continue;

            var left = box.Left;
            var bottom = box.Bottom;
            var w = box.Width;
            var h = box.Height;
            var topPdf = box.Top;
            var yFromTop = pageHeight - topPdf;

            double? nominalFontSizePt = null;
            var (isBold, isItalic) = (false, false);
            if (word.Letters is { Count: > 0 })
            {
                var sizes = word.Letters.Select(l => l.PointSize).Where(s => s > 0.01).ToList();
                if (sizes.Count > 0)
                    nominalFontSizePt = sizes.Max();

                (isBold, isItalic) = VoteFontStyleFromLetters(word.Letters);
            }

            words.Add(new FirstPageTextItem
            {
                Text = word.Text,
                LeftPt = left,
                BottomPt = bottom,
                WidthPt = w,
                HeightPt = h,
                NominalFontSizePt = nominalFontSizePt,
                IsBold = isBold,
                IsItalic = isItalic,
                XPtFromLeft = left,
                YPtFromTop = yFromTop,
                WidthMm = w * PtToMm,
                HeightMm = h * PtToMm,
                XMmFromLeft = left * PtToMm,
                YMmFromTop = yFromTop * PtToMm,
            });
        }

        var rawLines = new List<(double X1, double Y1, double X2, double Y2, double? Width, bool Horiz, bool Vert)>();
        CollectLinesFromPaths(page, rawLines);
        TryAddThinFilledHorizontalBars(page, rawLines);

        var deduped = DedupeSegments(rawLines);
        var lines = deduped
            .Select(s => ToLineDto(s, pageHeight))
            .ToList();

        return new FirstPageLayoutResult
        {
            PageNumber = page.Number,
            PageWidthPt = pageWidth,
            PageHeightPt = pageHeight,
            Words = words,
            Lines = lines,
        };
    }

    /// <summary>Большинство букв со шрифтом жирным/курсивным (PdfPig <see cref="UglyToad.PdfPig.Content.Letter.Font"/>).</summary>
    private static (bool Bold, bool Italic) VoteFontStyleFromLetters(IReadOnlyList<Letter> letters)
    {
        var n = 0;
        var boldVotes = 0;
        var italicVotes = 0;
        foreach (var letter in letters)
        {
            var font = letter.Font;
            if (font == null)
                continue;
            n++;
            if (font.IsBold || font.Weight > 500)
                boldVotes++;
            if (font.IsItalic)
                italicVotes++;
        }

        if (n == 0)
            return (false, false);

        return (boldVotes * 2 > n, italicVotes * 2 > n);
    }

    private static void CollectLinesFromPaths(Page page, List<(double X1, double Y1, double X2, double Y2, double? Width, bool Horiz, bool Vert)> rawLines)
    {
        foreach (var path in page.ExperimentalAccess.Paths)
        {
            if (!path.IsStroked)
                continue;

            double? lw = path.LineWidth > 0 ? (double)path.LineWidth : null;

            foreach (var subpath in path)
            {
                foreach (var cmd in subpath.Commands)
                {
                    switch (cmd)
                    {
                        case PdfSubpath.Line line:
                            AddIfAxisAligned(rawLines, line.From.X, line.From.Y, line.To.X, line.To.Y, lw);
                            break;
                        case PdfSubpath.BezierCurve curve:
                            AddIfAxisAligned(rawLines, curve.StartPoint.X, curve.StartPoint.Y, curve.EndPoint.X, curve.EndPoint.Y, lw);
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Горизонтальные линии из Google Docs / Word часто рисуются залитым тонким прямоугольником, а не stroke.
    /// </summary>
    private static void TryAddThinFilledHorizontalBars(Page page, List<(double X1, double Y1, double X2, double Y2, double? Width, bool Horiz, bool Vert)> rawLines)
    {
        foreach (var path in page.ExperimentalAccess.Paths)
        {
            if (!path.IsFilled || path.IsStroked)
                continue;

            foreach (var subpath in path)
            {
                if (!subpath.IsDrawnAsRectangle)
                    continue;

                var rect = subpath.GetDrawnRectangle();
                if (rect == null || rect.Value.Rotation != 0)
                    continue;

                var r = rect.Value;
                var h = r.Height;
                if (h > ThinFilledRectMaxHeightPt || r.Width < MinStrokeLineLengthPt)
                    continue;

                var yMid = r.Bottom + h / 2.0;
                AddIfAxisAligned(rawLines, r.Left, yMid, r.Right, yMid, null);
            }
        }
    }

    private static void AddIfAxisAligned(
        List<(double X1, double Y1, double X2, double Y2, double? Width, bool Horiz, bool Vert)> rawLines,
        double x1,
        double y1,
        double x2,
        double y2,
        double? lineWidth)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < MinStrokeLineLengthPt)
            return;

        var horiz = Math.Abs(dy) <= AxisEpsPt;
        var vert = Math.Abs(dx) <= AxisEpsPt;
        if (!horiz && !vert)
            return;

        if (horiz)
        {
            var y = (y1 + y2) / 2.0;
            var left = Math.Min(x1, x2);
            var right = Math.Max(x1, x2);
            rawLines.Add((left, y, right, y, lineWidth, true, false));
        }
        else
        {
            var x = (x1 + x2) / 2.0;
            var bottom = Math.Min(y1, y2);
            var top = Math.Max(y1, y2);
            rawLines.Add((x, bottom, x, top, lineWidth, false, true));
        }
    }

    private static List<(double X1, double Y1, double X2, double Y2, double? Width, bool Horiz, bool Vert)> DedupeSegments(
        List<(double X1, double Y1, double X2, double Y2, double? Width, bool Horiz, bool Vert)> raw)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<(double X1, double Y1, double X2, double Y2, double? Width, bool Horiz, bool Vert)>();
        foreach (var s in raw)
        {
            var key = s.Horiz
                ? $"H:{Round(s.Y1)}:{Round(s.X1)}:{Round(s.X2)}"
                : $"V:{Round(s.X1)}:{Round(s.Y1)}:{Round(s.Y2)}";
            if (set.Add(key))
                result.Add(s);
        }

        return result;

        static double Round(double v) => Math.Round(v / DedupEpsPt) * DedupEpsPt;
    }

    private static FirstPageLineSegment ToLineDto(
        (double X1, double Y1, double X2, double Y2, double? Width, bool Horiz, bool Vert) s,
        double pageHeight)
    {
        double ToTopY(double pdfY) => pageHeight - pdfY;

        return new FirstPageLineSegment
        {
            X1Pt = s.X1,
            Y1Pt = s.Y1,
            X2Pt = s.X2,
            Y2Pt = s.Y2,
            X1PtFromLeft = s.X1,
            Y1PtFromTop = ToTopY(s.Y1),
            X2PtFromLeft = s.X2,
            Y2PtFromTop = ToTopY(s.Y2),
            LineWidthPt = s.Width,
            IsHorizontal = s.Horiz,
            IsVertical = s.Vert,
        };
    }
}
