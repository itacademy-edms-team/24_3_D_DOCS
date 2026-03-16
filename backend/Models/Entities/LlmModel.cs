using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("llm_models")]
public class LlmModel
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("model_name")]
    public string ModelName { get; set; } = string.Empty;

    [Column("has_view")]
    public bool HasView { get; set; }
}
