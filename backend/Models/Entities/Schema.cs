using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("schemas")]
public class Schema
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("creator_id")]
    public Guid CreatorId { get; set; }

    [Required]
    [Column("schema_data")]
    public string SchemaData { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("CreatorId")]
    public virtual User? Creator { get; set; }
}

