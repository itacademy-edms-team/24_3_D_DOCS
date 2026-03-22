using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.TitlePage;
using RusalProject.Models.Entities;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;
using RusalProject.Services.Storage;

namespace RusalProject.Services.TitlePage;

public class TitlePageService : ITitlePageService
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;
    private readonly ILogger<TitlePageService> _logger;

    public TitlePageService(
        ApplicationDbContext context,
        IMinioService minioService,
        ILogger<TitlePageService> logger)
    {
        _context = context;
        _minioService = minioService;
        _logger = logger;
    }

    private string GetUserBucket(Guid userId) => $"user-{userId}";

    public async Task<TitlePageDTO> CreateTitlePageAsync(Guid userId, CreateTitlePageDTO dto)
    {
        try
        {
            var titlePage = new Models.Entities.TitlePage
            {
                CreatorId = userId,
                Name = dto.Name,
                Description = dto.Description,
                MinioPath = $"title-pages/{Guid.NewGuid()}/title-page.json"
            };

            _context.TitlePages.Add(titlePage);
            await _context.SaveChangesAsync();

            // Создаём бакет пользователя если его нет и сохраняем данные в MinIO
            try
            {
                var bucket = GetUserBucket(userId);
                await _minioService.EnsureBucketExistsAsync(bucket);

                // Сохраняем данные титульной страницы в MinIO
                var titlePageData = dto.Data ?? new TitlePageData();
                var titlePageJson = JsonSerializer.Serialize(titlePageData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                var titlePageBytes = Encoding.UTF8.GetBytes(titlePageJson);
                using var titlePageStream = new MemoryStream(titlePageBytes);
                await _minioService.UploadFileAsync(bucket, titlePage.MinioPath, titlePageStream, "application/json");
            }
            catch (Exception minioEx)
            {
                _logger.LogWarning(minioEx, "Failed to save title page data to MinIO for {TitlePageId}, but title page was created in database", titlePage.Id);
                // Продолжаем выполнение, так как запись в БД уже создана
            }

            _logger.LogInformation("Created title page {TitlePageId} for user {UserId}", titlePage.Id, userId);

            return MapToDTO(titlePage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating title page for user {UserId}: {Message}\n{StackTrace}", userId, ex.Message, ex.StackTrace);
            throw;
        }
    }

    public async Task<TitlePageDTO?> GetTitlePageByIdAsync(Guid titlePageId, Guid userId)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(t => t.Id == titlePageId && t.CreatorId == userId);

        if (titlePage == null) return null;

        return MapToDTO(titlePage);
    }

    public async Task<TitlePageWithDataDTO?> GetTitlePageWithDataAsync(Guid titlePageId, Guid userId)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(t => t.Id == titlePageId && t.CreatorId == userId);

        if (titlePage == null) return null;

        var dto = new TitlePageWithDataDTO
        {
            Id = titlePage.Id,
            CreatorId = titlePage.CreatorId,
            Name = titlePage.Name,
            Description = titlePage.Description,
            CreatedAt = titlePage.CreatedAt,
            UpdatedAt = titlePage.UpdatedAt,
            Data = new TitlePageData()
        };

        // Загружаем данные титульной страницы из MinIO
        try
        {
            var bucket = GetUserBucket(userId);
            using var titlePageStream = await _minioService.DownloadFileAsync(bucket, titlePage.MinioPath);
            using var reader = new StreamReader(titlePageStream, Encoding.UTF8);
            var titlePageJson = await reader.ReadToEndAsync();
            dto.Data = JsonSerializer.Deserialize<TitlePageData>(titlePageJson) ?? new TitlePageData();
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Title page data not found for {TitlePageId}", titlePageId);
        }

        return dto;
    }

    public async Task<List<TitlePageDTO>> GetTitlePagesAsync(Guid userId)
    {
        try
        {
            var titlePages = await _context.TitlePages
                .Where(t => t.CreatorId == userId)
                .OrderByDescending(t => t.UpdatedAt)
                .ToListAsync();

            return titlePages.Select(MapToDTO).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting title pages for user {UserId}: {Message}\n{StackTrace}", userId, ex.Message, ex.StackTrace);
            throw;
        }
    }

    public async Task<TitlePageDTO> UpdateTitlePageAsync(Guid titlePageId, Guid userId, UpdateTitlePageDTO dto)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(t => t.Id == titlePageId && t.CreatorId == userId);

        if (titlePage == null)
            throw new FileNotFoundException($"Title page {titlePageId} not found");

        if (dto.Name != null) titlePage.Name = dto.Name;
        if (dto.Description != null) titlePage.Description = dto.Description;

        await _context.SaveChangesAsync();

        // Обновляем данные титульной страницы в MinIO если они переданы
        if (dto.Data != null)
        {
            var bucket = GetUserBucket(userId);
            var titlePageJson = JsonSerializer.Serialize(dto.Data, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            var titlePageBytes = Encoding.UTF8.GetBytes(titlePageJson);
            using var titlePageStream = new MemoryStream(titlePageBytes);
            await _minioService.UploadFileAsync(bucket, titlePage.MinioPath, titlePageStream, "application/json");
        }

        return MapToDTO(titlePage);
    }

    public async Task DeleteTitlePageAsync(Guid titlePageId, Guid userId)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(t => t.Id == titlePageId && t.CreatorId == userId);

        if (titlePage == null)
            throw new FileNotFoundException($"Title page {titlePageId} not found");

        // Удаляем файлы из MinIO
        var bucket = GetUserBucket(userId);
        var titlePagePrefix = titlePage.MinioPath.Substring(0, titlePage.MinioPath.LastIndexOf('/'));
        await _minioService.DeleteDirectoryAsync(bucket, titlePagePrefix);

        // Удаляем из БД
        _context.TitlePages.Remove(titlePage);
        await _context.SaveChangesAsync();
    }

    private static readonly JsonSerializerOptions MinioJsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions MinioJsonWriteOptions = new()
    {
        WriteIndented = true
    };

    public async Task<ConvertTitlePageElementToVariableResponse> ConvertTextElementToVariableAsync(
        Guid titlePageId,
        Guid userId,
        string elementId,
        ConvertTitlePageElementToVariableRequest? request)
    {
        var titlePage = await _context.TitlePages
            .FirstOrDefaultAsync(t => t.Id == titlePageId && t.CreatorId == userId);

        if (titlePage == null)
            throw new FileNotFoundException("Титульная страница не найдена");

        TitlePageData data;
        try
        {
            var bucket = GetUserBucket(userId);
            using var titlePageStream = await _minioService.DownloadFileAsync(bucket, titlePage.MinioPath);
            using var reader = new StreamReader(titlePageStream, Encoding.UTF8);
            var titlePageJson = await reader.ReadToEndAsync();
            data = JsonSerializer.Deserialize<TitlePageData>(titlePageJson, MinioJsonReadOptions) ?? new TitlePageData();
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException("Данные титульной страницы не найдены в хранилище");
        }

        data.Elements ??= new List<TitlePageElement>();
        var element = data.Elements.FirstOrDefault(e => e.Id == elementId);
        if (element == null)
            throw new KeyNotFoundException("Элемент не найден");

        if (!string.Equals(element.Type, "text", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Можно преобразовать только текстовый элемент");

        string innerKey;
        var explicitKey = request?.VariableKey?.Trim();
        if (!string.IsNullOrEmpty(explicitKey))
        {
            innerKey = NormalizeVariableKeyInner(explicitKey);
        }
        else
        {
            var line = FirstNonEmptyLine(element.Text);
            innerKey = NormalizeVariableKeyInner(line);
        }

        if (string.IsNullOrEmpty(innerKey))
            throw new InvalidOperationException("Укажите непустой ключ переменной или текст элемента");

        element.Type = "variable";
        element.Text = "{" + innerKey + "}";
        element.Format = null;
        element.VariableType = null;

        var bucketWrite = GetUserBucket(userId);
        var jsonOut = JsonSerializer.Serialize(data, MinioJsonWriteOptions);
        var bytes = Encoding.UTF8.GetBytes(jsonOut);
        await using var uploadStream = new MemoryStream(bytes);
        await _minioService.UploadFileAsync(bucketWrite, titlePage.MinioPath, uploadStream, "application/json");

        titlePage.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ConvertTitlePageElementToVariableResponse { Element = element };
    }

    private static string FirstNonEmptyLine(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        foreach (var segment in text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
        {
            var t = segment.Trim();
            if (t.Length > 0)
                return t;
        }
        return string.Empty;
    }

    private static string StripOuterBraces(string s)
    {
        var t = s.Trim();
        while (t.Length >= 2 && t[0] == '{' && t[^1] == '}')
            t = t.Substring(1, t.Length - 2).Trim();
        return t;
    }

    private static string NormalizeVariableKeyInner(string raw)
    {
        var inner = StripOuterBraces(raw);
        if (inner.Length > 256)
            throw new InvalidOperationException("Ключ переменной не должен превышать 256 символов");
        return inner;
    }

    private TitlePageDTO MapToDTO(Models.Entities.TitlePage titlePage)
    {
        return new TitlePageDTO
        {
            Id = titlePage.Id,
            CreatorId = titlePage.CreatorId,
            Name = titlePage.Name,
            Description = titlePage.Description,
            CreatedAt = titlePage.CreatedAt,
            UpdatedAt = titlePage.UpdatedAt
        };
    }
}
