using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using RusalProject.Models.DTOs.Document;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Documents;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioClient _minioClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentService> _logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DocumentService(
        ApplicationDbContext context,
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _minioClient = minioClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetBucketName(Guid userId) => $"user-{userId}";
    private string GetDocumentPath(Guid documentId) => $"Document/{documentId}";
    private string GetContentPath(Guid documentId) => $"{GetDocumentPath(documentId)}/content.md";
    private string GetOverridesPath(Guid documentId) => $"{GetDocumentPath(documentId)}/overrides.json";
    private string GetImagePath(Guid documentId, string imageId) => $"{GetDocumentPath(documentId)}/images/{imageId}";

    public async Task<List<DocumentDTO>> GetDocumentsAsync(Guid userId, string? status = null, string? search = null)
    {
        var query = _context.DocumentLinks
            .Where(d => d.CreatorId == userId && d.DeletedAt == null);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(d => d.Status == status);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d => d.Name.Contains(search) || (d.Description != null && d.Description.Contains(search)));
        }

        var documents = await query
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new DocumentDTO
            {
                Id = d.Id,
                CreatorId = d.CreatorId,
                Name = d.Name,
                Description = d.Description,
                ProfileId = d.ProfileId,
                TitlePageId = d.TitlePageId,
                Status = d.Status,
                IsArchived = d.IsArchived,
                DeletedAt = d.DeletedAt,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                HasPdf = !string.IsNullOrEmpty(d.PdfMinioPath)
            })
            .ToListAsync();

        return documents;
    }

    public async Task<DocumentWithContentDTO?> GetDocumentWithContentAsync(Guid id, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null) return null;

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var content = await ReadFileAsync(bucketName, GetContentPath(id)) ?? string.Empty;
        var overrides = await ReadJsonAsync<Dictionary<string, object>>(bucketName, GetOverridesPath(id)) ?? new Dictionary<string, object>();

        Dictionary<string, string>? variables = null;
        if (!string.IsNullOrEmpty(document.Variables))
        {
            variables = JsonSerializer.Deserialize<Dictionary<string, string>>(document.Variables, s_jsonOptions);
        }

        return new DocumentWithContentDTO
        {
            Id = document.Id,
            CreatorId = document.CreatorId,
            Name = document.Name,
            Description = document.Description,
            ProfileId = document.ProfileId,
            TitlePageId = document.TitlePageId,
            Variables = variables,
            Status = document.Status,
            IsArchived = document.IsArchived,
            DeletedAt = document.DeletedAt,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            HasPdf = !string.IsNullOrEmpty(document.PdfMinioPath),
            Content = content,
            StyleOverrides = overrides
        };
    }

    public async Task<DocumentDTO?> GetDocumentByIdAsync(Guid id, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null) return null;

        Dictionary<string, string>? variables = null;
        if (!string.IsNullOrEmpty(document.Variables))
        {
            variables = JsonSerializer.Deserialize<Dictionary<string, string>>(document.Variables, s_jsonOptions);
        }

        return new DocumentDTO
        {
            Id = document.Id,
            CreatorId = document.CreatorId,
            Name = document.Name,
            Description = document.Description,
            ProfileId = document.ProfileId,
            TitlePageId = document.TitlePageId,
            Variables = variables,
            Status = document.Status,
            IsArchived = document.IsArchived,
            DeletedAt = document.DeletedAt,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            HasPdf = !string.IsNullOrEmpty(document.PdfMinioPath)
        };
    }

    public async Task<DocumentDTO> CreateDocumentAsync(Guid userId, CreateDocumentDTO dto)
    {
        var documentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var document = new DocumentLink
        {
            Id = documentId,
            CreatorId = userId,
            Name = dto.Name,
            Description = dto.Description,
            MdMinioPath = GetContentPath(documentId),
            ProfileId = dto.ProfileId,
            TitlePageId = dto.TitlePageId,
            Variables = dto.Variables != null && dto.Variables.Count > 0 ? JsonSerializer.Serialize(dto.Variables, s_jsonOptions) : null,
            Status = "draft",
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.DocumentLinks.Add(document);
        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        // Create initial content
        var initialContent = dto.InitialContent ?? string.Empty;
        await WriteFileAsync(bucketName, GetContentPath(documentId), initialContent);
        await WriteJsonAsync(bucketName, GetOverridesPath(documentId), new Dictionary<string, object>());

        return new DocumentDTO
        {
            Id = documentId,
            CreatorId = userId,
            Name = document.Name,
            Description = document.Description,
            ProfileId = document.ProfileId,
            TitlePageId = document.TitlePageId,
            Variables = dto.Variables,
            Status = document.Status,
            IsArchived = document.IsArchived,
            DeletedAt = document.DeletedAt,
            CreatedAt = now,
            UpdatedAt = now,
            HasPdf = false
        };
    }

    public async Task<DocumentDTO> UpdateDocumentAsync(Guid id, Guid userId, UpdateDocumentDTO dto)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        if (dto.Name != null) document.Name = dto.Name;
        if (dto.Description != null) document.Description = dto.Description;
        if (dto.ProfileId.HasValue) document.ProfileId = dto.ProfileId;
        if (dto.TitlePageId.HasValue) document.TitlePageId = dto.TitlePageId;
        if (dto.Variables != null) document.Variables = dto.Variables.Count > 0 ? JsonSerializer.Serialize(dto.Variables, s_jsonOptions) : null;

        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        Dictionary<string, string>? variables = null;
        if (!string.IsNullOrEmpty(document.Variables))
        {
            variables = JsonSerializer.Deserialize<Dictionary<string, string>>(document.Variables, s_jsonOptions);
        }

        return new DocumentDTO
        {
            Id = document.Id,
            CreatorId = document.CreatorId,
            Name = document.Name,
            Description = document.Description,
            ProfileId = document.ProfileId,
            TitlePageId = document.TitlePageId,
            Variables = variables,
            Status = document.Status,
            IsArchived = document.IsArchived,
            DeletedAt = document.DeletedAt,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            HasPdf = !string.IsNullOrEmpty(document.PdfMinioPath)
        };
    }

    public async Task UpdateDocumentContentAsync(Guid id, Guid userId, string content)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);
        await WriteFileAsync(bucketName, GetContentPath(id), content);

        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDocumentOverridesAsync(Guid id, Guid userId, Dictionary<string, object> overrides)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);
        await WriteJsonAsync(bucketName, GetOverridesPath(id), overrides);

        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDocumentVariablesAsync(Guid id, Guid userId, Dictionary<string, string> variables)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        document.Variables = variables.Count > 0 ? JsonSerializer.Serialize(variables, s_jsonOptions) : null;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDocumentAsync(Guid id, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        // Soft delete
        document.DeletedAt = DateTime.UtcNow;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task RestoreDocumentAsync(Guid id, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        document.DeletedAt = null;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDocumentPermanentlyAsync(Guid id, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        var bucketName = GetBucketName(userId);

        // Delete MinIO files
        try
        {
            await DeleteMinioObjectAsync(bucketName, GetContentPath(id));
            await DeleteMinioObjectAsync(bucketName, GetOverridesPath(id));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete MinIO files for document {DocumentId}", id);
        }

        _context.DocumentLinks.Remove(document);
        await _context.SaveChangesAsync();
    }

    public async Task ArchiveDocumentAsync(Guid id, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        document.IsArchived = true;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UnarchiveDocumentAsync(Guid id, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        document.IsArchived = false;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePdfPathAsync(Guid id, Guid userId, string? pdfPath)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        document.PdfMinioPath = pdfPath;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<byte[]> ExportDocumentAsync(Guid id, Guid userId)
    {
        var document = await GetDocumentWithContentAsync(id, userId);
        if (document == null)
        {
            throw new FileNotFoundException($"Document {id} not found");
        }

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Add content.md
            var contentEntry = archive.CreateEntry("content.md");
            using (var writer = new StreamWriter(contentEntry.Open()))
            {
                await writer.WriteAsync(document.Content ?? string.Empty);
            }

            // Add metadata.json (includes variables)
            var metadataEntry = archive.CreateEntry("metadata.json");
            using (var writer = new StreamWriter(metadataEntry.Open()))
            {
                var exportData = new
                {
                    document.Name,
                    document.Description,
                    document.ProfileId,
                    document.TitlePageId,
                    document.Variables,
                    document.StyleOverrides
                };
                await writer.WriteAsync(JsonSerializer.Serialize(exportData, s_jsonOptions));
            }
        }

        return memoryStream.ToArray();
    }

    public async Task<DocumentDTO> ImportDocumentAsync(Guid userId, Stream fileStream, string filename)
    {
        // Copy to MemoryStream so we can detect format and re-read
        using var memStream = new MemoryStream();
        await fileStream.CopyToAsync(memStream);
        memStream.Position = 0;

        var (content, name, description, profileId, titlePageId, variables, overrides) = await ReadDdocArchiveAsync(memStream, filename);

        // Validate profileId and titlePageId belong to current user (imported file may reference another user's resources)
        if (profileId.HasValue)
        {
            var profileOwned = await _context.Profiles.AnyAsync(p => p.Id == profileId.Value && p.CreatorId == userId);
            if (!profileOwned) profileId = null;
        }
        if (titlePageId.HasValue)
        {
            var titlePageOwned = await _context.TitlePages.AnyAsync(t => t.Id == titlePageId.Value && t.CreatorId == userId);
            if (!titlePageOwned) titlePageId = null;
        }

        var dto = new CreateDocumentDTO
        {
            Name = name ?? "Imported Document",
            Description = description,
            ProfileId = profileId,
            TitlePageId = titlePageId,
            Variables = variables,
            InitialContent = content ?? string.Empty
        };

        var document = await CreateDocumentAsync(userId, dto);

        if (overrides != null && overrides.Count > 0)
        {
            await UpdateDocumentOverridesAsync(document.Id, userId, overrides);
        }

        return document;
    }

    /// <summary>
    /// Reads .ddoc archive (ZIP or TAR format). Supports both content.md and document.md for content.
    /// </summary>
    private async Task<(string? content, string? name, string? description, Guid? profileId, Guid? titlePageId, Dictionary<string, string>? variables, Dictionary<string, object>? overrides)> ReadDdocArchiveAsync(Stream stream, string filename)
    {
        string? content = null;
        string? name = Path.GetFileNameWithoutExtension(filename);
        string? description = null;
        Guid? profileId = null;
        Guid? titlePageId = null;
        Dictionary<string, string>? variables = null;
        Dictionary<string, object>? overrides = null;

        var header = new byte[2];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, 2));
        stream.Position = 0;

        if (bytesRead >= 2 && header[0] == 0x50 && header[1] == 0x4B) // "PK" - ZIP
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            content = ReadEntry(archive, "content.md") ?? ReadEntry(archive, "document.md");
            var metadataJson = ReadEntry(archive, "metadata.json");
            var overridesJson = ReadEntry(archive, "overrides.json");

            if (!string.IsNullOrEmpty(metadataJson))
                ParseMetadata(metadataJson, ref name, ref description, ref profileId, ref titlePageId, ref variables);
            if (!string.IsNullOrEmpty(overridesJson))
                overrides = JsonSerializer.Deserialize<Dictionary<string, object>>(overridesJson, s_jsonOptions);

            static string? ReadEntry(ZipArchive archive, string entryName)
            {
                var entry = archive.GetEntry(entryName);
                if (entry == null) return null;
                using var reader = new StreamReader(entry.Open());
                return reader.ReadToEnd();
            }
        }
        else // TAR (POSIX tar, ustar, etc.)
        {
            stream.Position = 0;
            using var reader = new TarReader(stream, leaveOpen: true);
            var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            TarEntry? entry;
            while ((entry = reader.GetNextEntry()) != null)
            {
                if (entry.DataStream == null || entry.EntryType is TarEntryType.Directory) continue;
                var key = entry.Name.TrimStart('.', '/').Replace('\\', '/');
                if (key.Contains('/')) key = key.Split('/').Last(); // flatten path
                using var sr = new StreamReader(entry.DataStream);
                entries[key] = await sr.ReadToEndAsync();
            }

            content = entries.GetValueOrDefault("content.md") ?? entries.GetValueOrDefault("document.md");
            var metaJson = entries.GetValueOrDefault("metadata.json");
            var overJson = entries.GetValueOrDefault("overrides.json");

            if (!string.IsNullOrEmpty(metaJson))
                ParseMetadata(metaJson, ref name, ref description, ref profileId, ref titlePageId, ref variables);
            if (!string.IsNullOrEmpty(overJson))
                overrides = JsonSerializer.Deserialize<Dictionary<string, object>>(overJson, s_jsonOptions);
        }

        return (content, name, description, profileId, titlePageId, variables, overrides ?? new Dictionary<string, object>());
    }

    private static void ParseMetadata(string json, ref string? name, ref string? description, ref Guid? profileId, ref Guid? titlePageId, ref Dictionary<string, string>? variables)
    {
        var importData = JsonSerializer.Deserialize<JsonElement>(json, s_jsonOptions);
        if (importData.TryGetProperty("name", out var nameElement)) name = nameElement.GetString();
        if (importData.TryGetProperty("description", out var descElement)) description = descElement.GetString();
        if (importData.TryGetProperty("profileId", out var profileElement) && profileElement.ValueKind != JsonValueKind.Null) profileId = profileElement.GetGuid();
        if (importData.TryGetProperty("titlePageId", out var titleElement) && titleElement.ValueKind != JsonValueKind.Null) titlePageId = titleElement.GetGuid();
        // New format: variables as Dictionary
        if (importData.TryGetProperty("variables", out var varsElement) && varsElement.ValueKind == JsonValueKind.Object)
        {
            variables = JsonSerializer.Deserialize<Dictionary<string, string>>(varsElement.GetRawText(), s_jsonOptions);
        }
        // Legacy format: metadata (DocumentMetadataDTO) -> convert to variables
        else if (importData.TryGetProperty("metadata", out var metaElement) && metaElement.ValueKind == JsonValueKind.Object)
        {
            var oldMeta = JsonSerializer.Deserialize<DocumentMetadataDTO>(metaElement.GetRawText(), s_jsonOptions);
            if (oldMeta != null)
            {
                variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(oldMeta.Title)) variables["Title"] = oldMeta.Title;
                if (!string.IsNullOrEmpty(oldMeta.Author)) variables["Author"] = oldMeta.Author;
                if (!string.IsNullOrEmpty(oldMeta.Group)) variables["Group"] = oldMeta.Group;
                if (!string.IsNullOrEmpty(oldMeta.Year)) variables["Year"] = oldMeta.Year;
                if (!string.IsNullOrEmpty(oldMeta.City)) variables["City"] = oldMeta.City;
                if (!string.IsNullOrEmpty(oldMeta.Supervisor)) variables["Supervisor"] = oldMeta.Supervisor;
                if (!string.IsNullOrEmpty(oldMeta.DocumentType)) variables["DocumentType"] = oldMeta.DocumentType;
                if (oldMeta.AdditionalFields != null)
                    foreach (var kv in oldMeta.AdditionalFields)
                        variables[kv.Key] = kv.Value ?? string.Empty;
            }
        }
    }

    public async Task<bool> DocumentExistsAsync(Guid documentId, Guid userId)
    {
        return await _context.DocumentLinks
            .AnyAsync(d => d.Id == documentId && d.CreatorId == userId);
    }

    public async Task<string> UploadImageAsync(Guid documentId, Stream fileStream, string filename, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null)
        {
            throw new FileNotFoundException($"Document {documentId} not found");
        }

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var imageId = Guid.NewGuid();
        var extension = Path.GetExtension(filename);
        var imagePath = GetImagePath(documentId, $"{imageId}{extension}");

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(imagePath)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType("image/" + extension.TrimStart('.'));

        await _minioClient.PutObjectAsync(putArgs);

        return $"/api/documents/{documentId}/images/{imageId}{extension}";
    }

    public async Task<Stream?> GetImageAsync(Guid documentId, string imageId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null) return null;

        var bucketName = GetBucketName(userId);
        var imagePath = GetImagePath(documentId, imageId);

        try
        {
            var memoryStream = new MemoryStream();
            var getArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(imagePath)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getArgs);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(Guid documentId, string imageId, Guid userId)
    {
        var document = await _context.DocumentLinks
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null) return false;

        var bucketName = GetBucketName(userId);
        var imagePath = GetImagePath(documentId, imageId);

        try
        {
            await DeleteMinioObjectAsync(bucketName, imagePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete image {ImageId} from document {DocumentId}", imageId, documentId);
            return false;
        }
    }

    private async Task EnsureBucketExistsAsync(string bucketName)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

        if (!exists)
        {
            var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
            await _minioClient.MakeBucketAsync(makeBucketArgs);
        }
    }

    private async Task<string?> ReadFileAsync(string bucketName, string objectName)
    {
        try
        {
            var memoryStream = new MemoryStream();
            var getArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getArgs);
            memoryStream.Position = 0;
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
    }

    private async Task WriteFileAsync(string bucketName, string objectName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        if (bytes.Length == 0)
        {
            bytes = new byte[] { (byte)'\n' };
        }
        var stream = new MemoryStream(bytes);
        stream.Position = 0;

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize((long)bytes.Length)
            .WithContentType("text/markdown");

        await _minioClient.PutObjectAsync(putArgs);
    }

    private async Task<T?> ReadJsonAsync<T>(string bucketName, string objectName)
    {
        var content = await ReadFileAsync(bucketName, objectName);
        if (content == null) return default;
        return JsonSerializer.Deserialize<T>(content, s_jsonOptions);
    }

    private async Task WriteJsonAsync<T>(string bucketName, string objectName, T data)
    {
        var json = JsonSerializer.Serialize(data, s_jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        if (bytes.Length == 0)
        {
            bytes = new byte[] { (byte)' ' };
        }
        var stream = new MemoryStream(bytes);
        stream.Position = 0;

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize((long)bytes.Length)
            .WithContentType("application/json");

        await _minioClient.PutObjectAsync(putArgs);
    }

    private async Task DeleteMinioObjectAsync(string bucketName, string objectName)
    {
        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            await _minioClient.RemoveObjectAsync(removeArgs);
        }
        catch (ObjectNotFoundException)
        {
            // Object doesn't exist, skip
        }
    }
}
