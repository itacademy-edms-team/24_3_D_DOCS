namespace RusalProject.Models.Types;

public class EntityStyle
{
    // Типографика
    public string? FontFamily { get; set; }
    public double? FontSize { get; set; } // pt
    public string? FontWeight { get; set; } // normal, bold
    public string? FontStyle { get; set; } // normal, italic
    public string? TextAlign { get; set; } // left, center, right, justify
    public double? TextIndent { get; set; } // см (отступ первой строки)
    public double? LineHeight { get; set; } // множитель (1.5)
    public bool? LineHeightUseGlobal { get; set; } // Использовать глобальный межстрочный интервал
    public string? Color { get; set; } // hex цвет
    public string? BackgroundColor { get; set; }
    
    // Настройки выделения текста (==text==)
    public string? HighlightColor { get; set; } // hex цвет текста для выделения
    public string? HighlightBackgroundColor { get; set; } // hex цвет фона для выделения

    // Отступы
    public double? MarginTop { get; set; } // pt
    public double? MarginBottom { get; set; }
    public double? MarginLeft { get; set; }
    public double? MarginRight { get; set; }
    public double? PaddingLeft { get; set; } // pt (для списков)
    
    // Настройки отступов для списков
    public double? ListAdditionalIndent { get; set; } // мм, от -50 до 50 (добавочный отступ для каждого уровня)
    public bool? ListUseParagraphTextIndent { get; set; } // Использовать красную строку из параграфа

    // Границы (для таблиц и изображений)
    public double? BorderWidth { get; set; } // px
    public string? BorderColor { get; set; }
    public string? BorderStyle { get; set; } // solid, dashed, none

    // Размеры (для изображений)
    public double? MaxWidth { get; set; } // процент от ширины страницы
    
    // Шаблон подписи (для image-caption, table-caption, formula-caption)
    public string? CaptionFormat { get; set; } // Шаблон: "Рисунок {n} - {content}"
}
