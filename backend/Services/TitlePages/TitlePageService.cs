using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using RusalProject.Models.DTOs.TitlePages;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;

namespace RusalProject.Services.TitlePages;

public class TitlePageService : ITitlePageService
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioClient _minioClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TitlePageService> _logger;

    // Internal class for deserializing meta.json
    private class TitlePageMetaJson
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }

    public TitlePageService(
        ApplicationDbContext context,
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<TitlePageService> logger)
    {
        _context = context;
        _minioClient = minioClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetBucketName(Guid userId) => $"user-{userId}";
    private string GetTitlePagePath(Guid titlePageId) => $"TitlePage/{titlePageId}";
    private string GetMetaPath(Guid titlePageId) => $"{GetTitlePagePath(titlePageId)}/meta.json";
    private string GetElementsPath(Guid titlePageId) => $"{GetTitlePagePath(titlePageId)}/elements.json";
    private string GetVariablesPath(Guid titlePageId) => $"{GetTitlePagePath(titlePageId)}/variables.json";

    public async Task<List<TitlePageMetaDTO>> GetAllTitlePagesAsync(Guid userId)
    {
        // Get title pages from DB (only id and updatedAt for sorting)
        var dbTitlePages = await _context.TitlePages
            .Where(tp => tp.CreatorId == userId)
            .OrderByDescending(tp => tp.UpdatedAt)
            .Select(tp => new { tp.Id, tp.UpdatedAt })
            .ToListAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        // Read meta.json for each title page in parallel
        var metaTasks = dbTitlePages.Select(async dbTitlePage =>
        {
            try
            {
                var metaJson = await ReadJsonAsync<TitlePageMetaJson>(bucketName, GetMetaPath(dbTitlePage.Id));
                if (metaJson == null) return null;

                return new TitlePageMetaDTO
                {
                    Id = Guid.Parse(metaJson.id),
                    Name = metaJson.name,
                    CreatedAt = metaJson.createdAt,
                    UpdatedAt = metaJson.updatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read meta.json for title page {TitlePageId}", dbTitlePage.Id);
                return null;
            }
        });

        var metas = await Task.WhenAll(metaTasks);
        return metas.Where(m => m != null).Cast<TitlePageMetaDTO>().ToList();
    }

    public async Task<TitlePageDTO?> GetTitlePageByIdAsync(Guid id, Guid userId)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(tp => tp.Id == id && tp.CreatorId == userId);

        if (titlePage == null) return null;

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var metaJson = await ReadJsonAsync<TitlePageMetaJson>(bucketName, GetMetaPath(id));
        if (metaJson == null) return null;

        var elements = await ReadJsonAsync<List<TitlePageElementDTO>>(bucketName, GetElementsPath(id)) ?? new List<TitlePageElementDTO>();
        var variables = await ReadJsonAsync<Dictionary<string, string>>(bucketName, GetVariablesPath(id)) ?? new Dictionary<string, string>();

        return new TitlePageDTO
        {
            Id = Guid.Parse(metaJson.id),
            Name = metaJson.name,
            Elements = elements,
            Variables = variables,
            CreatedAt = metaJson.createdAt,
            UpdatedAt = metaJson.updatedAt
        };
    }

    public async Task<TitlePageDTO> CreateTitlePageAsync(CreateTitlePageDTO dto, Guid userId)
    {
        var titlePageId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var titlePage = new TitlePage
        {
            Id = titlePageId,
            CreatorId = userId,
            UpdatedAt = now
        };

        _context.TitlePages.Add(titlePage);
        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var meta = new TitlePageMetaJson
        {
            id = titlePageId.ToString(),
            name = dto.Name ?? "Новый титульный лист",
            createdAt = now,
            updatedAt = now
        };

        await WriteJsonAsync(bucketName, GetMetaPath(titlePageId), meta);
        await WriteJsonAsync(bucketName, GetElementsPath(titlePageId), new List<TitlePageElementDTO>());
        await WriteJsonAsync(bucketName, GetVariablesPath(titlePageId), new Dictionary<string, string>());

        return new TitlePageDTO
        {
            Id = titlePageId,
            Name = meta.name,
            Elements = new List<TitlePageElementDTO>(),
            Variables = new Dictionary<string, string>(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public async Task<TitlePageDTO?> UpdateTitlePageAsync(Guid id, UpdateTitlePageDTO dto, Guid userId)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(tp => tp.Id == id && tp.CreatorId == userId);

        if (titlePage == null) return null;

        titlePage.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        // Read existing meta
        var metaJson = await ReadJsonAsync<TitlePageMetaJson>(bucketName, GetMetaPath(id));
        if (metaJson == null) return null;

        // Update meta if needed
        if (dto.Name != null) metaJson.name = dto.Name;
        metaJson.updatedAt = titlePage.UpdatedAt;

        if (dto.Elements != null)
        {
            await WriteJsonAsync(bucketName, GetElementsPath(id), dto.Elements);
        }

        if (dto.Variables != null)
        {
            await WriteJsonAsync(bucketName, GetVariablesPath(id), dto.Variables);
        }

        await WriteJsonAsync(bucketName, GetMetaPath(id), metaJson);

        var elements = dto.Elements ?? await ReadJsonAsync<List<TitlePageElementDTO>>(bucketName, GetElementsPath(id)) ?? new List<TitlePageElementDTO>();
        var variables = dto.Variables ?? await ReadJsonAsync<Dictionary<string, string>>(bucketName, GetVariablesPath(id)) ?? new Dictionary<string, string>();

        return new TitlePageDTO
        {
            Id = id,
            Name = metaJson.name,
            Elements = elements,
            Variables = variables,
            CreatedAt = metaJson.createdAt,
            UpdatedAt = metaJson.updatedAt
        };
    }

    public async Task<bool> DeleteTitlePageAsync(Guid id, Guid userId)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(tp => tp.Id == id && tp.CreatorId == userId);

        if (titlePage == null) return false;

        var bucketName = GetBucketName(userId);

        try
        {
            var filesToDelete = new[]
            {
                GetMetaPath(id),
                GetElementsPath(id),
                GetVariablesPath(id)
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
            _logger.LogWarning(ex, "Failed to delete files from MinIO for title page {TitlePageId}", id);
        }

        _context.TitlePages.Remove(titlePage);
        await _context.SaveChangesAsync();

        return true;
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
        using var stream = new MemoryStream(bytes);
        stream.Position = 0;

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize((long)bytes.Length)
            .WithContentType("application/json");

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
        using var stream = new MemoryStream(bytes);
        stream.Position = 0;

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize((long)bytes.Length)
            .WithContentType("application/json");

        await _minioClient.PutObjectAsync(putArgs);
    }
}
