using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RusalProject.Models.Types;

namespace RusalProject.Models.Entities;

[Table("agent_source_parts")]
public class AgentSourcePart
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Column("part_index")]
    public int PartIndex { get; set; }

    [Required]
    [Column("kind")]
    [MaxLength(20)]
    public string Kind { get; set; } = string.Empty;

    [Required]
    [Column("mime_type")]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    [Column("label")]
    [MaxLength(500)]
    public string Label { get; set; } = string.Empty;

    [Column("storage_path")]
    [MaxLength(512)]
    public string? StoragePath { get; set; }

    [Column("inline_text")]
    public string? InlineText { get; set; }

    [ForeignKey("SessionId")]
    public virtual AgentSourceSession? Session { get; set; }

    public AgentSourcePartKind KindEnum =>
        Enum.TryParse<AgentSourcePartKind>(Kind, ignoreCase: true, out var k) ? k : AgentSourcePartKind.Text;
}
