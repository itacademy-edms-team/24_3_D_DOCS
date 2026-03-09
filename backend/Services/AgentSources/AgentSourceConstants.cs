namespace RusalProject.Services.AgentSources;

public static class AgentSourceConstants
{
    public const long MaxUploadBytes = 25 * 1024 * 1024;
    public const int MaxInlineTextChars = 256_000;
    public const int MaxTextCharsForAttachmentLlm = 120_000;
    public const long MaxImageBytes = 12 * 1024 * 1024;
    public static readonly TimeSpan SessionTtl = TimeSpan.FromHours(72);

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".txt", ".md", ".png", ".jpg", ".jpeg", ".webp", ".gif"
    };
}
