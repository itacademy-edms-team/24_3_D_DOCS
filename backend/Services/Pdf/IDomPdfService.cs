namespace RusalProject.Services.Pdf;

public interface IDomPdfService
{
    Task<byte[]> GenerateDocumentPdfAsync(
        Guid documentId,
        Guid userId,
        string? accessToken,
        Guid? titlePageId = null
    );
}
