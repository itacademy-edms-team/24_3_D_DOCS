namespace RusalProject.Models.Types;

// Переопределения стилей для конкретных элементов по их ID
public class StyleOverrides
{
    // elementId -> частичный EntityStyle (только изменённые поля)
    public Dictionary<string, EntityStyle> Overrides { get; set; } = new();
}
