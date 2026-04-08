using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Formats.Tar;
using System.Threading;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.DTOs.Document;
using RusalProject.Models.Entities;
using RusalProject.Models.Exceptions;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;
using RusalProject.Services.Storage;
using RusalProject.Services.Profile;
using RusalProject.Services.TitlePage;
using RusalProject.Models.DTOs.Profile;
using RusalProject.Models.DTOs.TitlePage;
using System.IO;

namespace RusalProject.Services.Document;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;
    private readonly IProfileService _profileService;
    private readonly ITitlePageService _titlePageService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IMinioService minioService,
        IProfileService profileService,
        ITitlePageService titlePageService,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _minioService = minioService;
        _profileService = profileService;
        _titlePageService = titlePageService;
        _logger = logger;
    }

    private string GetUserBucket(Guid userId) => $"user-{userId}";

    public async Task<DocumentDTO> CreateDocumentAsync(Guid userId, CreateDocumentDTO dto)
    {
        var document = new DocumentLink
        {
            CreatorId = userId,
            Name = dto.Name,
            Description = dto.Description,
            ProfileId = dto.ProfileId,
            TitlePageId = dto.TitlePageId,
            IsArchived = false,
            MdMinioPath = $"documents/{Guid.NewGuid()}/content.md"
        };

        if (dto.Metadata != null)
        {
            document.Metadata = JsonSerializer.Serialize(dto.Metadata);
        }

        _context.DocumentLinks.Add(document);
        await _context.SaveChangesAsync();

        // Создаём бакет пользователя если его нет
        var bucket = GetUserBucket(userId);
        await _minioService.EnsureBucketExistsAsync(bucket);

        // Сохраняем контент в MinIO
        var content = dto.InitialContent ?? string.Empty;
        var contentBytes = Encoding.UTF8.GetBytes(content);
        using var contentStream = new MemoryStream(contentBytes);
        await _minioService.UploadFileAsync(bucket, document.MdMinioPath, contentStream, "text/markdown");

        // Создаём пустой файл overrides.json
        var overridesPath = document.MdMinioPath.Replace("content.md", "overrides.json");
        var emptyOverrides = new StyleOverrides();
        var overridesJson = JsonSerializer.Serialize(emptyOverrides);
        var overridesBytes = Encoding.UTF8.GetBytes(overridesJson);
        using var overridesStream = new MemoryStream(overridesBytes);
        await _minioService.UploadFileAsync(bucket, overridesPath, overridesStream, "application/json");

        _logger.LogInformation("Created document {DocumentId} for user {UserId}", document.Id, userId);

        // Перезагружаем документ с навигационными свойствами для маппинга
        var documentWithNav = await _context.DocumentLinks
            .Include(d => d.Profile)
            .Include(d => d.TitlePage)
            .FirstOrDefaultAsync(d => d.Id == document.Id);

        if (documentWithNav == null)
            throw new InvalidOperationException($"Document {document.Id} not found after creation");

        return await MapToDTOAsync(documentWithNav);
    }

    public async Task<DocumentDTO?> GetDocumentByIdAsync(Guid documentId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .Include(d => d.Profile)
            .Include(d => d.TitlePage)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null) return null;

        return await MapToDTOAsync(document);
    }

    public async Task<DocumentWithContentDTO?> GetDocumentWithContentAsync(Guid documentId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .Include(d => d.Profile)
            .Include(d => d.TitlePage)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null) return null;

        var dto = await MapToDTOAsync(document);
        var withContent = new DocumentWithContentDTO
        {
            Id = dto.Id,
            CreatorId = dto.CreatorId,
            Name = dto.Name,
            Description = dto.Description,
            ProfileId = dto.ProfileId,
            ProfileName = dto.ProfileName,
            TitlePageId = dto.TitlePageId,
            TitlePageName = dto.TitlePageName,
            Metadata = dto.Metadata,
            IsArchived = dto.IsArchived,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            HasPdf = dto.HasPdf
        };

        // Загружаем контент из MinIO
        try
        {
            var bucket = GetUserBucket(userId);
            using var contentStream = await _minioService.DownloadFileAsync(bucket, document.MdMinioPath);
            using var reader = new StreamReader(contentStream, Encoding.UTF8);
            withContent.Content = await reader.ReadToEndAsync();
        }
        catch (FileNotFoundException)
        {
            withContent.Content = string.Empty;
        }

        // Загружаем overrides
        try
        {
            var bucket = GetUserBucket(userId);
            var overridesPath = document.MdMinioPath.Replace("content.md", "overrides.json");
            using var overridesStream = await _minioService.DownloadFileAsync(bucket, overridesPath);
            using var reader = new StreamReader(overridesStream, Encoding.UTF8);
            var overridesJson = await reader.ReadToEndAsync();
            var overrides = JsonSerializer.Deserialize<StyleOverrides>(overridesJson);
            if (overrides?.Overrides != null)
            {
                withContent.StyleOverrides = overrides.Overrides.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)kvp.Value
                );
            }
        }
        catch (FileNotFoundException)
        {
            withContent.StyleOverrides = new Dictionary<string, object>();
        }

        withContent.AiChanges = await GetPendingDocumentChangesAsync(documentId, userId);

        return withContent;
    }

    public async Task<List<DocumentDTO>> GetDocumentsAsync(Guid userId, string? status = null, string? search = null)
    {
        var query = _context.DocumentLinks
            .Include(d => d.Profile)
            .Include(d => d.TitlePage)
            .Where(d => d.CreatorId == userId);

        // Фильтр по статусу (all, archived)
        if (status == "archived")
        {
            query = query.Where(d => d.IsArchived);
        }
        else if (status != "all")
        {
            query = query.Where(d => !d.IsArchived);
        }

        // Поиск по названию
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => d.Name.Contains(search));
        }

        var documents = await query
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();

        var result = new List<DocumentDTO>();
        foreach (var doc in documents)
        {
            result.Add(await MapToDTOAsync(doc));
        }

        return result;
    }

    public async Task<DocumentDTO> UpdateDocumentAsync(Guid documentId, Guid userId, UpdateDocumentDTO dto)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        if (dto.Name != null) document.Name = dto.Name;
        if (dto.Description != null) document.Description = dto.Description;
        if (dto.ProfileId.HasValue) document.ProfileId = dto.ProfileId;
        if (dto.TitlePageId.HasValue) document.TitlePageId = dto.TitlePageId;
        if (dto.Metadata != null)
        {
            document.Metadata = JsonSerializer.Serialize(dto.Metadata);
        }

        await _context.SaveChangesAsync();

        return await MapToDTOAsync(document);
    }

    public async Task UpdateDocumentContentAsync(Guid documentId, Guid userId, string content)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var bucket = GetUserBucket(userId);
        _logger.LogInformation("UpdateDocumentContentAsync: Saving content for document {DocumentId} to bucket {Bucket}, path {Path}, content length: {ContentLength}", 
            documentId, bucket, document.MdMinioPath, content.Length);
        
        var contentBytes = Encoding.UTF8.GetBytes(content);
        using var contentStream = new MemoryStream(contentBytes);
        await _minioService.UploadFileAsync(bucket, document.MdMinioPath, contentStream, "text/markdown");

        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("UpdateDocumentContentAsync: Content saved successfully to MinIO");
    }

    public async Task AddPendingDocumentChangesAsync(Guid documentId, Guid userId, IReadOnlyCollection<DocumentEntityChangeDTO> changes)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        if (changes.Count == 0)
            return;

        var entities = changes.Select(change => new DocumentAiChange
        {
            DocumentId = documentId,
            ChangeId = change.ChangeId,
            ChangeType = change.ChangeType,
            EntityType = change.EntityType,
            StartLine = change.StartLine,
            EndLine = change.EndLine,
            Content = change.Content,
            GroupId = change.GroupId,
            Order = change.Order,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.DocumentAiChanges.AddRange(entities);
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<List<DocumentEntityChangeDTO>> GetPendingDocumentChangesAsync(Guid documentId, Guid userId)
    {
        var documentExists = await _context.DocumentLinks
            .AnyAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (!documentExists)
            throw new FileNotFoundException($"Document {documentId} not found");

        return await _context.DocumentAiChanges
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.StartLine)
            .ThenBy(c => c.Order ?? 0)
            .Select(c => new DocumentEntityChangeDTO
            {
                ChangeId = c.ChangeId,
                ChangeType = c.ChangeType,
                EntityType = c.EntityType,
                StartLine = c.StartLine,
                EndLine = c.EndLine,
                Content = c.Content,
                GroupId = c.GroupId,
                Order = c.Order
            })
            .ToListAsync();
    }

    public async Task AcceptPendingDocumentChangeAsync(Guid documentId, Guid userId, string changeId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var allPendingChanges = await _context.DocumentAiChanges
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.StartLine)
            .ThenBy(c => c.Order ?? 0)
            .ToListAsync();

        var targetChange = allPendingChanges.FirstOrDefault(c => c.ChangeId == changeId);
        if (targetChange == null)
        {
            _logger.LogInformation("AcceptPendingDocumentChangeAsync: change {ChangeId} already removed, treating as no-op", changeId);
            return;
        }

        var groupKey = targetChange.GroupId ?? targetChange.ChangeId;
        var groupChanges = allPendingChanges
            .Where(c => (c.GroupId ?? c.ChangeId) == groupKey)
            .OrderBy(c => c.Order ?? 0)
            .ThenBy(c => c.CreatedAt)
            .ToList();

        var currentDocument = await GetDocumentWithContentAsync(documentId, userId);
        var currentContent = currentDocument?.Content ?? string.Empty;
        var updatedContent = ApplyAcceptedChangeGroup(currentContent, groupChanges);

        var bucket = GetUserBucket(userId);
        var contentBytes = Encoding.UTF8.GetBytes(updatedContent);
        using var contentStream = new MemoryStream(contentBytes);
        await _minioService.UploadFileAsync(bucket, document.MdMinioPath, contentStream, "text/markdown");

        var remainingChanges = allPendingChanges
            .Where(c => (c.GroupId ?? c.ChangeId) != groupKey)
            .ToList();

        RebasePendingChangesAfterAccept(remainingChanges, groupChanges);

        _context.DocumentAiChanges.RemoveRange(groupChanges);
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task RejectPendingDocumentChangeAsync(Guid documentId, Guid userId, string changeId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var allPendingChanges = await _context.DocumentAiChanges
            .Where(c => c.DocumentId == documentId)
            .ToListAsync();

        var targetChange = allPendingChanges.FirstOrDefault(c => c.ChangeId == changeId);
        if (targetChange == null)
        {
            _logger.LogInformation("RejectPendingDocumentChangeAsync: change {ChangeId} already removed, treating as no-op", changeId);
            return;
        }

        var groupKey = targetChange.GroupId ?? targetChange.ChangeId;
        var groupChanges = allPendingChanges
            .Where(c => (c.GroupId ?? c.ChangeId) == groupKey)
            .ToList();

        _context.DocumentAiChanges.RemoveRange(groupChanges);
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDocumentOverridesAsync(Guid documentId, Guid userId, Dictionary<string, object> overrides)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var bucket = GetUserBucket(userId);
        var overridesPath = document.MdMinioPath.Replace("content.md", "overrides.json");
        
        var styleOverrides = new StyleOverrides
        {
            Overrides = overrides.ToDictionary(
                kvp => kvp.Key,
                kvp => JsonSerializer.Deserialize<EntityStyle>(JsonSerializer.Serialize(kvp.Value)) ?? new EntityStyle()
            )
        };

        var overridesJson = JsonSerializer.Serialize(styleOverrides);
        var overridesBytes = Encoding.UTF8.GetBytes(overridesJson);
        using var overridesStream = new MemoryStream(overridesBytes);
        await _minioService.UploadFileAsync(bucket, overridesPath, overridesStream, "application/json");
    }

    public async Task UpdateDocumentMetadataAsync(Guid documentId, Guid userId, DocumentMetadataDTO metadata)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        document.Metadata = JsonSerializer.Serialize(metadata);
        await _context.SaveChangesAsync();
    }

    public async Task<DocumentVersionDTO> SaveVersionAsync(Guid documentId, Guid userId, string name)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var bucket = GetUserBucket(userId);

        // Read current content from MinIO
        string content;
        try
        {
            using var contentStream = await _minioService.DownloadFileAsync(bucket, document.MdMinioPath);
            using var reader = new StreamReader(contentStream, Encoding.UTF8);
            content = await reader.ReadToEndAsync();
        }
        catch (FileNotFoundException)
        {
            content = string.Empty;
        }

        // Check for duplicate content in existing versions
        var existingVersions = await _context.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .ToListAsync();
        foreach (var ver in existingVersions)
        {
            string existingContent;
            try
            {
                using var existingStream = await _minioService.DownloadFileAsync(bucket, ver.ContentMinioPath);
                using var existingReader = new StreamReader(existingStream, Encoding.UTF8);
                existingContent = await existingReader.ReadToEndAsync();
            }
            catch (FileNotFoundException)
            {
                existingContent = string.Empty;
            }
            if (existingContent == content)
            {
                throw new DuplicateContentException("Версия с таким содержимым уже существует");
            }
        }

        var versionId = Guid.NewGuid();
        var documentPrefix = document.MdMinioPath.Substring(0, document.MdMinioPath.LastIndexOf('/'));
        var versionPath = $"{documentPrefix}/versions/{versionId}.md";

        var version = new DocumentVersion
        {
            Id = versionId,
            DocumentId = documentId,
            Name = name.Trim(),
            ContentMinioPath = versionPath
        };

        _context.DocumentVersions.Add(version);
        await _context.SaveChangesAsync();

        var contentBytes = Encoding.UTF8.GetBytes(content);
        using var versionStream = new MemoryStream(contentBytes);
        await _minioService.UploadFileAsync(bucket, versionPath, versionStream, "text/markdown");

        _logger.LogInformation("Saved version {VersionId} for document {DocumentId}", versionId, documentId);

        return new DocumentVersionDTO
        {
            Id = version.Id,
            DocumentId = version.DocumentId,
            Name = version.Name,
            CreatedAt = version.CreatedAt
        };
    }

    public async Task<List<DocumentVersionDTO>> GetVersionsAsync(Guid documentId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            return new List<DocumentVersionDTO>();

        var versions = await _context.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        return versions.Select(v => new DocumentVersionDTO
        {
            Id = v.Id,
            DocumentId = v.DocumentId,
            Name = v.Name,
            CreatedAt = v.CreatedAt
        }).ToList();
    }

    public async Task<string> GetVersionContentAsync(Guid documentId, Guid versionId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var version = await _context.DocumentVersions
            .FirstOrDefaultAsync(v => v.Id == versionId && v.DocumentId == documentId);

        if (version == null)
            throw new FileNotFoundException($"Version {versionId} not found");

        var bucket = GetUserBucket(userId);

        try
        {
            using var contentStream = await _minioService.DownloadFileAsync(bucket, version.ContentMinioPath);
            using var reader = new StreamReader(contentStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
        catch (FileNotFoundException)
        {
            return string.Empty;
        }
    }

    public async Task RestoreVersionAsync(Guid documentId, Guid versionId, Guid userId)
    {
        var content = await GetVersionContentAsync(documentId, versionId, userId);

        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var bucket = GetUserBucket(userId);
        var contentBytes = Encoding.UTF8.GetBytes(content);
        using var contentStream = new MemoryStream(contentBytes);
        await _minioService.UploadFileAsync(bucket, document.MdMinioPath, contentStream, "text/markdown");

        document.UpdatedAt = DateTime.UtcNow;
        await ClearPendingDocumentChangesAsync(documentId);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Restored version {VersionId} for document {DocumentId}", versionId, documentId);
    }

    public async Task DeleteVersionAsync(Guid documentId, Guid versionId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var version = await _context.DocumentVersions
            .FirstOrDefaultAsync(v => v.Id == versionId && v.DocumentId == documentId);

        if (version == null)
            throw new FileNotFoundException($"Version {versionId} not found");

        var bucket = GetUserBucket(userId);

        try
        {
            await _minioService.DeleteFileAsync(bucket, version.ContentMinioPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete version file from MinIO {Path}", version.ContentMinioPath);
        }

        _context.DocumentVersions.Remove(version);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted version {VersionId} for document {DocumentId}", versionId, documentId);
    }

    public async Task<List<TocItem>?> GetTableOfContentsAsync(Guid documentId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null) return null;

        var bucket = GetUserBucket(userId);
        var tocPath = document.MdMinioPath.Replace("content.md", "toc.json");

        try
        {
            using var stream = await _minioService.DownloadFileAsync(bucket, tocPath);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<List<TocItem>>(json);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public async Task<List<TocItem>> GenerateTableOfContentsAsync(Guid documentId, Guid userId)
    {
        var withContent = await GetDocumentWithContentAsync(documentId, userId);
        if (withContent == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var content = withContent.Content ?? string.Empty;
        var items = ParseHeadingsFromMarkdown(content);

        await SaveTableOfContentsAsync(documentId, userId, items);
        return items;
    }

    public async Task UpdateTableOfContentsAsync(Guid documentId, Guid userId, List<TocItem> items)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        await SaveTableOfContentsAsync(documentId, userId, items);
    }

    public async Task<List<TocItem>> ResetTableOfContentsAsync(Guid documentId, Guid userId)
    {
        return await GenerateTableOfContentsAsync(documentId, userId);
    }

    private static List<TocItem> ParseHeadingsFromMarkdown(string markdown)
    {
        var items = new List<TocItem>();
        var headingRegex = new Regex(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline);
        var matches = headingRegex.Matches(markdown);

        var usedIds = new HashSet<string>();
        var levelCounters = new int[6];

        foreach (Match match in matches)
        {
            var level = match.Groups[1].Length;
            var text = match.Groups[2].Value.Trim();
            if (string.IsNullOrEmpty(text)) continue;

            // Strip redundant manual numbering (e.g. "4.1. ", "1. ") - TOC provides its own
            text = Regex.Replace(text, @"^\d+(\.\d+)*\.\s*", string.Empty).Trim();
            if (string.IsNullOrEmpty(text)) continue;

            var slug = text.Length > 50 ? text[..50] : text;
            var baseId = $"h{level}-{slug.Replace(" ", "-")}";
            var headingId = baseId;
            var counter = 0;
            while (usedIds.Contains(headingId))
            {
                counter++;
                headingId = $"{baseId}-{counter}";
            }
            usedIds.Add(headingId);

            items.Add(new TocItem
            {
                Level = level,
                Text = text,
                HeadingId = headingId,
                IsManual = false
            });
        }

        return items;
    }

    private async Task SaveTableOfContentsAsync(Guid documentId, Guid userId, List<TocItem> items)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var bucket = GetUserBucket(userId);
        var tocPath = document.MdMinioPath.Replace("content.md", "toc.json");

        var json = JsonSerializer.Serialize(items);
        var bytes = Encoding.UTF8.GetBytes(json);
        using var stream = new MemoryStream(bytes);
        await _minioService.UploadFileAsync(bucket, tocPath, stream, "application/json");
    }

    public async Task DeleteDocumentAsync(Guid documentId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var bucket = GetUserBucket(userId);

        var attachments = await _context.Attachments
            .Where(a => a.DocumentId == documentId && a.CreatorId == userId)
            .ToListAsync();
        foreach (var attachment in attachments)
        {
            try
            {
                await _minioService.DeleteFileAsync(bucket, attachment.StoragePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete attachment from MinIO {Path}", attachment.StoragePath);
            }
        }

        var documentPrefix = document.MdMinioPath.Substring(0, document.MdMinioPath.LastIndexOf('/'));
        try
        {
            await _minioService.DeleteDirectoryAsync(bucket, documentPrefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete document directory from MinIO {Prefix}", documentPrefix);
        }

        var byIdPrefix = $"documents/{documentId}";
        if (!string.Equals(documentPrefix.TrimEnd('/'), byIdPrefix, StringComparison.Ordinal))
        {
            try
            {
                await _minioService.DeleteDirectoryAsync(bucket, byIdPrefix);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete document id prefix from MinIO {Prefix}", byIdPrefix);
            }
        }

        if (!string.IsNullOrEmpty(document.PdfMinioPath))
        {
            try
            {
                await _minioService.DeleteFileAsync(bucket, document.PdfMinioPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete PDF from MinIO {Path}", document.PdfMinioPath);
            }
        }

        _context.DocumentLinks.Remove(document);
        await _context.SaveChangesAsync();
    }

    public async Task ArchiveDocumentAsync(Guid documentId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        document.IsArchived = true;
        await _context.SaveChangesAsync();
    }

    public async Task UnarchiveDocumentAsync(Guid documentId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        document.IsArchived = false;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DocumentExistsAsync(Guid documentId, Guid userId)
    {
        return await _context.DocumentLinks
            .AnyAsync(d => d.Id == documentId && d.CreatorId == userId);
    }

    public async Task UpdatePdfPathAsync(Guid documentId, Guid userId, string pdfPath)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        document.PdfMinioPath = pdfPath;
        await _context.SaveChangesAsync();
    }

    private Task<DocumentDTO> MapToDTOAsync(DocumentLink document)
    {
        var dto = new DocumentDTO
        {
            Id = document.Id,
            CreatorId = document.CreatorId,
            Name = document.Name,
            Description = document.Description,
            ProfileId = document.ProfileId,
            ProfileName = document.Profile?.Name,
            TitlePageId = document.TitlePageId,
            TitlePageName = document.TitlePage?.Name,
            IsArchived = document.IsArchived,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            HasPdf = !string.IsNullOrEmpty(document.PdfMinioPath)
        };

        if (!string.IsNullOrEmpty(document.Metadata))
        {
            try
            {
                dto.Metadata = JsonSerializer.Deserialize<DocumentMetadataDTO>(document.Metadata);
            }
            catch
            {
                // Игнорируем ошибки десериализации
            }
        }

        return Task.FromResult(dto);
    }

    public async Task<Stream> ExportDocumentAsync(Guid documentId, Guid userId) =>
        await ExportDocumentAsync(documentId, userId, DdocBundleOptions.Full);

    public async Task<Stream> ExportDocumentAsync(Guid documentId, Guid userId, DdocBundleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.AnyIncluded)
            throw new InvalidOperationException("At least one ddoc bundle part must be included.");

        var document = await _context.DocumentLinks
            .Include(d => d.Profile)
            .Include(d => d.TitlePage)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
            throw new FileNotFoundException($"Document {documentId} not found");

        var documentWithContent = options.IncludeDocument
            ? await GetDocumentWithContentAsync(documentId, userId)
            : null;

        if (options.IncludeDocument && documentWithContent == null)
            throw new FileNotFoundException($"Document content not found for {documentId}");

        var content = options.IncludeDocument
            ? (documentWithContent!.Content ?? string.Empty)
            : $"# {document.Name}\n";
        var bucket = GetUserBucket(userId);

        var imageMap = new Dictionary<string, string>();
        if (options.IncludeDocument)
        {
            var imagePattern = @"!\[([^\]]*)\]\(([^)]+)\)";
            var regex = new Regex(imagePattern);
            var imageMatches = regex.Matches(content);
            foreach (Match match in imageMatches)
            {
                var url = match.Groups[2].Value;
                var fileName = ExtractFileNameFromUrl(url, documentId);
                if (!string.IsNullOrWhiteSpace(fileName) && !imageMap.ContainsKey(url))
                    imageMap[url] = fileName;
            }
        }

        // Создаем TAR архив в памяти
        var tarStream = new MemoryStream();
        using (var tarWriter = new TarWriter(tarStream, leaveOpen: true))
        {
            var modifiedContent = content;
            if (options.IncludeDocument)
            {
                foreach (var kvp in imageMap)
                {
                    var oldUrl = kvp.Key;
                    var fileName = kvp.Value;
                    var newPath = $"assets/{fileName}";
                    modifiedContent = modifiedContent.Replace($"]({oldUrl})", $"]({newPath})");
                }
            }

            // Вспомогательная функция для записи файла в TAR
            // В .NET 9.0 используем правильный API TarWriter
            async Task WriteFileToTarAsync(string entryName, Stream dataStream)
            {
                dataStream.Position = 0;
                
                // Читаем все данные из потока в массив байтов
                using var buffer = new MemoryStream();
                await dataStream.CopyToAsync(buffer);
                var dataBytes = buffer.ToArray();
                
                // Создаём entry
                var entry = new UstarTarEntry(TarEntryType.RegularFile, entryName);
                
                // В .NET 9.0 DataStream доступен только для чтения после создания entry
                // Используем рефлексию для установки DataStream с данными
                var dataStreamProperty = typeof(UstarTarEntry).GetProperty("DataStream", BindingFlags.Public | BindingFlags.Instance);
                if (dataStreamProperty != null && dataStreamProperty.CanWrite)
                {
                    var entryDataStream = new MemoryStream(dataBytes);
                    dataStreamProperty.SetValue(entry, entryDataStream);
                }
                else
                {
                    // Если рефлексия не работает, используем альтернативный подход
                    // Создаём новый MemoryStream и устанавливаем через рефлексию с использованием SetMethod
                    var setMethod = dataStreamProperty?.GetSetMethod(nonPublic: true);
                    if (setMethod != null)
                    {
                        var entryDataStream = new MemoryStream(dataBytes);
                        setMethod.Invoke(entry, new object[] { entryDataStream });
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unable to set DataStream for TAR entry '{entryName}'. Property is read-only or not accessible.");
                    }
                }
                
                // Записываем entry в TAR
                await tarWriter.WriteEntryAsync(entry, CancellationToken.None);
            }

            // 1. Добавляем document.md с замененными ссылками на изображения
            var contentBytes = Encoding.UTF8.GetBytes(modifiedContent);
            using (var contentMemoryStream = new MemoryStream(contentBytes))
            {
                await WriteFileToTarAsync("document.md", contentMemoryStream);
            }

            // 2. Добавляем metadata.json
            var metadataJson = document.Metadata ?? "{}";
            var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);
            using (var metadataMemoryStream = new MemoryStream(metadataBytes))
            {
                await WriteFileToTarAsync("metadata.json", metadataMemoryStream);
            }

            // 3. Добавляем overrides.json
            try
            {
                var overridesPath = document.MdMinioPath.Replace("content.md", "overrides.json");
                using var overridesStream = await _minioService.DownloadFileAsync(bucket, overridesPath);
                await WriteFileToTarAsync("overrides.json", overridesStream);
            }
            catch (FileNotFoundException)
            {
                // Если overrides.json не существует, создаем пустой
                var emptyOverrides = new StyleOverrides();
                var emptyOverridesJson = JsonSerializer.Serialize(emptyOverrides);
                var emptyOverridesBytes = Encoding.UTF8.GetBytes(emptyOverridesJson);
                using (var emptyOverridesMemoryStream = new MemoryStream(emptyOverridesBytes))
                {
                    await WriteFileToTarAsync("overrides.json", emptyOverridesMemoryStream);
                }
            }

            if (options.IncludeDocument)
            {
                foreach (var kvp in imageMap)
                {
                    var fileName = kvp.Value;
                    var objectPath = $"documents/{documentId}/assets/{fileName}";

                    try
                    {
                        using var imageStream = await _minioService.DownloadFileAsync(bucket, objectPath);
                        var assetPath = $"assets/{fileName}";
                        await WriteFileToTarAsync(assetPath, imageStream);
                    }
                    catch (FileNotFoundException ex)
                    {
                        _logger.LogWarning(ex, "Image not found in MinIO: {ObjectPath}", objectPath);
                    }
                }
            }

            if (options.IncludeStyleProfile)
            {
                var profileIdForExport = options.ExportStyleProfileId ?? document.ProfileId;
                if (profileIdForExport.HasValue)
                {
                    try
                    {
                        var profile = await _profileService.GetProfileWithDataAsync(profileIdForExport.Value, userId);
                        if (profile != null && profile.Data != null)
                        {
                            var profileJson = JsonSerializer.Serialize(profile.Data, new JsonSerializerOptions 
                            { 
                                WriteIndented = true 
                            });
                            var profileBytes = Encoding.UTF8.GetBytes(profileJson);
                            using (var profileMemoryStream = new MemoryStream(profileBytes))
                            {
                                await WriteFileToTarAsync("profile.json", profileMemoryStream);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to export profile {ProfileId}", profileIdForExport.Value);
                    }
                }
            }

            if (options.IncludeTitlePage)
            {
                var titlePageIdForExport = options.ExportTitlePageId ?? document.TitlePageId;
                if (titlePageIdForExport.HasValue)
                {
                    try
                    {
                        var titlePage = await _titlePageService.GetTitlePageWithDataAsync(titlePageIdForExport.Value, userId);
                        if (titlePage != null && titlePage.Data != null)
                        {
                            var titlePageJson = JsonSerializer.Serialize(titlePage.Data, new JsonSerializerOptions 
                            { 
                                WriteIndented = true 
                            });
                            var titlePageBytes = Encoding.UTF8.GetBytes(titlePageJson);
                            using (var titlePageMemoryStream = new MemoryStream(titlePageBytes))
                            {
                                await WriteFileToTarAsync("titlepage.json", titlePageMemoryStream);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to export title page {TitlePageId}", titlePageIdForExport.Value);
                    }
                }
            }
        }

        tarStream.Position = 0;
        return tarStream;
    }

    private string ExtractFileNameFromUrl(string url, Guid documentId)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // Если URL содержит /asset/ - извлечь часть после /asset/
        if (url.Contains("/asset/"))
        {
            var index = url.LastIndexOf("/asset/");
            var fileName = url.Substring(index + "/asset/".Length);
            // Убрать query параметры если есть
            var queryIndex = fileName.IndexOf('?');
            if (queryIndex >= 0)
            {
                fileName = fileName.Substring(0, queryIndex);
            }
            return fileName;
        }

        // Если URL содержит полный путь к API
        if (url.Contains($"/api/upload/document/{documentId}/asset/"))
        {
            var index = url.LastIndexOf($"/asset/");
            var fileName = url.Substring(index + "/asset/".Length);
            var queryIndex = fileName.IndexOf('?');
            if (queryIndex >= 0)
            {
                fileName = fileName.Substring(0, queryIndex);
            }
            return fileName;
        }

        // Иначе попытаться извлечь имя файла из URL напрямую
        try
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            var fileName = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }
        }
        catch
        {
            // Игнорируем ошибки парсинга URI
        }

        // Если ничего не помогло, вернуть пустую строку
        return string.Empty;
    }

    public async Task<DocumentDTO> ImportDocumentAsync(Guid userId, Stream ddocStream, string? documentName = null)
    {
        string? documentContent = null;
        DocumentMetadataDTO? metadata = null;
        StyleOverrides? overrides = null;
        ProfileData? profileData = null;
        TitlePageData? titlePageData = null;
        var assets = new Dictionary<string, byte[]>();

        // Распаковка TAR архива
        ddocStream.Position = 0;
        using var tarReader = new TarReader(ddocStream, leaveOpen: false);

        TarEntry? entry;
        while ((entry = await tarReader.GetNextEntryAsync()) != null)
        {
            if (entry.EntryType != TarEntryType.RegularFile)
                continue;

            var entryName = entry.Name;
            using var entryStream = entry.DataStream ?? new MemoryStream();
            using var memoryStream = new MemoryStream();
            await entryStream.CopyToAsync(memoryStream);
            var fileData = memoryStream.ToArray();

            // Обработка файлов по их именам
            if (entryName == "document.md")
            {
                documentContent = Encoding.UTF8.GetString(fileData);
            }
            else if (entryName == "metadata.json")
            {
                try
                {
                    var metadataJson = Encoding.UTF8.GetString(fileData);
                    metadata = JsonSerializer.Deserialize<DocumentMetadataDTO>(metadataJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse metadata.json during import");
                }
            }
            else if (entryName == "overrides.json")
            {
                try
                {
                    var overridesJsonContent = Encoding.UTF8.GetString(fileData);
                    overrides = JsonSerializer.Deserialize<StyleOverrides>(overridesJsonContent) ?? new StyleOverrides();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse overrides.json during import");
                    overrides = new StyleOverrides();
                }
            }
            else if (entryName == "profile.json")
            {
                try
                {
                    var profileJson = Encoding.UTF8.GetString(fileData);
                    profileData = JsonSerializer.Deserialize<ProfileData>(profileJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse profile.json during import");
                }
            }
            else if (entryName == "titlepage.json")
            {
                try
                {
                    var titlePageJson = Encoding.UTF8.GetString(fileData);
                    titlePageData = JsonSerializer.Deserialize<TitlePageData>(titlePageJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse titlepage.json during import");
                }
            }
            else if (entryName.StartsWith("assets/"))
            {
                var fileName = entryName.Substring("assets/".Length);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    assets[fileName] = fileData;
                }
            }
        }

        // Проверка обязательного файла
        if (string.IsNullOrWhiteSpace(documentContent))
        {
            throw new InvalidOperationException("document.md is required in .ddoc file");
        }

        // Определение имени документа
        var finalDocumentName = documentName;
        if (string.IsNullOrWhiteSpace(finalDocumentName))
        {
            finalDocumentName = metadata?.Title ?? "Импортированный документ";
        }

        // Обработка профиля
        Guid? profileId = null;
        if (profileData != null)
        {
            try
            {
                // Ищем существующий профиль по имени документа
                var existingProfiles = await _profileService.GetProfilesAsync(userId);
                var existingProfile = existingProfiles.FirstOrDefault(p => p.Name.Equals(finalDocumentName, StringComparison.OrdinalIgnoreCase));

                if (existingProfile != null)
                {
                    profileId = existingProfile.Id;
                    _logger.LogInformation("Using existing profile {ProfileId} for imported document", profileId);
                }
                else
                {
                    // Создаем новый профиль
                    var profileName = $"{finalDocumentName} - Профиль";
                    var createProfileDto = new CreateProfileDTO
                    {
                        Name = profileName,
                        Description = "Импортирован из .ddoc файла",
                        Data = profileData
                    };
                    var newProfile = await _profileService.CreateProfileAsync(userId, createProfileDto);
                    profileId = newProfile.Id;
                    _logger.LogInformation("Created new profile {ProfileId} for imported document", profileId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process profile during import, continuing without profile");
            }
        }

        // Обработка титульной страницы
        Guid? titlePageId = null;
        if (titlePageData != null)
        {
            try
            {
                // Ищем существующую титульную страницу по имени документа
                var existingTitlePages = await _titlePageService.GetTitlePagesAsync(userId);
                var existingTitlePage = existingTitlePages.FirstOrDefault(tp => tp.Name.Equals(finalDocumentName, StringComparison.OrdinalIgnoreCase));

                if (existingTitlePage != null)
                {
                    titlePageId = existingTitlePage.Id;
                    _logger.LogInformation("Using existing title page {TitlePageId} for imported document", titlePageId);
                }
                else
                {
                    // Создаем новую титульную страницу
                    var titlePageName = $"{finalDocumentName} - Титульная страница";
                    var createTitlePageDto = new CreateTitlePageDTO
                    {
                        Name = titlePageName,
                        Description = "Импортирована из .ddoc файла",
                        Data = titlePageData
                    };
                    var newTitlePage = await _titlePageService.CreateTitlePageAsync(userId, createTitlePageDto);
                    titlePageId = newTitlePage.Id;
                    _logger.LogInformation("Created new title page {TitlePageId} for imported document", titlePageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process title page during import, continuing without title page");
            }
        }

        // Создание документа
        var document = new DocumentLink
        {
            CreatorId = userId,
            Name = finalDocumentName,
            Description = null,
            ProfileId = profileId,
            TitlePageId = titlePageId,
            IsArchived = false,
            MdMinioPath = $"documents/{Guid.NewGuid()}/content.md"
        };

        if (metadata != null)
        {
            document.Metadata = JsonSerializer.Serialize(metadata);
        }

        _context.DocumentLinks.Add(document);
        await _context.SaveChangesAsync();

        // Создаём бакет пользователя если его нет
        var bucket = GetUserBucket(userId);
        await _minioService.EnsureBucketExistsAsync(bucket);

        // Обновляем ссылки на изображения в контенте
        var modifiedContent = documentContent;
        foreach (var assetFileName in assets.Keys)
        {
            // Заменяем относительные пути assets/ на полные URL API
            var oldPath = $"assets/{assetFileName}";
            var newUrl = $"/api/upload/document/{document.Id}/asset/{assetFileName}";
            modifiedContent = modifiedContent.Replace($"]({oldPath})", $"]({newUrl})");
            modifiedContent = modifiedContent.Replace($"]({oldPath}?t=", $"]({newUrl}?t=");
        }

        // Сохраняем контент в MinIO
        var contentBytes = Encoding.UTF8.GetBytes(modifiedContent);
        using var contentStream = new MemoryStream(contentBytes);
        await _minioService.UploadFileAsync(bucket, document.MdMinioPath, contentStream, "text/markdown");

        // Сохраняем overrides.json
        var overridesPath = document.MdMinioPath.Replace("content.md", "overrides.json");
        var overridesToSave = overrides ?? new StyleOverrides();
        var overridesJson = JsonSerializer.Serialize(overridesToSave);
        var overridesBytes = Encoding.UTF8.GetBytes(overridesJson);
        using var overridesStream = new MemoryStream(overridesBytes);
        await _minioService.UploadFileAsync(bucket, overridesPath, overridesStream, "application/json");

        // Загружаем изображения в MinIO
        foreach (var kvp in assets)
        {
            var assetPath = $"documents/{document.Id}/assets/{kvp.Key}";
            using var assetStream = new MemoryStream(kvp.Value);
            // Определяем MIME тип по расширению
            var mimeType = GetMimeType(kvp.Key);
            await _minioService.UploadFileAsync(bucket, assetPath, assetStream, mimeType);
        }

        _logger.LogInformation("Imported document {DocumentId} for user {UserId}", document.Id, userId);

        // Перезагружаем документ с навигационными свойствами для маппинга
        var documentWithNav = await _context.DocumentLinks
            .Include(d => d.Profile)
            .Include(d => d.TitlePage)
            .FirstOrDefaultAsync(d => d.Id == document.Id);

        if (documentWithNav == null)
            throw new InvalidOperationException($"Document {document.Id} not found after import");

        return await MapToDTOAsync(documentWithNav);
    }

    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }

    private async Task ClearPendingDocumentChangesAsync(Guid documentId)
    {
        var pendingChanges = await _context.DocumentAiChanges
            .Where(c => c.DocumentId == documentId)
            .ToListAsync();

        if (pendingChanges.Count > 0)
            _context.DocumentAiChanges.RemoveRange(pendingChanges);
    }

    private static string ApplyAcceptedChangeGroup(string currentContent, IReadOnlyCollection<DocumentAiChange> groupChanges)
    {
        var normalizedContent = currentContent.Replace("\r\n", "\n");
        var lines = normalizedContent.Length == 0 ? new List<string>() : normalizedContent.Split('\n').ToList();

        var deletePart = groupChanges
            .Where(c => string.Equals(c.ChangeType, "delete", StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Order ?? 0)
            .ThenBy(c => c.CreatedAt)
            .ToList();

        var insertPart = groupChanges
            .Where(c => string.Equals(c.ChangeType, "insert", StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Order ?? 0)
            .ThenBy(c => c.CreatedAt)
            .ToList();

        if (deletePart.Count > 0)
        {
            var startLine = deletePart.Min(c => c.StartLine);
            var endLine = deletePart.Max(c => c.EndLine ?? c.StartLine);
            startLine = Math.Clamp(startLine, 1, Math.Max(lines.Count, 1));
            endLine = Math.Clamp(endLine, startLine, lines.Count);

            if (lines.Count > 0 && endLine >= startLine)
                lines.RemoveRange(startLine - 1, endLine - startLine + 1);

            if (insertPart.Count > 0)
            {
                var replacementLines = MergeGroupedContent(insertPart);
                var insertIndex = Math.Clamp(startLine - 1, 0, lines.Count);
                lines.InsertRange(insertIndex, replacementLines);
            }

            return string.Join("\n", lines);
        }

        if (insertPart.Count > 0)
        {
            var insertAfterLine = insertPart.Min(c => c.StartLine);
            var insertIndex = Math.Clamp(insertAfterLine, 0, lines.Count);
            lines.InsertRange(insertIndex, MergeGroupedContent(insertPart));
        }

        return string.Join("\n", lines);
    }

    private static List<string> MergeGroupedContent(IReadOnlyCollection<DocumentAiChange> changes)
    {
        var content = string.Join(
            "\n",
            changes
                .OrderBy(c => c.Order ?? 0)
                .ThenBy(c => c.CreatedAt)
                .Select(c => c.Content.Trim('\n'))
        ).Trim('\n');

        if (string.IsNullOrEmpty(content))
            return new List<string>();

        return content.Split('\n').ToList();
    }

    private void RebasePendingChangesAfterAccept(List<DocumentAiChange> remainingChanges, IReadOnlyCollection<DocumentAiChange> acceptedGroup)
    {
        if (remainingChanges.Count == 0 || acceptedGroup.Count == 0)
            return;

        var acceptedDelete = acceptedGroup
            .Where(c => string.Equals(c.ChangeType, "delete", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var acceptedInsert = acceptedGroup
            .Where(c => string.Equals(c.ChangeType, "insert", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (acceptedDelete.Count == 0 && acceptedInsert.Count > 0)
        {
            var anchorLine = acceptedInsert.Min(c => c.StartLine);
            var delta = CountLines(acceptedInsert);
            var conflicting = new List<DocumentAiChange>();

            foreach (var change in remainingChanges)
            {
                var changeStart = change.StartLine;
                var changeEnd = change.EndLine ?? change.StartLine;

                // Pending change whose range spans the insertion anchor is conflicting
                var overlaps = string.Equals(change.ChangeType, "insert", StringComparison.OrdinalIgnoreCase)
                    ? changeStart == anchorLine
                    : changeStart <= anchorLine && changeEnd > anchorLine;

                if (overlaps)
                {
                    conflicting.Add(change);
                    continue;
                }

                if (change.StartLine > anchorLine)
                    change.StartLine += delta;
                if (change.EndLine.HasValue && change.EndLine.Value > anchorLine)
                    change.EndLine += delta;
            }

            if (conflicting.Count > 0)
                _context.DocumentAiChanges.RemoveRange(conflicting);

            return;
        }

        if (acceptedDelete.Count == 0)
            return;

        var deleteStart = acceptedDelete.Min(c => c.StartLine);
        var deleteEnd = acceptedDelete.Max(c => c.EndLine ?? c.StartLine);
        var deltaAfterDelete = CountLines(acceptedInsert) - (deleteEnd - deleteStart + 1);
        var conflictingChanges = new List<DocumentAiChange>();

        foreach (var change in remainingChanges)
        {
            var changeStart = change.StartLine;
            var changeEnd = change.EndLine ?? change.StartLine;
            var overlapsDeletedRange = string.Equals(change.ChangeType, "insert", StringComparison.OrdinalIgnoreCase)
                ? changeStart >= deleteStart && changeStart <= deleteEnd
                : changeStart <= deleteEnd && changeEnd >= deleteStart;

            if (overlapsDeletedRange)
            {
                conflictingChanges.Add(change);
                continue;
            }

            if (changeStart > deleteEnd)
                change.StartLine += deltaAfterDelete;
            if (change.EndLine.HasValue && change.EndLine.Value > deleteEnd)
                change.EndLine += deltaAfterDelete;
        }

        if (conflictingChanges.Count > 0)
            _context.DocumentAiChanges.RemoveRange(conflictingChanges);
    }

    private static int CountLines(IEnumerable<DocumentAiChange> changes)
    {
        var content = string.Join(
            "\n",
            changes
                .OrderBy(c => c.Order ?? 0)
                .ThenBy(c => c.CreatedAt)
                .Select(c => c.Content.Trim('\n'))
        ).Trim('\n');

        if (string.IsNullOrEmpty(content))
            return 0;

        return content.Split('\n').Length;
    }
}
