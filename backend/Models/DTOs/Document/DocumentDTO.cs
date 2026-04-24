using RusalProject.Models.DTOs.Agent;

namespace RusalProject.Models.DTOs.Document;

public class DocumentDTO
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    /// <summary>True when the current user is an accepted collaborator (not the owner).</summary>
    public bool IsShared { get; set; }
    /// <summary>Document owner display name when <see cref="IsShared"/> is true.</summary>
    public string? OwnerName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProfileId { get; set; }
    public string? ProfileName { get; set; }
    public Guid? TitlePageId { get; set; }
    public string? TitlePageName { get; set; }
    public DocumentMetadataDTO? Metadata { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool HasPdf { get; set; }
}

public class DocumentWithContentDTO : DocumentDTO
{
    public string? Content { get; set; } // Markdown контент
    public Dictionary<string, object>? StyleOverrides { get; set; } // Переопределения стилей
    public List<DocumentEntityChangeDTO> AiChanges { get; set; } = new();
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
    /// <summary>Markdown snapshot when the document was opened (for three-way merge). Omit for legacy last-write-wins.</summary>
    public string? BaseContent { get; set; }

    public string Content { get; set; } = string.Empty;
}

public class UpdateDocumentContentResultDto
{
    public string Content { get; set; } = string.Empty;
    public bool HasConflicts { get; set; }
}

public class UpdateDocumentOverridesDTO
{
    public Dictionary<string, object> Overrides { get; set; } = new();
}

public class DocumentVersionDTO
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SaveDocumentVersionDTO
{
    public string Name { get; set; } = string.Empty;
}
