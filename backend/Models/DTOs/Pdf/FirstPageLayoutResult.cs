namespace RusalProject.Models.DTOs.Pdf;

/// <summary>
/// Результат разбора первой страницы PDF: текстовый слой (слова) и отрезки линий.
/// Координаты PDF: начало в левом нижнем углу страницы, ось Y вверх, единица — пункт (1/72 дюйма).
/// Дополнительно дублируются величины от верхнего левого угла (как в типичных UI / вашем титульнике в мм сверху).
/// </summary>
public sealed class FirstPageLayoutResult
{
    public int PageNumber { get; init; }

    /// <summary>Ширина страницы, pt.</summary>
    public double PageWidthPt { get; init; }

    /// <summary>Высота страницы, pt.</summary>
    public double PageHeightPt { get; init; }

    public IReadOnlyList<FirstPageTextItem> Words { get; init; } = [];

    public IReadOnlyList<FirstPageLineSegment> Lines { get; init; } = [];
}

/// <summary>Слово с геометрией в pt.</summary>
public sealed class FirstPageTextItem
{
    public string Text { get; init; } = "";

    /// <summary>Левый край bbox, PDF user space (снизу).</summary>
    public double LeftPt { get; init; }

    /// <summary>Нижний край bbox, PDF user space.</summary>
    public double BottomPt { get; init; }

    public double WidthPt { get; init; }

    public double HeightPt { get; init; }

    /// <summary>
    /// Номинальный кегль в pt из PDF (<c>Letter.PointSize</c> у PdfPig), не путать с высотой bbox <see cref="HeightPt"/>.
    /// </summary>
    public double? NominalFontSizePt { get; init; }

    /// <summary>Жирное начертание по большинству букв слова (<c>FontDetails</c>).</summary>
    public bool IsBold { get; init; }

    /// <summary>Курсив по большинству букв слова.</summary>
    public bool IsItalic { get; init; }

    /// <summary>X от левого края страницы (то же, что LeftPt).</summary>
    public double XPtFromLeft { get; init; }

    /// <summary>Y верхней границы bbox от верхнего края страницы (вниз положительный).</summary>
    public double YPtFromTop { get; init; }

    public double WidthMm { get; init; }

    public double HeightMm { get; init; }

    public double XMmFromLeft { get; init; }

    public double YMmFromTop { get; init; }
}

/// <summary>Отрезок: концы в PDF pt (нижний левый origin) и дубликат от верхнего левого.</summary>
public sealed class FirstPageLineSegment
{
    public double X1Pt { get; init; }

    public double Y1Pt { get; init; }

    public double X2Pt { get; init; }

    public double Y2Pt { get; init; }

    public double X1PtFromLeft { get; init; }

    public double Y1PtFromTop { get; init; }

    public double X2PtFromLeft { get; init; }

    public double Y2PtFromTop { get; init; }

    public double? LineWidthPt { get; init; }

    public bool IsHorizontal { get; init; }

    public bool IsVertical { get; init; }
}
