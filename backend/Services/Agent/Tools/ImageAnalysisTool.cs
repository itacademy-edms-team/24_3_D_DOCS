using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RusalProject.Services.Documents;
using RusalProject.Services.Ollama;
using RusalProject.Services.Storage;

namespace RusalProject.Services.Agent.Tools;

public class ImageAnalysisTool : ITool
{
    private readonly IDocumentService _documentService;
    private readonly IMinioService _minioService;
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<ImageAnalysisTool> _logger;

    // Имя инструмента соответствует новому системному промпту: image_analyse(id)
    public string Name => "image_analyse";
    public string Description => "Анализирует изображение по номеру строки (1-based) и возвращает описание/подпись.";

    public ImageAnalysisTool(
        IDocumentService documentService,
        IMinioService minioService,
        IOllamaService ollamaService,
        ILogger<ImageAnalysisTool> logger)
    {
        _documentService = documentService;
        _minioService = minioService;
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public Dictionary<string, object> GetParametersSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                // document_id и user_id добавляются агентом автоматически
                ["id"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "1-based номер строки с изображением в формате ![alt](url)"
                }
            },
            ["required"] = new[] { "id" }
        };
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentId = Guid.Parse(GetStringValue(arguments, "document_id"));
            var userId = Guid.Parse(GetStringValue(arguments, "user_id"));
            // Поддерживаем и новый ключ id, и старый line_number для обратной совместимости
            int lineNumber;
            if (arguments.ContainsKey("id"))
            {
                lineNumber = GetIntValue(arguments, "id");
            }
            else if (arguments.ContainsKey("line_number"))
            {
                lineNumber = GetIntValue(arguments, "line_number");
            }
            else
            {
                return "Ошибка: id (или line_number) обязателен для image_analyse";
            }

            // Get document content
            var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
            if (document == null)
            {
                return "Ошибка: Документ не найден";
            }

            var lines = (document.Content ?? string.Empty).Split('\n');
            if (lineNumber < 1 || lineNumber > lines.Length)
            {
                return $"Ошибка: Номер строки {lineNumber} вне диапазона документа (1-{lines.Length})";
            }

            // Search for image in a range around the specified line (StartLine might be block start, not image line)
            // Check ±3 lines around the specified line
            var searchStart = Math.Max(0, lineNumber - 4); // -1 for 0-based, -3 for range
            var searchEnd = Math.Min(lines.Length, lineNumber + 2); // +1 for 0-based, +1 for range
            
            string? imageLine = null;
            int actualLineNumber = lineNumber;
            
            for (int i = searchStart; i < searchEnd; i++)
            {
                var line = lines[i];
                var imageMatch = Regex.Match(line, @"!\[([^\]]*)\]\(([^)]+)\)");
                if (imageMatch.Success)
                {
                    imageLine = line;
                    actualLineNumber = i + 1; // Convert back to 1-based
                    break;
                }
            }

            if (imageLine == null)
            {
                // Show what we found in the search range for debugging
                var rangeLines = string.Join(" | ", lines.Skip(searchStart).Take(searchEnd - searchStart).Select((l, idx) => $"{searchStart + idx + 1}:{l.Trim()}"));
                return $"Ошибка: На строке {lineNumber} и в диапазоне {searchStart + 1}-{searchEnd} не найдено изображение в формате ![alt](url). Проверенные строки: {rangeLines}";
            }

            // Parse markdown image syntax: ![alt](url) - imageMatch already found in loop above
            var finalImageMatch = Regex.Match(imageLine, @"!\[([^\]]*)\]\(([^)]+)\)");
            if (!finalImageMatch.Success)
            {
                return $"Ошибка: Не удалось распарсить изображение на строке {actualLineNumber}";
            }

            var imageUrl = finalImageMatch.Groups[2].Value;

            // Extract fileName from URL
            // URLs can be like: /api/upload/document/{documentId}/asset/{fileName} or full URLs
            // or just fileName
            string fileName;
            if (imageUrl.Contains("/asset/"))
            {
                fileName = imageUrl.Substring(imageUrl.LastIndexOf("/asset/") + "/asset/".Length);
                // Remove query parameters if present
                var queryIndex = fileName.IndexOf('?');
                if (queryIndex >= 0)
                {
                    fileName = fileName.Substring(0, queryIndex);
                }
            }
            else if (imageUrl.Contains("/"))
            {
                fileName = imageUrl.Substring(imageUrl.LastIndexOf('/') + 1);
            }
            else
            {
                fileName = imageUrl;
            }

            _logger.LogInformation("ImageAnalysisTool: Extracted fileName={FileName} from URL={Url}", fileName, imageUrl);

            // Get image from MinIO
            var bucket = $"user-{userId}";
            var objectPath = $"documents/{documentId}/assets/{fileName}";

            if (!await _minioService.FileExistsAsync(bucket, objectPath))
            {
                return $"Ошибка: Изображение {fileName} не найдено в хранилище";
            }

            using var imageStream = await _minioService.DownloadFileAsync(bucket, objectPath);
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, cancellationToken);
            var imageBytes = memoryStream.ToArray();

            // Convert to base64
            var imageBase64 = Convert.ToBase64String(imageBytes);

            // Call vision model
            var prompt = "Проанализируй это изображение и создай краткое, информативное описание для подписи. Описание должно описывать содержимое изображения. Не добавляй номера или префиксы типа 'Рисунок 1:' - только текст описания.";
            var caption = await _ollamaService.GenerateVisionChatAsync(prompt, imageBase64, cancellationToken);

            // Clean up caption (remove any numbering the model might have added)
            caption = caption.Trim();
            // Remove common prefixes like "Рисунок 1:", "Image 1:", etc.
            caption = Regex.Replace(caption, @"^(Рисунок|Рис\.|Image|Рисунок\s+\d+[:\-]?\s*)", "", RegexOptions.IgnoreCase);
            caption = caption.Trim();

            // Return both the actual line number and the caption
            // Format: "IMAGE_LINE: <actualLineNumber>\nCAPTION: <caption>"
            // This allows the agent to use the correct line number for document_edit
            return $"IMAGE_LINE: {actualLineNumber}\nCAPTION: {caption}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing image analysis tool");
            return $"Ошибка при анализе изображения: {ex.Message}";
        }
    }

    private static string GetStringValue(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value))
        {
            throw new ArgumentException($"Missing required argument: {key}");
        }

        return value switch
        {
            string str => str,
            JsonElement jsonElement => jsonElement.GetString() ?? throw new InvalidOperationException($"Cannot convert {key} to string"),
            _ => value.ToString() ?? throw new InvalidOperationException($"Cannot convert {key} to string")
        };
    }

    private static int GetIntValue(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value))
        {
            throw new ArgumentException($"Missing required argument: {key}");
        }

        return value switch
        {
            int i => i,
            long l => (int)l,
            JsonElement jsonElement => jsonElement.GetInt32(),
            _ => Convert.ToInt32(value)
        };
    }
}
