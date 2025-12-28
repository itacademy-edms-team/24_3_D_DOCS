using System.ComponentModel.DataAnnotations;

namespace RusalProject.Models.DTOs.RAG;

public class RAGSearchRequestDTO
{
    [Required]
    public Guid DocumentId { get; set; }

    [Required]
    public string Query { get; set; } = string.Empty;

    public int? TopK { get; set; } = 5;
}
