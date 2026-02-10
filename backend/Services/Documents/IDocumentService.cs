using RusalProject.Models.DTOs.Document;

namespace RusalProject.Services.Documents;

public interface IDocumentService
{
    Task<List<DocumentDTO>> GetDocumentsAsync(Guid userId, string? status = null, string? search = null);
    Task<DocumentWithContentDTO?> GetDocumentWithContentAsync(Guid id, Guid userId);
    Task<DocumentDTO?> GetDocumentByIdAsync(Guid id, Guid userId);
    Task<DocumentDTO> CreateDocumentAsync(Guid userId, CreateDocumentDTO dto);
    Task<DocumentDTO> UpdateDocumentAsync(Guid id, Guid userId, UpdateDocumentDTO dto);
    Task UpdateDocumentContentAsync(Guid id, Guid userId, string content);
    Task UpdateDocumentOverridesAsync(Guid id, Guid userId, Dictionary<string, object> overrides);
    Task UpdateDocumentVariablesAsync(Guid id, Guid userId, Dictionary<string, string> variables);
    Task DeleteDocumentAsync(Guid id, Guid userId);
    Task RestoreDocumentAsync(Guid id, Guid userId);
    Task DeleteDocumentPermanentlyAsync(Guid id, Guid userId);
    Task ArchiveDocumentAsync(Guid id, Guid userId);
    Task UnarchiveDocumentAsync(Guid id, Guid userId);
    Task UpdatePdfPathAsync(Guid id, Guid userId, string? pdfPath);
    Task<byte[]> ExportDocumentAsync(Guid id, Guid userId);
    Task<DocumentDTO> ImportDocumentAsync(Guid userId, Stream fileStream, string filename);
    Task<bool> DocumentExistsAsync(Guid documentId, Guid userId);
    Task<string> UploadImageAsync(Guid documentId, Stream fileStream, string filename, Guid userId);
    Task<Stream?> GetImageAsync(Guid documentId, string imageId, Guid userId);
    Task<bool> DeleteImageAsync(Guid documentId, string imageId, Guid userId);
}
