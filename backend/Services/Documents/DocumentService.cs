using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using RusalProject.Models.DTOs.Documents;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Documents;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioClient _minioClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentService> _logger;

    // Internal class for deserializing meta.json
    private class DocumentMetaJson
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string? profileId { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }

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
    private string GetMetaPath(Guid documentId) => $"{GetDocumentPath(documentId)}/meta.json";
    private string GetOverridesPath(Guid documentId) => $"{GetDocumentPath(documentId)}/overrides.json";
    private string GetImagePath(Guid documentId, string imageId) => $"{GetDocumentPath(documentId)}/images/{imageId}";

    public async Task<List<DocumentMetaDTO>> GetAllDocumentsAsync(Guid userId)
    {
        // Get documents from DB (only id and updatedAt for sorting)
        var dbDocuments = await _context.Documents
            .Where(d => d.CreatorId == userId)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new { d.Id, d.UpdatedAt })
            .ToListAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        // Read meta.json for each document in parallel
        var metaTasks = dbDocuments.Select(async dbDoc =>
        {
            try
            {
                var metaJson = await ReadJsonAsync<DocumentMetaJson>(bucketName, GetMetaPath(dbDoc.Id));
                if (metaJson == null) return null;

                return new DocumentMetaDTO
                {
                    Id = Guid.Parse(metaJson.id),
                    Name = metaJson.name,
                    ProfileId = !string.IsNullOrEmpty(metaJson.profileId) ? Guid.Parse(metaJson.profileId) : null,
                    CreatedAt = metaJson.createdAt,
                    UpdatedAt = metaJson.updatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read meta.json for document {DocumentId}", dbDoc.Id);
                return null;
            }
        });

        var metas = await Task.WhenAll(metaTasks);
        return metas.Where(m => m != null).Cast<DocumentMetaDTO>().ToList();
    }

    public async Task<DocumentDTO?> GetDocumentByIdAsync(Guid id, Guid userId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null) return null;

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var metaJson = await ReadJsonAsync<DocumentMetaJson>(bucketName, GetMetaPath(id));
        if (metaJson == null) return null;

        var content = await ReadFileAsync(bucketName, GetContentPath(id)) ?? string.Empty;
        var overrides = await ReadJsonAsync<Dictionary<string, object>>(bucketName, GetOverridesPath(id)) ?? new Dictionary<string, object>();

        return new DocumentDTO
        {
            Id = Guid.Parse(metaJson.id),
            Name = metaJson.name,
            ProfileId = !string.IsNullOrEmpty(metaJson.profileId) ? Guid.Parse(metaJson.profileId) : null,
            Content = content,
            Overrides = overrides,
            CreatedAt = metaJson.createdAt,
            UpdatedAt = metaJson.updatedAt
        };
    }

    public async Task<DocumentDTO> CreateDocumentAsync(CreateDocumentDTO dto, Guid userId)
    {
        try
        {
            // #region agent log
            Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:118\",\"message\":\"CreateDocumentAsync started\",\"data\":{{\"userId\":\"{userId}\",\"dtoName\":\"{dto.Name}\",\"dtoProfileId\":\"{dto.ProfileId}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run2\",\"hypothesisId\":\"A\"}}");
            // #endregion

            var documentId = Guid.NewGuid();
            var now = DateTime.UtcNow;
        
        var document = new Document
        {
            Id = documentId,
            CreatorId = userId,
            UpdatedAt = now
        };

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:130\",\"message\":\"About to save document to DB\",\"data\":{{\"documentId\":\"{documentId}\",\"userId\":\"{userId}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\"}}");
        // #endregion

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:131\",\"message\":\"Document saved to DB successfully\",\"data\":{{\"documentId\":\"{documentId}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\"}}");
        // #endregion

        var bucketName = GetBucketName(userId);

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:133\",\"message\":\"About to ensure bucket exists\",\"data\":{{\"bucketName\":\"{bucketName}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}");
        // #endregion

        await EnsureBucketExistsAsync(bucketName);

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:134\",\"message\":\"Bucket ensured successfully\",\"data\":{{\"bucketName\":\"{bucketName}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}");
        // #endregion

        var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "DefaultContent.md");
        var defaultContent = string.Empty;

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:136\",\"message\":\"Checking default content file\",\"data\":{{\"configPath\":\"{configPath}\",\"fileExists\":\"{File.Exists(configPath)}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}");
        // #endregion

        if (File.Exists(configPath))
        {
            defaultContent = await File.ReadAllTextAsync(configPath);

            // #region agent log
            Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:140\",\"message\":\"Default content loaded\",\"data\":{{\"contentLength\":\"{defaultContent.Length}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}");
            // #endregion
        }

        var meta = new DocumentMetaJson
        {
            id = documentId.ToString(),
            name = dto.Name ?? "Новый документ",
            profileId = dto.ProfileId?.ToString(),
            createdAt = now,
            updatedAt = now
        };

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:152\",\"message\":\"About to write content file to MinIO\",\"data\":{{\"path\":\"{GetContentPath(documentId)}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}");
        // #endregion

        await WriteFileAsync(bucketName, GetContentPath(documentId), defaultContent);

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:152\",\"message\":\"Content file written successfully\",\"data\":{{\"path\":\"{GetContentPath(documentId)}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}");
        // #endregion

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:153\",\"message\":\"About to write meta.json to MinIO\",\"data\":{{\"path\":\"{GetMetaPath(documentId)}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}");
        // #endregion

        await WriteJsonAsync(bucketName, GetMetaPath(documentId), meta);

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:153\",\"message\":\"Meta.json written successfully\",\"data\":{{\"path\":\"{GetMetaPath(documentId)}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}");
        // #endregion

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:154\",\"message\":\"About to write overrides.json to MinIO\",\"data\":{{\"path\":\"{GetOverridesPath(documentId)}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}");
        // #endregion

        await WriteJsonAsync(bucketName, GetOverridesPath(documentId), new Dictionary<string, object>());

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:154\",\"message\":\"Overrides.json written successfully\",\"data\":{{\"path\":\"{GetOverridesPath(documentId)}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}");
        // #endregion

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:225\",\"message\":\"CreateDocumentAsync completed successfully\",\"data\":{{\"documentId\":\"{documentId}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\"}}");
        // #endregion

            return new DocumentDTO
            {
                Id = documentId,
                Name = meta.name,
                ProfileId = dto.ProfileId,
                Content = defaultContent,
                Overrides = new Dictionary<string, object>(),
                CreatedAt = now,
                UpdatedAt = now
            };
        }
        catch (Exception ex)
        {
            // #region agent log
            Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:242\",\"message\":\"CreateDocumentAsync failed with exception\",\"data\":{{\"userId\":\"{userId}\",\"error\":\"{ex.Message}\",\"stackTrace\":\"{ex.StackTrace}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"E\"}}");
            // #endregion

            throw;
        }
    }

    public async Task<DocumentDTO?> UpdateDocumentAsync(Guid id, UpdateDocumentDTO dto, Guid userId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null) return null;

        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        // Read existing meta
        var metaJson = await ReadJsonAsync<DocumentMetaJson>(bucketName, GetMetaPath(id));
        if (metaJson == null) return null;

        // Update meta if needed
        if (dto.Name != null) metaJson.name = dto.Name;
        if (dto.ProfileId != null) metaJson.profileId = dto.ProfileId.ToString();
        metaJson.updatedAt = document.UpdatedAt;

        if (dto.Content != null)
        {
            await WriteFileAsync(bucketName, GetContentPath(id), dto.Content);
        }

        if (dto.Overrides != null)
        {
            await WriteJsonAsync(bucketName, GetOverridesPath(id), dto.Overrides);
        }

        await WriteJsonAsync(bucketName, GetMetaPath(id), metaJson);

        var content = dto.Content ?? await ReadFileAsync(bucketName, GetContentPath(id)) ?? string.Empty;
        var overrides = dto.Overrides ?? await ReadJsonAsync<Dictionary<string, object>>(bucketName, GetOverridesPath(id)) ?? new Dictionary<string, object>();

        return new DocumentDTO
        {
            Id = id,
            Name = metaJson.name,
            ProfileId = !string.IsNullOrEmpty(metaJson.profileId) ? Guid.Parse(metaJson.profileId) : null,
            Content = content,
            Overrides = overrides,
            CreatedAt = metaJson.createdAt,
            UpdatedAt = metaJson.updatedAt
        };
    }

    public async Task<bool> DeleteDocumentAsync(Guid id, Guid userId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null) return false;

        var bucketName = GetBucketName(userId);

        try
        {
            var filesToDelete = new[]
            {
                GetContentPath(id),
                GetMetaPath(id),
                GetOverridesPath(id)
            };

            foreach (var filePath in filesToDelete)
            {
                try
                {
                    var removeArgs = new RemoveObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(filePath);
                    await _minioClient.RemoveObjectAsync(removeArgs);
                }
                catch (ObjectNotFoundException)
                {
                    // File doesn't exist, skip
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete files from MinIO for document {DocumentId}", id);
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<string> UploadImageAsync(Guid documentId, Stream fileStream, string filename, Guid userId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null) throw new InvalidOperationException("Document not found");

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
        var document = await _context.Documents
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
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == userId);

        if (document == null) return false;

        var bucketName = GetBucketName(userId);
        var imagePath = GetImagePath(documentId, imageId);

        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(imagePath);
            await _minioClient.RemoveObjectAsync(removeArgs);
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
        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:414\",\"message\":\"EnsureBucketExistsAsync checking bucket\",\"data\":{{\"bucketName\":\"{bucketName}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}");
        // #endregion

        var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:414\",\"message\":\"Bucket exists check result\",\"data\":{{\"bucketName\":\"{bucketName}\",\"exists\":\"{exists}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}");
        // #endregion

        if (!exists)
        {
            // #region agent log
            Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:419\",\"message\":\"Creating bucket\",\"data\":{{\"bucketName\":\"{bucketName}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}");
            // #endregion

            var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
            await _minioClient.MakeBucketAsync(makeBucketArgs);

            // #region agent log
            Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:421\",\"message\":\"Bucket created successfully\",\"data\":{{\"bucketName\":\"{bucketName}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}");
            // #endregion
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
        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:466\",\"message\":\"WriteFileAsync starting\",\"data\":{{\"bucketName\":\"{bucketName}\",\"objectName\":\"{objectName}\",\"contentLength\":\"{content.Length}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}");
        // #endregion

        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        stream.Position = 0;

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize((long)bytes.Length)
            .WithContentType("text/markdown");

        await _minioClient.PutObjectAsync(putArgs);

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:479\",\"message\":\"WriteFileAsync completed\",\"data\":{{\"bucketName\":\"{bucketName}\",\"objectName\":\"{objectName}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}");
        // #endregion
    }

    private async Task<T?> ReadJsonAsync<T>(string bucketName, string objectName)
    {
        var content = await ReadFileAsync(bucketName, objectName);
        if (content == null) return default;

        return JsonSerializer.Deserialize<T>(content);
    }

    private async Task WriteJsonAsync<T>(string bucketName, string objectName, T data)
    {
        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:500\",\"message\":\"WriteJsonAsync starting\",\"data\":{{\"bucketName\":\"{bucketName}\",\"objectName\":\"{objectName}\",\"type\":\"{typeof(T).Name}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}");
        // #endregion

        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        using var stream = new MemoryStream(bytes);
        stream.Position = 0;

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize((long)bytes.Length)
            .WithContentType("application/json");

        await _minioClient.PutObjectAsync(putArgs);

        // #region agent log
        Console.WriteLine($"{{\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"location\":\"DocumentService.cs:514\",\"message\":\"WriteJsonAsync completed\",\"data\":{{\"bucketName\":\"{bucketName}\",\"objectName\":\"{objectName}\"}},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\"}}");
        // #endregion
    }
}
