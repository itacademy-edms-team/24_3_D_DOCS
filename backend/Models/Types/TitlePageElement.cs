namespace RusalProject.Models.Types;

public class TitlePageData
{
    public List<TitlePageElement> Elements { get; set; } = new();
}

public class TitlePageElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "text"; // text, variable, line
    public double X { get; set; } // мм от левого края
    public double Y { get; set; } // мм от верхнего края
    public double? Width { get; set; } // мм (для текстовых блоков)
    public double? Height { get; set; } // мм (опционально)
    
    // Для текста и переменных
    public string? Text { get; set; }
    public string? VariableType { get; set; } // Для переменных: Title, Author, Year, Group, etc.
    public string? Format { get; set; } // Для переменных: "г. {city}, {year}"
    
    // Стили текста
    public string? FontFamily { get; set; }
    public double? FontSize { get; set; } // pt
    public string? FontWeight { get; set; } // normal, bold
    public string? FontStyle { get; set; } // normal, italic
    public string? TextAlign { get; set; } // left, center, right
    public double? LineHeight { get; set; }
    public bool? AllCaps { get; set; } // Все заглавные
    
    // Для линий
    public double? Length { get; set; } // мм
    public double? Thickness { get; set; } // мм или pt
    public string? LineStyle { get; set; } // solid, dashed
    public bool? StretchToPageWidth { get; set; } // Растянуть по ширине страницы
}
