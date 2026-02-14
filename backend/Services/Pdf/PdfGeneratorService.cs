using System.Text.Json;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using RusalProject.Services.Document;
using RusalProject.Services.Profile;
using RusalProject.Services.TitlePage;
using RusalProject.Services.Storage;
using System.Text.RegularExpressions;

namespace RusalProject.Services.Pdf;

public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly IDocumentService _documentService;
    private readonly IProfileService _profileService;
    private readonly ITitlePageService _titlePageService;
    private readonly IMinioService _minioService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PdfGeneratorService> _logger;

    public PdfGeneratorService(
        IDocumentService documentService,
        IProfileService profileService,
        ITitlePageService titlePageService,
        IMinioService minioService,
        IConfiguration configuration,
        ILogger<PdfGeneratorService> logger)
    {
        _documentService = documentService;
        _profileService = profileService;
        _titlePageService = titlePageService;
        _minioService = minioService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<byte[]> GeneratePdfAsync(Guid documentId, Guid userId, Guid? titlePageId = null)
    {
        try
        {
            // Get document with content
            var document = await _documentService.GetDocumentWithContentAsync(documentId, userId);
            if (document == null)
            {
                throw new FileNotFoundException($"Document {documentId} not found");
            }

            // Get profile
            Models.DTOs.Profile.ProfileWithDataDTO? profile = null;
            if (document.ProfileId.HasValue)
            {
                profile = await _profileService.GetProfileWithDataAsync(document.ProfileId.Value, userId);
            }

            // Get title page
            Models.DTOs.TitlePage.TitlePageWithDataDTO? titlePage = null;
            if (titlePageId.HasValue)
            {
                titlePage = await _titlePageService.GetTitlePageWithDataAsync(titlePageId.Value, userId);
            }
            else if (document.TitlePageId.HasValue)
            {
                titlePage = await _titlePageService.GetTitlePageWithDataAsync(document.TitlePageId.Value, userId);
            }

            // Get title page variables from metadata
            var titlePageVariables = new Dictionary<string, string>();
            if (document.Metadata != null)
            {
                // Convert DocumentMetadataDTO to Dictionary
                if (!string.IsNullOrEmpty(document.Metadata.Title))
                    titlePageVariables["Title"] = document.Metadata.Title;
                if (!string.IsNullOrEmpty(document.Metadata.Author))
                    titlePageVariables["Author"] = document.Metadata.Author;
                if (!string.IsNullOrEmpty(document.Metadata.Group))
                    titlePageVariables["Group"] = document.Metadata.Group;
                if (!string.IsNullOrEmpty(document.Metadata.Year))
                    titlePageVariables["Year"] = document.Metadata.Year;
                if (!string.IsNullOrEmpty(document.Metadata.City))
                    titlePageVariables["City"] = document.Metadata.City;
                if (!string.IsNullOrEmpty(document.Metadata.Supervisor))
                    titlePageVariables["Supervisor"] = document.Metadata.Supervisor;
                if (!string.IsNullOrEmpty(document.Metadata.DocumentType))
                    titlePageVariables["DocumentType"] = document.Metadata.DocumentType;
                
                // Add additional fields if any
                if (document.Metadata.AdditionalFields != null)
                {
                    foreach (var field in document.Metadata.AdditionalFields)
                    {
                        titlePageVariables[field.Key] = field.Value;
                    }
                }
            }

            // Get overrides
            var overrides = document.StyleOverrides ?? new Dictionary<string, object>();

            // Get table of contents
            var tableOfContents = await _documentService.GetTableOfContentsAsync(documentId, userId);
            var tableOfContentsSettings = profile?.Data?.TableOfContents;

            // Get base URL for images
            var baseUrl = _configuration["MinIO:PublicUrl"] ?? "http://localhost:5000";
            // If we're in a web context, prefer the actual request URL
            // For now, use the configured public URL

            // Prepare data for rendering
            // Embed images in markdown so Puppeteer doesn't require external access
            var processedMarkdown = await EmbedImagesInMarkdownAsync(document.Content ?? string.Empty, documentId, userId);
            // Attempt to embed images referenced in title page data (no-op if none)
            if (titlePage != null)
            {
                titlePage.Data = await EmbedImagesInTitlePageDataAsync(titlePage.Data, userId);
            }

            var renderData = new
            {
                markdown = processedMarkdown,
                profile = profile?.Data,
                titlePage = titlePage != null ? new
                {
                    data = titlePage.Data
                } : null,
                titlePageVariables = titlePageVariables,
                overrides = overrides,
                baseUrl = baseUrl,
                tableOfContents = tableOfContents,
                tableOfContentsSettings = tableOfContentsSettings
            };

            // Generate PDF using Puppeteer
            return await GeneratePdfFromHtmlAsync(renderData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<byte[]> GenerateTitlePagePdfAsync(Guid titlePageId, Guid userId, Dictionary<string, string>? variables = null)
    {
        try
        {
            // Get title page
            var titlePage = await _titlePageService.GetTitlePageWithDataAsync(titlePageId, userId);
            if (titlePage == null)
            {
                throw new FileNotFoundException($"Title page {titlePageId} not found");
            }

            // Prepare data for rendering
            // Embed images in title page data if any (no-op for most cases)
            titlePage.Data = await EmbedImagesInTitlePageDataAsync(titlePage.Data, userId);

            var renderData = new
            {
                markdown = string.Empty,
                profile = (object?)null,
                titlePage = new
                {
                    data = titlePage.Data
                },
                titlePageVariables = variables ?? new Dictionary<string, string>(),
                overrides = new Dictionary<string, object>(),
                baseUrl = _configuration["MinIO:PublicUrl"] ?? "http://localhost:5000"
            };

            // Generate PDF using Puppeteer
            return await GeneratePdfFromHtmlAsync(renderData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for title page {TitlePageId}", titlePageId);
            throw;
        }
    }

    /// <summary>
    /// Replace image URLs in markdown with data:URIs by fetching assets from MinIO when they belong to the given document/user.
    /// </summary>
    private async Task<string> EmbedImagesInMarkdownAsync(string markdown, Guid documentId, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return markdown;

        try
        {
            // Patterns for markdown images and HTML <img> tags
            var mdImgRegex = new Regex(@"!\[[^\]]*\]\((?<url>[^)\s]+)(?:\s+""[^""]*"")?\)", RegexOptions.Compiled);
            var htmlImgRegex = new Regex(@"<img[^>]+src\s*=\s*[""'](?<url>[^""']+)[""'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var urls = new HashSet<string>();

            foreach (Match m in mdImgRegex.Matches(markdown))
            {
                var u = m.Groups["url"].Value;
                if (!string.IsNullOrWhiteSpace(u)) urls.Add(u);
            }
            foreach (Match m in htmlImgRegex.Matches(markdown))
            {
                var u = m.Groups["url"].Value;
                if (!string.IsNullOrWhiteSpace(u)) urls.Add(u);
            }

            if (urls.Count == 0) return markdown;

            var result = markdown;

            foreach (var url in urls)
            {
                try
                {
                    if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;

                    // Extract filename from URL (strip query)
                    var pathPart = url.Split('?', '#')[0];
                    var fileName = pathPart.Contains('/') ? pathPart.Substring(pathPart.LastIndexOf('/') + 1) : pathPart;
                    if (string.IsNullOrEmpty(fileName)) continue;

                    // Construct MinIO object path and bucket
                    var bucket = $"user-{userId}";
                    var objectPath = $"documents/{documentId}/assets/{fileName}";

                    var exists = await _minioService.FileExistsAsync(bucket, objectPath);
                    if (!exists)
                    {
                        // Not an uploaded asset we can embed
                        continue;
                    }

                    await using var stream = await _minioService.DownloadFileAsync(bucket, objectPath);
                    if (stream == null) continue;

                    byte[] bytes;
                    await using (var ms = new MemoryStream())
                    {
                        await stream.CopyToAsync(ms);
                        bytes = ms.ToArray();
                    }

                    var ext = Path.GetExtension(fileName).ToLowerInvariant();
                    var contentType = ext switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".gif" => "image/gif",
                        ".webp" => "image/webp",
                        ".svg" => "image/svg+xml",
                        _ => "application/octet-stream"
                    };

                    var base64 = Convert.ToBase64String(bytes);
                    var dataUri = $"data:{contentType};base64,{base64}";

                    // Replace all occurrences of this URL in markdown (both md and html forms)
                    result = result.Replace(url, dataUri);
                }
                catch (Exception exUrl)
                {
                    _logger.LogWarning(exUrl, "Failed to embed image {Url} for document {DocumentId}", url, documentId);
                    // continue with other images
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EmbedImagesInMarkdownAsync failed for document {DocumentId}", documentId);
            return markdown;
        }
    }

    /// <summary>
    /// Attempt to replace image references inside title page data with data:URIs.
    /// TitlePageData rarely contains image sources in current schema; implement as safe no-op unless schema includes image fields.
    /// </summary>
    private Task<Models.Types.TitlePageData?> EmbedImagesInTitlePageDataAsync(Models.Types.TitlePageData? titlePageData, Guid userId)
    {
        // Current TitlePageData/TitlePageElement does not include image fields, so just return unchanged data.
        // This method exists to satisfy the plan and provides a hook for future enhancements.
        return Task.FromResult(titlePageData);
    }

    private async Task<byte[]> GeneratePdfFromHtmlAsync(object renderData)
    {
        var htmlTemplatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Services",
            "Pdf",
            "PdfPreviewRenderer.html"
        );

        if (!File.Exists(htmlTemplatePath))
        {
            throw new FileNotFoundException($"HTML template not found at {htmlTemplatePath}");
        }

        var htmlTemplate = await File.ReadAllTextAsync(htmlTemplatePath);

        // Load JavaScript renderer
        var jsRendererPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Services",
            "Pdf",
            "PdfPreviewRenderer.js"
        );

        if (!File.Exists(jsRendererPath))
        {
            throw new FileNotFoundException($"JavaScript renderer not found at {jsRendererPath}");
        }

        var jsRenderer = await File.ReadAllTextAsync(jsRendererPath);

        // Inject render data into HTML
        var jsonData = JsonSerializer.Serialize(renderData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        // Replace script tag with inline JavaScript
        var htmlWithData = htmlTemplate
            .Replace("<script src=\"PdfPreviewRenderer.js\"></script>", $"<script>{jsRenderer}</script>")
            .Replace(
                "</body>",
                $@"
<script>
    // Wait for all dependencies to load and render
    (async function() {{
        // Wait for markdown-it and other dependencies
        function waitForDependencies() {{
            return new Promise((resolve) => {{
                if (typeof markdownit !== 'undefined' && 
                    typeof katex !== 'undefined' && 
                    typeof renderPreview !== 'undefined') {{
                    resolve();
                }} else {{
                    const checkInterval = setInterval(() => {{
                        if (typeof markdownit !== 'undefined' && 
                            typeof katex !== 'undefined' && 
                            typeof renderPreview !== 'undefined') {{
                            clearInterval(checkInterval);
                            resolve();
                        }}
                    }}, 100);
                    
                    // Timeout after 10 seconds
                    setTimeout(() => {{
                        clearInterval(checkInterval);
                        resolve();
                    }}, 10000);
                }}
            }});
        }}
        
        await waitForDependencies();
        
        const renderData = {jsonData};
        if (typeof renderPreview !== 'undefined') {{
            // Support async renderPreview (returns Promise) — set renderComplete when it finishes
            Promise.resolve(renderPreview(renderData))
                .then(() => {{
                    window.renderComplete = true;
                }})
                .catch((err) => {{
                    console.error('renderPreview failed', err);
                    window.renderComplete = false;
                }});
        }} else {{
            console.error('renderPreview function not found');
            window.renderComplete = false;
        }}
    }})();
</script>
</body>"
            );

        // Launch browser
        var launchOptions = new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
        };

        // Use environment variable for Chromium path if available (for Docker)
        var chromiumPath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrEmpty(chromiumPath))
        {
            launchOptions.ExecutablePath = chromiumPath;
        }
        else
        {
            // Download browser if not available
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
        }

        using var browser = await Puppeteer.LaunchAsync(launchOptions);
        using var page = await browser.NewPageAsync();

        // Set content
        await page.SetContentAsync(htmlWithData, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
        });

        // Wait for rendering to complete - check both renderComplete flag and DOM element
        try
        {
            await page.WaitForFunctionAsync(@"() => {
                return (window.renderComplete === true) && 
                       (document.querySelector('.document-preview__page') !== null);
            }", new WaitForFunctionOptions
            {
                Timeout = 60000 // Increase timeout to 60 seconds
            });
        }
        catch (WaitTaskTimeoutException)
        {
            // Log error but try to continue - check what's in the page
            var pageContent = await page.GetContentAsync();
            _logger.LogWarning("Timeout waiting for render. Page content length: {Length}", pageContent.Length);
            
            // Try to wait a bit more and check if element exists
            await Task.Delay(5000);
            var elementExists = await page.EvaluateExpressionAsync<bool>(
                "document.querySelector('.document-preview__page') !== null"
            );
            
            if (!elementExists)
            {
                throw new InvalidOperationException("Failed to render document preview. Element not found after timeout.");
            }
        }

        // Additional wait for images to load and rendering to stabilize
        await Task.Delay(2000);

        // Generate PDF with CSS page sizes (respects profile settings)
        var pdfOptions = new PdfOptions
        {
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "0",
                Right = "0",
                Bottom = "0",
                Left = "0"
            },
            PreferCSSPageSize = true // Use CSS page sizes from profile
        };

        var pdfBytes = await page.PdfDataAsync(pdfOptions);

        return pdfBytes;
    }
}
