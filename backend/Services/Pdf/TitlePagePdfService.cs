using System.Text.Json;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using RusalProject.Services.TitlePage;

namespace RusalProject.Services.Pdf;

/// <summary>
/// Renders a title page to PDF via the bundled PdfPreviewRenderer (Puppeteer).
/// Document body PDF is handled separately by <see cref="DomPdfService"/>.
/// </summary>
public class TitlePagePdfService : ITitlePagePdfService
{
    private readonly ITitlePageService _titlePageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TitlePagePdfService> _logger;

    public TitlePagePdfService(
        ITitlePageService titlePageService,
        IConfiguration configuration,
        ILogger<TitlePagePdfService> logger)
    {
        _titlePageService = titlePageService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<byte[]> GenerateTitlePagePdfAsync(Guid titlePageId, Guid userId, Dictionary<string, string>? variables = null)
    {
        try
        {
            var titlePage = await _titlePageService.GetTitlePageWithDataAsync(titlePageId, userId);
            if (titlePage == null)
            {
                throw new FileNotFoundException($"Title page {titlePageId} not found");
            }

            titlePage.Data = await EmbedImagesInTitlePageDataAsync(titlePage.Data) ?? new Models.Types.TitlePageData();

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

            return await GeneratePdfFromHtmlAsync(renderData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for title page {TitlePageId}", titlePageId);
            throw;
        }
    }

    private static Task<Models.Types.TitlePageData?> EmbedImagesInTitlePageDataAsync(Models.Types.TitlePageData? titlePageData)
    {
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

        var jsonData = JsonSerializer.Serialize(renderData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var htmlWithData = htmlTemplate
            .Replace("<script src=\"PdfPreviewRenderer.js\"></script>", $"<script>{jsRenderer}</script>")
            .Replace(
                "</body>",
                $@"
<script>
    (async function() {{
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

        var launchOptions = new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
        };

        var chromiumPath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrEmpty(chromiumPath))
        {
            launchOptions.ExecutablePath = chromiumPath;
        }
        else
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
        }

        using var browser = await Puppeteer.LaunchAsync(launchOptions);
        using var page = await browser.NewPageAsync();

        await page.SetContentAsync(htmlWithData, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
        });

        try
        {
            await page.WaitForFunctionAsync(@"() => {
                return (window.renderComplete === true) &&
                       (document.querySelector('.document-preview__page') !== null);
            }", new WaitForFunctionOptions
            {
                Timeout = 60000
            });
        }
        catch (WaitTaskTimeoutException)
        {
            var pageContent = await page.GetContentAsync();
            _logger.LogWarning("Timeout waiting for render. Page content length: {Length}", pageContent.Length);

            await Task.Delay(5000);
            var elementExists = await page.EvaluateExpressionAsync<bool>(
                "document.querySelector('.document-preview__page') !== null"
            );

            if (!elementExists)
            {
                throw new InvalidOperationException("Failed to render document preview. Element not found after timeout.");
            }
        }

        await Task.Delay(2000);

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
            PreferCSSPageSize = true
        };

        return await page.PdfDataAsync(pdfOptions);
    }
}
