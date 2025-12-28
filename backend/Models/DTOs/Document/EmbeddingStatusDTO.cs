namespace RusalProject.Models.DTOs.Document;

public class EmbeddingStatusDTO
{
    public double CoveragePercentage { get; set; }
    public int TotalLines { get; set; }
    public int CoveredLines { get; set; }
    public List<LineEmbeddingStatusDTO> LineStatuses { get; set; } = new();
}

public class LineEmbeddingStatusDTO
{
    public int LineNumber { get; set; } // 0-based
    public bool IsCovered { get; set; }
    public Guid? BlockId { get; set; }
    public bool IsEmpty { get; set; }
}
