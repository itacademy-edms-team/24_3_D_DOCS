namespace RusalProject.Services.Document;

/// <summary>
/// Что включить в TAR (.ddoc) при экспорте или вложении в PDF.
/// </summary>
public sealed class DdocBundleOptions
{
    public bool IncludeDocument { get; init; } = true;
    public bool IncludeStyleProfile { get; init; } = true;
    public bool IncludeTitlePage { get; init; } = true;

    /// <summary>Подмена профиля в profile.json относительно привязки документа.</summary>
    public Guid? ExportStyleProfileId { get; init; }

    /// <summary>Подмена титульника в titlepage.json относительно привязки документа.</summary>
    public Guid? ExportTitlePageId { get; init; }

    public bool AnyIncluded => IncludeDocument || IncludeStyleProfile || IncludeTitlePage;

    public static DdocBundleOptions Full => new()
    {
        IncludeDocument = true,
        IncludeStyleProfile = true,
        IncludeTitlePage = true,
    };
}
