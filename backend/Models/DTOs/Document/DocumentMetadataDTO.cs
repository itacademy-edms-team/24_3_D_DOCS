namespace RusalProject.Models.DTOs.Document;

public class DocumentMetadataDTO
{
    public string? Title { get; set; } // Название работы
    public string? Author { get; set; } // Автор
    public string? Group { get; set; } // Группа
    public string? Year { get; set; } // Год
    public string? City { get; set; } // Город
    public string? Supervisor { get; set; } // Руководитель
    public string? DocumentType { get; set; } // Тип документа (Диплом, Курсовая, и т.д.)
    
    // Дополнительные поля для расширяемости
    public Dictionary<string, string>? AdditionalFields { get; set; }
}
