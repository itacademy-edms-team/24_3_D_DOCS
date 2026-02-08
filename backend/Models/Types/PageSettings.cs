namespace RusalProject.Models.Types;

public class PageSettings
{
    public string Size { get; set; } = "A4"; // A4, A5, Letter
    public string Orientation { get; set; } = "portrait"; // portrait, landscape
    public PageMargins Margins { get; set; } = new();
    public PageNumberSettings PageNumbers { get; set; } = new();
    public double? GlobalLineHeight { get; set; } // Глобальный межстрочный интервал (множитель)
}

public class PageMargins
{
    public double Top { get; set; } = 20; // мм
    public double Right { get; set; } = 20;
    public double Bottom { get; set; } = 20;
    public double Left { get; set; } = 20;
}

public class PageNumberSettings
{
    public bool Enabled { get; set; } = true;
    public string Position { get; set; } = "bottom"; // top, bottom
    public string Align { get; set; } = "center"; // left, center, right
    public string Format { get; set; } = "{n}"; // Шаблон: {n}, Страница {n}
    public double FontSize { get; set; } = 12; // pt
    public string FontStyle { get; set; } = "normal"; // normal, italic
    public string FontFamily { get; set; } = "Times New Roman";
    public double? BottomOffset { get; set; } // Отступ снизу для номера страницы (px)
}
