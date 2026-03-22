using RusalProject.Models.Types;

namespace RusalProject.Models.DTOs.TitlePage;

/// <summary>Тело POST .../convert-to-variable</summary>
public class ConvertTitlePageElementToVariableRequest
{
    /// <summary>Явный ключ переменной (без обязательных фигурных скобок). Если не задан — берётся из текста элемента.</summary>
    public string? VariableKey { get; set; }
}

public class ConvertTitlePageElementToVariableResponse
{
    public TitlePageElement Element { get; set; } = null!;
}
