namespace RusalProject.Models.DTOs.TitlePages;

public class TitlePageElementDTO
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "text" | "variable" | "line"
    public double X { get; set; } // мм
    public double Y { get; set; } // мм
    
    // For text/variable:
    public string? Content { get; set; }
    public string? VariableKey { get; set; }
    public double? FontSize { get; set; }
    public string? FontFamily { get; set; }
    public string? FontWeight { get; set; }
    public string? FontStyle { get; set; }
    public double? LineHeight { get; set; }
    public string? TextAlign { get; set; }
    
    // For line:
    public double? Length { get; set; } // мм
    public double? Thickness { get; set; } // мм
}
