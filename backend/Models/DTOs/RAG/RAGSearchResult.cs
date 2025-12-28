namespace RusalProject.Models.DTOs.RAG;

public class RAGSearchResult
{
    public Guid BlockId { get; set; }
    public string BlockType { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
    public double Score { get; set; }
}
