using RusalProject.Models.DTOs.Document;
using RusalProject.Models.Types;

namespace RusalProject.Services.Document;

public interface IDocumentService
{
    Task<DocumentDTO> CreateDocumentAsync(Guid userId, CreateDocumentDTO dto);
    Task<DocumentDTO?> GetDocumentByIdAsync(Guid documentId, Guid userId);
    Task<DocumentWithContentDTO?> GetDocumentWithContentAsync(Guid documentId, Guid userId);
    Task<List<DocumentDTO>> GetDocumentsAsync(Guid userId, string? status = null, string? search = null);
    Task<DocumentDTO> UpdateDocumentAsync(Guid documentId, Guid userId, UpdateDocumentDTO dto);
    Task UpdateDocumentContentAsync(Guid documentId, Guid userId, string content);
    Task UpdateDocumentOverridesAsync(Guid documentId, Guid userId, Dictionary<string, object> overrides);
    Task UpdateDocumentMetadataAsync(Guid documentId, Guid userId, DocumentMetadataDTO metadata);
    Task DeleteDocumentAsync(Guid documentId, Guid userId);
    Task RestoreDocumentAsync(Guid documentId, Guid userId);
    Task DeleteDocumentPermanentlyAsync(Guid documentId, Guid userId);
    Task ArchiveDocumentAsync(Guid documentId, Guid userId);
    Task UnarchiveDocumentAsync(Guid documentId, Guid userId);
    Task<bool> DocumentExistsAsync(Guid documentId, Guid userId);
    Task UpdatePdfPathAsync(Guid documentId, Guid userId, string pdfPath);
    Task<Stream> ExportDocumentAsync(Guid documentId, Guid userId);
    Task<DocumentDTO> ImportDocumentAsync(Guid userId, Stream ddocStream, string? documentName = null);

    Task<List<TocItem>?> GetTableOfContentsAsync(Guid documentId, Guid userId);
    Task<List<TocItem>> GenerateTableOfContentsAsync(Guid documentId, Guid userId);
    Task UpdateTableOfContentsAsync(Guid documentId, Guid userId, List<TocItem> items);
    Task<List<TocItem>> ResetTableOfContentsAsync(Guid documentId, Guid userId);

    Task<DocumentVersionDTO> SaveVersionAsync(Guid documentId, Guid userId, string name);
    Task<List<DocumentVersionDTO>> GetVersionsAsync(Guid documentId, Guid userId);
    Task<string> GetVersionContentAsync(Guid documentId, Guid versionId, Guid userId);
    Task RestoreVersionAsync(Guid documentId, Guid versionId, Guid userId);
    Task DeleteVersionAsync(Guid documentId, Guid versionId, Guid userId);
}
