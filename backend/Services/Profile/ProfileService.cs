using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.Profile;
using RusalProject.Models.Entities;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;
using RusalProject.Services.Storage;

namespace RusalProject.Services.Profile;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        ApplicationDbContext context,
        IMinioService minioService,
        ILogger<ProfileService> logger)
    {
        _context = context;
        _minioService = minioService;
        _logger = logger;
    }

    private string GetUserBucket(Guid userId) => $"user-{userId}";

    private static ProfileData GetDefaultProfileData()
    {
        const string defaultFontFamily = "Times New Roman";
        const double defaultFontSize = 14.0; // pt
        const double defaultLineHeight = 1.5; // ГОСТ
        const double defaultTextIndent = 1.25; // см, ГОСТ

        return new ProfileData
        {
            PageSettings = new PageSettings
            {
                Size = "A4",
                Orientation = "portrait",
                Margins = new PageMargins
                {
                    Top = 20,
                    Right = 20,
                    Bottom = 20,
                    Left = 20
                },
                PageNumbers = new PageNumberSettings
                {
                    Enabled = true,
                    Position = "bottom",
                    Align = "center",
                    Format = "{n}",
                    FontSize = 12,
                    FontStyle = "normal",
                    FontFamily = defaultFontFamily
                },
                GlobalLineHeight = defaultLineHeight
            },
            EntityStyles = new Dictionary<string, EntityStyle>
            {
                // Параграф - ГОСТ
                [EntityTypes.Paragraph] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize,
                    FontWeight = "normal",
                    FontStyle = "normal",
                    TextAlign = "justify", // По ширине
                    TextIndent = defaultTextIndent,
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 0,
                    MarginBottom = 0
                },
                // Заголовки H1-H6 - ГОСТ
                [EntityTypes.Heading1] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = 18,
                    FontWeight = "bold",
                    TextAlign = "center",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 12,
                    MarginBottom = 12
                },
                [EntityTypes.Heading2] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = 16,
                    FontWeight = "bold",
                    TextAlign = "center",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 10,
                    MarginBottom = 10
                },
                [EntityTypes.Heading3] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = 15,
                    FontWeight = "bold",
                    TextAlign = "left",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 8,
                    MarginBottom = 8
                },
                [EntityTypes.Heading4] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = 14,
                    FontWeight = "bold",
                    TextAlign = "left",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 6,
                    MarginBottom = 6
                },
                [EntityTypes.Heading5] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = 13,
                    FontWeight = "bold",
                    TextAlign = "left",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 6,
                    MarginBottom = 6
                },
                [EntityTypes.Heading6] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = 12,
                    FontWeight = "bold",
                    TextAlign = "left",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 6,
                    MarginBottom = 6
                },
                // Списки - ГОСТ
                [EntityTypes.OrderedList] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize,
                    FontWeight = "normal",
                    TextAlign = "left",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 6,
                    MarginBottom = 6,
                    ListUseParagraphTextIndent = true
                },
                [EntityTypes.UnorderedList] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize,
                    FontWeight = "normal",
                    TextAlign = "left",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 6,
                    MarginBottom = 6,
                    ListUseParagraphTextIndent = true
                },
                // Таблицы - ГОСТ
                [EntityTypes.Table] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize,
                    TextAlign = "center",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 6,
                    MarginBottom = 6,
                    BorderWidth = 1,
                    BorderColor = "#000000",
                    BorderStyle = "solid"
                },
                // Изображения - ГОСТ
                [EntityTypes.Image] = new EntityStyle
                {
                    TextAlign = "center",
                    MaxWidth = 100,
                    MarginTop = 6,
                    MarginBottom = 6
                },
                // Формулы - ГОСТ
                [EntityTypes.Formula] = new EntityStyle
                {
                    TextAlign = "center",
                    MarginTop = 6,
                    MarginBottom = 6
                },
                // Подписи - ГОСТ
                [EntityTypes.ImageCaption] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize,
                    TextAlign = "center",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 0,
                    MarginBottom = 12,
                    CaptionFormat = "Рисунок {n} - {content}"
                },
                [EntityTypes.TableCaption] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize,
                    TextAlign = "center",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 0,
                    MarginBottom = 12,
                    CaptionFormat = "Таблица {n} - {content}"
                },
                [EntityTypes.FormulaCaption] = new EntityStyle
                {
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize,
                    TextAlign = "center",
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true,
                    MarginTop = 0,
                    MarginBottom = 12,
                    CaptionFormat = "Формула {n} - {content}"
                },
                // Выделенный текст
                ["highlight"] = new EntityStyle
                {
                    HighlightColor = "#000000",
                    HighlightBackgroundColor = "#ffeb3b"
                },
                // Код
                [EntityTypes.CodeBlock] = new EntityStyle
                {
                    FontFamily = "Courier New",
                    FontSize = 12,
                    BackgroundColor = "#f5f5f5",
                    MarginTop = 6,
                    MarginBottom = 6,
                    LineHeight = defaultLineHeight,
                    LineHeightUseGlobal = true
                }
            },
            HeadingNumbering = new HeadingNumberingSettings
            {
                Templates = new Dictionary<int, HeadingTemplate>
                {
                    [1] = new HeadingTemplate { Format = "{n} {content}", Enabled = false },
                    [2] = new HeadingTemplate { Format = "{n} {content}", Enabled = false },
                    [3] = new HeadingTemplate { Format = "{n} {content}", Enabled = false },
                    [4] = new HeadingTemplate { Format = "{n} {content}", Enabled = false },
                    [5] = new HeadingTemplate { Format = "{n} {content}", Enabled = false },
                    [6] = new HeadingTemplate { Format = "{n} {content}", Enabled = false }
                }
            }
        };
    }

    public async Task<ProfileDTO> CreateProfileAsync(Guid userId, CreateProfileDTO dto)
    {
        var profile = new SchemaLink
        {
            CreatorId = userId,
            Name = dto.Name,
            Description = dto.Description,
            IsPublic = dto.IsPublic,
            MinioPath = $"profiles/{Guid.NewGuid()}/profile.json"
        };

        _context.SchemaLinks.Add(profile);
        await _context.SaveChangesAsync();

        // Создаём бакет пользователя если его нет
        var bucket = GetUserBucket(userId);
        await _minioService.EnsureBucketExistsAsync(bucket);

        // Сохраняем данные профиля в MinIO
        // Если данные не переданы, используем значения по умолчанию по ГОСТу
        var profileData = dto.Data ?? GetDefaultProfileData();
        var profileJson = JsonSerializer.Serialize(profileData, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        var profileBytes = Encoding.UTF8.GetBytes(profileJson);
        using var profileStream = new MemoryStream(profileBytes);
        await _minioService.UploadFileAsync(bucket, profile.MinioPath, profileStream, "application/json");

        _logger.LogInformation("Created profile {ProfileId} for user {UserId}", profile.Id, userId);

        return MapToDTO(profile);
    }

    public async Task<ProfileDTO?> GetProfileByIdAsync(Guid profileId, Guid userId)
    {
        var profile = await _context.SchemaLinks
            .FirstOrDefaultAsync(p => p.Id == profileId && 
                (p.CreatorId == userId || p.IsPublic));

        if (profile == null) return null;

        return MapToDTO(profile);
    }

    public async Task<ProfileWithDataDTO?> GetProfileWithDataAsync(Guid profileId, Guid userId)
    {
        var profile = await _context.SchemaLinks
            .FirstOrDefaultAsync(p => p.Id == profileId && 
                (p.CreatorId == userId || p.IsPublic));

        if (profile == null) return null;

        var dto = new ProfileWithDataDTO
        {
            Id = profile.Id,
            CreatorId = profile.CreatorId,
            Name = profile.Name,
            Description = profile.Description,
            IsPublic = profile.IsPublic,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            Data = new ProfileData()
        };

        // Загружаем данные профиля из MinIO
        try
        {
            var bucket = GetUserBucket(profile.CreatorId);
            using var profileStream = await _minioService.DownloadFileAsync(bucket, profile.MinioPath);
            using var reader = new StreamReader(profileStream, Encoding.UTF8);
            var profileJson = await reader.ReadToEndAsync();
            dto.Data = JsonSerializer.Deserialize<ProfileData>(profileJson) ?? GetDefaultProfileData();
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Profile data not found for {ProfileId}, using defaults", profileId);
            dto.Data = GetDefaultProfileData();
        }

        return dto;
    }

    public async Task<List<ProfileDTO>> GetProfilesAsync(Guid userId, bool includePublic = true)
    {
        var query = _context.SchemaLinks.AsQueryable();

        if (includePublic)
        {
            query = query.Where(p => p.CreatorId == userId || p.IsPublic);
        }
        else
        {
            query = query.Where(p => p.CreatorId == userId);
        }

        var profiles = await query
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

        return profiles.Select(MapToDTO).ToList();
    }

    public async Task<ProfileDTO> UpdateProfileAsync(Guid profileId, Guid userId, UpdateProfileDTO dto)
    {
        var profile = await _context.SchemaLinks
            .FirstOrDefaultAsync(p => p.Id == profileId && p.CreatorId == userId);

        if (profile == null)
            throw new FileNotFoundException($"Profile {profileId} not found");

        if (dto.Name != null) profile.Name = dto.Name;
        if (dto.Description != null) profile.Description = dto.Description;
        if (dto.IsPublic.HasValue) profile.IsPublic = dto.IsPublic.Value;

        await _context.SaveChangesAsync();

        // Обновляем данные профиля в MinIO если они переданы
        if (dto.Data != null)
        {
            var bucket = GetUserBucket(userId);
            var profileJson = JsonSerializer.Serialize(dto.Data, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            var profileBytes = Encoding.UTF8.GetBytes(profileJson);
            using var profileStream = new MemoryStream(profileBytes);
            await _minioService.UploadFileAsync(bucket, profile.MinioPath, profileStream, "application/json");
        }

        return MapToDTO(profile);
    }

    public async Task DeleteProfileAsync(Guid profileId, Guid userId)
    {
        var profile = await _context.SchemaLinks
            .FirstOrDefaultAsync(p => p.Id == profileId && p.CreatorId == userId);

        if (profile == null)
            throw new FileNotFoundException($"Profile {profileId} not found");

        // Удаляем файлы из MinIO
        var bucket = GetUserBucket(userId);
        var profilePrefix = profile.MinioPath.Substring(0, profile.MinioPath.LastIndexOf('/'));
        await _minioService.DeleteDirectoryAsync(bucket, profilePrefix);

        // Удаляем из БД
        _context.SchemaLinks.Remove(profile);
        await _context.SaveChangesAsync();
    }

    public async Task<ProfileDTO> DuplicateProfileAsync(Guid profileId, Guid userId, string? newName = null)
    {
        var sourceProfile = await _context.SchemaLinks
            .FirstOrDefaultAsync(p => p.Id == profileId && 
                (p.CreatorId == userId || p.IsPublic));

        if (sourceProfile == null)
            throw new FileNotFoundException($"Profile {profileId} not found");

        // Загружаем данные исходного профиля
        ProfileData? sourceData = null;
        try
        {
            var bucket = GetUserBucket(sourceProfile.CreatorId);
            using var profileStream = await _minioService.DownloadFileAsync(bucket, sourceProfile.MinioPath);
            using var reader = new StreamReader(profileStream, Encoding.UTF8);
            var profileJson = await reader.ReadToEndAsync();
            sourceData = JsonSerializer.Deserialize<ProfileData>(profileJson);
        }
        catch (FileNotFoundException)
        {
            sourceData = GetDefaultProfileData();
        }

        // Создаём новый профиль
        var newProfile = new SchemaLink
        {
            CreatorId = userId,
            Name = newName ?? $"{sourceProfile.Name} (копия)",
            Description = sourceProfile.Description,
            IsPublic = false, // Копия всегда приватная
            MinioPath = $"profiles/{Guid.NewGuid()}/profile.json"
        };

        _context.SchemaLinks.Add(newProfile);
        await _context.SaveChangesAsync();

        // Сохраняем данные профиля в MinIO
        var newBucket = GetUserBucket(userId);
        await _minioService.EnsureBucketExistsAsync(newBucket);
        
        var profileData = sourceData ?? GetDefaultProfileData();
        var newProfileJson = JsonSerializer.Serialize(profileData, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        var newProfileBytes = Encoding.UTF8.GetBytes(newProfileJson);
        using var newProfileStream = new MemoryStream(newProfileBytes);
        await _minioService.UploadFileAsync(newBucket, newProfile.MinioPath, newProfileStream, "application/json");

        return MapToDTO(newProfile);
    }

    public async Task<bool> ProfileExistsAsync(Guid profileId, Guid userId)
    {
        return await _context.SchemaLinks
            .AnyAsync(p => p.Id == profileId && (p.CreatorId == userId || p.IsPublic));
    }

    private ProfileDTO MapToDTO(SchemaLink profile)
    {
        return new ProfileDTO
        {
            Id = profile.Id,
            CreatorId = profile.CreatorId,
            Name = profile.Name,
            Description = profile.Description,
            IsPublic = profile.IsPublic,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
