namespace RusalProject.Models.DTOs.Agent;

public class DocumentEntityChangeDTO
{
    public string ChangeId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty; // insert | delete
    public string EntityType { get; set; } = string.Empty;
    public int StartLine { get; set; } // 1-based. For insert: line after which insert (0 means top of document).
    public int? EndLine { get; set; } // For delete ranges.
    public string Content { get; set; } = string.Empty; // Inserted or deleted entity content.
    public string? GroupId { get; set; } // Links entity-level chunks produced from one proposal call.
    public int? Order { get; set; } // Chunk order inside group (0-based).
}
