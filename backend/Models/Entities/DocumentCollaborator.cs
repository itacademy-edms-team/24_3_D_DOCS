using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("document_collaborators")]
public class DocumentCollaborator
{
    public const string RoleEditor = "editor";
    public const string StatusPending = "pending";
    public const string StatusAccepted = "accepted";
    public const string StatusRevoked = "revoked";

    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("role")]
    [MaxLength(32)]
    public string Role { get; set; } = RoleEditor;

    [Required]
    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = StatusPending;

    [Required]
    [Column("invited_by")]
    public Guid InvitedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DocumentId))]
    public virtual DocumentLink? Document { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [ForeignKey(nameof(InvitedBy))]
    public virtual User? Inviter { get; set; }
}
