using System.Text.Json.Serialization;

namespace RusalProject.Models.Types;

public class TocItem
{
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("pageNumber")]
    public int? PageNumber { get; set; }

    [JsonPropertyName("headingId")]
    public string? HeadingId { get; set; }

    [JsonPropertyName("isManual")]
    public bool IsManual { get; set; }
}
