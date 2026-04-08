namespace RusalProject.Services.Document;

/// <summary>
/// Что включить в TAR (.ddoc) при экспорте или вложении в PDF.
/// </summary>
public sealed class DdocBundleOptions
{
    public bool IncludeDocument { get; init; } = true;
    public bool IncludeStyleProfile { get; init; } = true;
    public bool IncludeTitlePage { get; init; } = true;

    public bool AnyIncluded => IncludeDocument || IncludeStyleProfile || IncludeTitlePage;

    public static DdocBundleOptions Full => new()
    {
        IncludeDocument = true,
        IncludeStyleProfile = true,
        IncludeTitlePage = true,
    };
}
