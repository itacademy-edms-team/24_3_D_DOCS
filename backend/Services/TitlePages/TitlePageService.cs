using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using RusalProject.Models.DTOs.TitlePage;
using RusalProject.Models.Entities;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;

namespace RusalProject.Services.TitlePages;

public class TitlePageService : ITitlePageService
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioClient _minioClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TitlePageService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

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
    private string GetTitlePagePath(Guid titlePageId) => $"TitlePage/{titlePageId}.json";

    public async Task<List<TitlePageDTO>> GetTitlePagesAsync(Guid userId)
    {
        var dbTitlePages = await _context.TitlePages
            .Where(tp => tp.CreatorId == userId)
            .OrderByDescending(tp => tp.UpdatedAt)
            .Select(tp => new { tp.Id, tp.CreatorId, tp.UpdatedAt })
            .ToListAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var titlePageTasks = dbTitlePages.Select(async dbTitlePage =>
        {
            try
            {
                var storageData = await ReadJsonAsync<TitlePageStorageDTO>(bucketName, GetTitlePagePath(dbTitlePage.Id));
                if (storageData == null) return null;

                return new TitlePageDTO
                {
                    Id = dbTitlePage.Id,
                    CreatorId = dbTitlePage.CreatorId,
                    Name = storageData.Name ?? "Untitled",
                    Description = storageData.Description,
                    CreatedAt = storageData.CreatedAt,
                    UpdatedAt = dbTitlePage.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read title page JSON for {TitlePageId}", dbTitlePage.Id);
                return null;
            }
        });

        var titlePages = (await Task.WhenAll(titlePageTasks))
            .Where(tp => tp != null)
            .Cast<TitlePageDTO>()
            .ToList();

        return titlePages;
    }

    public async Task<TitlePageWithDataDTO?> GetTitlePageWithDataAsync(Guid id, Guid userId)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(tp => tp.Id == id && tp.CreatorId == userId);

        if (titlePage == null) return null;

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var storageData = await ReadJsonAsync<TitlePageStorageDTO>(bucketName, GetTitlePagePath(id));
        if (storageData == null) return null;

        return new TitlePageWithDataDTO
        {
            Id = id,
            CreatorId = titlePage.CreatorId,
            Name = storageData.Name ?? "Untitled",
            Description = storageData.Description,
            CreatedAt = storageData.CreatedAt,
            UpdatedAt = titlePage.UpdatedAt,
            Data = storageData.Data ?? new TitlePageData()
        };
    }

    public async Task<TitlePageDTO?> GetTitlePageByIdAsync(Guid id, Guid userId)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(tp => tp.Id == id && tp.CreatorId == userId);

        if (titlePage == null) return null;

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var storageData = await ReadJsonAsync<TitlePageStorageDTO>(bucketName, GetTitlePagePath(id));
        if (storageData == null) return null;

        return new TitlePageDTO
        {
            Id = id,
            CreatorId = titlePage.CreatorId,
            Name = storageData.Name ?? "Untitled",
            Description = storageData.Description,
            CreatedAt = storageData.CreatedAt,
            UpdatedAt = titlePage.UpdatedAt
        };
    }

    public async Task<TitlePageDTO> CreateTitlePageAsync(Guid userId, CreateTitlePageDTO dto)
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

        var storageData = new TitlePageStorageDTO
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = now,
            Data = dto.Data ?? new TitlePageData()
        };

        await WriteJsonAsync(bucketName, GetTitlePagePath(titlePageId), storageData);

        return new TitlePageDTO
        {
            Id = titlePageId,
            CreatorId = userId,
            Name = storageData.Name,
            Description = storageData.Description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public async Task<TitlePageDTO> UpdateTitlePageAsync(Guid id, Guid userId, UpdateTitlePageDTO dto)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(tp => tp.Id == id && tp.CreatorId == userId);

        if (titlePage == null)
        {
            throw new FileNotFoundException($"Title page {id} not found");
        }

        titlePage.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var storageData = await ReadJsonAsync<TitlePageStorageDTO>(bucketName, GetTitlePagePath(id));
        if (storageData == null)
        {
            throw new FileNotFoundException($"Title page data for {id} not found in storage");
        }

        // Update fields if provided
        if (dto.Name != null) storageData.Name = dto.Name;
        if (dto.Description != null) storageData.Description = dto.Description;
        if (dto.Data != null) storageData.Data = dto.Data;

        await WriteJsonAsync(bucketName, GetTitlePagePath(id), storageData);

        return new TitlePageDTO
        {
            Id = id,
            CreatorId = userId,
            Name = storageData.Name ?? "Untitled",
            Description = storageData.Description,
            CreatedAt = storageData.CreatedAt,
            UpdatedAt = titlePage.UpdatedAt
        };
    }

    public async Task DeleteTitlePageAsync(Guid id, Guid userId)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(tp => tp.Id == id && tp.CreatorId == userId);

        if (titlePage == null)
        {
            throw new FileNotFoundException($"Title page {id} not found");
        }

        var bucketName = GetBucketName(userId);
        var titlePagePath = GetTitlePagePath(id);

        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(titlePagePath);
            await _minioClient.RemoveObjectAsync(removeArgs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete title page file from MinIO for {TitlePageId}", id);
        }

        _context.TitlePages.Remove(titlePage);
        await _context.SaveChangesAsync();
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

    private async Task<T?> ReadJsonAsync<T>(string bucketName, string objectName)
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
            var content = Encoding.UTF8.GetString(memoryStream.ToArray());
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (ObjectNotFoundException)
        {
            return default;
        }
    }

    private async Task WriteJsonAsync<T>(string bucketName, string objectName, T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
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

    // Internal DTO for storage
    private class TitlePageStorageDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public TitlePageData? Data { get; set; }
    }
}
