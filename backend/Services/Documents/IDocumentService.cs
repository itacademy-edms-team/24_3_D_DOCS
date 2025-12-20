using RusalProject.Models.DTOs.Documents;

namespace RusalProject.Services.Documents;

public interface IDocumentService
{
    Task<List<DocumentMetaDTO>> GetAllDocumentsAsync(Guid userId);
    Task<DocumentDTO?> GetDocumentByIdAsync(Guid id, Guid userId);
    Task<DocumentDTO> CreateDocumentAsync(CreateDocumentDTO dto, Guid userId);
    Task<DocumentDTO?> UpdateDocumentAsync(Guid id, UpdateDocumentDTO dto, Guid userId);
    Task<bool> DeleteDocumentAsync(Guid id, Guid userId);
    Task<string> UploadImageAsync(Guid documentId, Stream fileStream, string filename, Guid userId);
    Task<Stream?> GetImageAsync(Guid documentId, string imageId, Guid userId);
    Task<bool> DeleteImageAsync(Guid documentId, string imageId, Guid userId);
}
