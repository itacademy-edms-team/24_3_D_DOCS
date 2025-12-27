using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("attachments")]
public class Attachment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid CreatorId { get; set; }

    [Column("document_id")]
    public Guid? DocumentId { get; set; }

    [Required]
    [Column("file_name")]
    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [Column("content_type")]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    [Column("size")]
    public long Size { get; set; }

    [Required]
    [Column("storage_path")]
    public string StoragePath { get; set; } = string.Empty;

    [Column("version_number")]
    public int VersionNumber { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    [ForeignKey("CreatorId")]
    public virtual User? User { get; set; }

    [ForeignKey("DocumentId")]
    public virtual Document? Document { get; set; }
}
