namespace RusalProject.Models.Types;

public class ParsedBlock
{
    public BlockType BlockType { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
}
