namespace RusalProject.Services.Pdf;

public interface ITitlePagePdfService
{
    Task<byte[]> GenerateTitlePagePdfAsync(Guid titlePageId, Guid userId, Dictionary<string, string>? variables = null);
}
