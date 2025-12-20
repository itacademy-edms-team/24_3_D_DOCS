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
        var documents = await _context.Documents
            .Where(d => d.CreatorId == userId)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new DocumentMetaDTO
            {
                Id = d.Id,
                Name = d.Name,
                ProfileId = d.ProfileId,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();

        return documents;
    }

    public async Task<DocumentDTO?> GetDocumentByIdAsync(Guid id, Guid userId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null) return null;

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var content = await ReadFileAsync(bucketName, GetContentPath(id)) ?? string.Empty;
        var overrides = await ReadJsonAsync<Dictionary<string, object>>(bucketName, GetOverridesPath(id)) ?? new Dictionary<string, object>();

        return new DocumentDTO
        {
            Id = document.Id,
            Name = document.Name,
            ProfileId = document.ProfileId,
            Content = content,
            Overrides = overrides,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }

    public async Task<DocumentDTO> CreateDocumentAsync(CreateDocumentDTO dto, Guid userId)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            CreatorId = userId,
            Name = dto.Name,
            ProfileId = dto.ProfileId
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "DefaultContent.md");
        var defaultContent = await File.ReadAllTextAsync(configPath);
        var meta = new
        {
            id = document.Id.ToString(),
            name = document.Name,
            profileId = document.ProfileId?.ToString(),
            createdAt = document.CreatedAt,
            updatedAt = document.UpdatedAt
        };

        await WriteFileAsync(bucketName, GetContentPath(document.Id), defaultContent);
        await WriteJsonAsync(bucketName, GetMetaPath(document.Id), meta);
        await WriteJsonAsync(bucketName, GetOverridesPath(document.Id), new Dictionary<string, object>());

        return new DocumentDTO
        {
            Id = document.Id,
            Name = document.Name,
            ProfileId = document.ProfileId,
            Content = defaultContent,
            Overrides = new Dictionary<string, object>(),
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }

    public async Task<DocumentDTO?> UpdateDocumentAsync(Guid id, UpdateDocumentDTO dto, Guid userId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.CreatorId == userId);

        if (document == null) return null;

        if (dto.Name != null) document.Name = dto.Name;
        if (dto.ProfileId != null) document.ProfileId = dto.ProfileId;

        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        if (dto.Content != null)
        {
            await WriteFileAsync(bucketName, GetContentPath(id), dto.Content);
        }

        if (dto.Overrides != null)
        {
            await WriteJsonAsync(bucketName, GetOverridesPath(id), dto.Overrides);
        }

        var meta = new
        {
            id = document.Id.ToString(),
            name = document.Name,
            profileId = document.ProfileId?.ToString(),
            createdAt = document.CreatedAt,
            updatedAt = document.UpdatedAt
        };
        await WriteJsonAsync(bucketName, GetMetaPath(id), meta);

        var content = dto.Content ?? await ReadFileAsync(bucketName, GetContentPath(id)) ?? string.Empty;
        var overrides = dto.Overrides ?? await ReadJsonAsync<Dictionary<string, object>>(bucketName, GetOverridesPath(id)) ?? new Dictionary<string, object>();

        return new DocumentDTO
        {
            Id = document.Id,
            Name = document.Name,
            ProfileId = document.ProfileId,
            Content = content,
            Overrides = overrides,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
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
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(bytes.Length)
            .WithContentType("text/markdown");

        await _minioClient.PutObjectAsync(putArgs);
    }

    private async Task<T?> ReadJsonAsync<T>(string bucketName, string objectName)
    {
        var content = await ReadFileAsync(bucketName, objectName);
        if (content == null) return default;

        return JsonSerializer.Deserialize<T>(content);
    }

    private async Task WriteJsonAsync<T>(string bucketName, string objectName, T data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(bytes.Length)
            .WithContentType("application/json");

        await _minioClient.PutObjectAsync(putArgs);
    }
}
