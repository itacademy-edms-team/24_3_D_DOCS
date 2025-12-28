namespace RusalProject.Services.Pdf;

public interface IPdfGeneratorService
{
    Task<byte[]> GeneratePdfAsync(Guid documentId, Guid userId, Guid? titlePageId = null);
    Task<byte[]> GenerateTitlePagePdfAsync(Guid titlePageId, Guid userId, Dictionary<string, string>? variables = null);
}
