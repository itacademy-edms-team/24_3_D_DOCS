namespace RusalProject.Models.DTOs.Document;

public class DocumentDTO
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProfileId { get; set; }
    public string? ProfileName { get; set; }
    public Guid? TitlePageId { get; set; }
    public string? TitlePageName { get; set; }
    public DocumentMetadataDTO? Metadata { get; set; }
    public string Status { get; set; } = "draft";
    public bool IsArchived { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool HasPdf { get; set; }
}

public class DocumentWithContentDTO : DocumentDTO
{
    public string? Content { get; set; } // Markdown контент
    public Dictionary<string, object>? StyleOverrides { get; set; } // Переопределения стилей
}

public class CreateDocumentDTO
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProfileId { get; set; }
    public Guid? TitlePageId { get; set; }
    public DocumentMetadataDTO? Metadata { get; set; }
    public string? InitialContent { get; set; } // Начальный Markdown контент
}

public class UpdateDocumentDTO
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? ProfileId { get; set; }
    public Guid? TitlePageId { get; set; }
    public DocumentMetadataDTO? Metadata { get; set; }
}

public class UpdateDocumentContentDTO
{
    public string Content { get; set; } = string.Empty;
}

public class UpdateDocumentOverridesDTO
{
    public Dictionary<string, object> Overrides { get; set; } = new();
}
