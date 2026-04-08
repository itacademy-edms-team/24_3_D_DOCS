using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace RusalProject.Services.Pdf;

public class DomPdfService : IDomPdfService
{
    private readonly PdfDomExportOptions _options;
    private readonly ILogger<DomPdfService> _logger;

    public DomPdfService(
        IOptions<PdfDomExportOptions> options,
        ILogger<DomPdfService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]> GenerateDocumentPdfAsync(
        Guid documentId,
        Guid userId,
        string? accessToken,
        Guid? titlePageId = null,
        Guid? profileId = null,
        bool omitTitlePageInPrint = false)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("DOM PDF export is disabled by configuration.");
        }

        var printUrl = BuildPrintUrl(documentId, titlePageId, profileId, omitTitlePageInPrint);
        var launchOptions = CreateLaunchOptions();

        using var browser = await LaunchBrowserAsync(launchOptions);
        using var page = await browser.NewPageAsync();

        page.DefaultNavigationTimeout = _options.NavigationTimeoutMs;
        page.DefaultTimeout = _options.ReadyTimeoutMs;

        await page.GoToAsync(printUrl, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
            Timeout = _options.NavigationTimeoutMs
        });

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            await InjectAccessTokenAsync(page, accessToken);
            await page.GoToAsync(printUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                Timeout = _options.NavigationTimeoutMs
            });
        }

        await WaitForPrintReadyAsync(page);
        await WaitForFontsAndImagesAsync(page);

        await page.EmulateMediaTypeAsync(MediaType.Print);

        return await page.PdfDataAsync(new PdfOptions
        {
            PrintBackground = true,
            Format = PaperFormat.A4,
            PreferCSSPageSize = false,
            MarginOptions = new MarginOptions
            {
                Top = "0",
                Right = "0",
                Bottom = "0",
                Left = "0"
            }
        });
    }

    private string BuildPrintUrl(Guid documentId, Guid? titlePageId, Guid? profileId, bool omitTitlePageInPrint)
    {
        return BuildPrintUrlCore(
            documentId,
            titlePageId,
            profileId,
            omitTitlePageInPrint,
            _options.FrontendPrintBaseUrl.TrimEnd('/'));
    }

    private static string BuildPrintUrlCore(
        Guid documentId,
        Guid? titlePageId,
        Guid? profileId,
        bool omitTitlePageInPrint,
        string trimmedBase)
    {
        var path = $"{trimmedBase}/print/document/{documentId}";
        var parts = new List<string>();
        if (omitTitlePageInPrint)
            parts.Add("noTitlePage=1");
        else if (titlePageId.HasValue)
            parts.Add($"titlePageId={UrlEncoder.Default.Encode(titlePageId.Value.ToString())}");

        if (profileId.HasValue)
            parts.Add($"profileId={UrlEncoder.Default.Encode(profileId.Value.ToString())}");

        return parts.Count > 0 ? $"{path}?{string.Join("&", parts)}" : path;
    }

    private static LaunchOptions CreateLaunchOptions()
    {
        var launchOptions = new LaunchOptions
        {
            Headless = true,
            Args = new[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--font-render-hinting=none"
            }
        };

        var chromiumPath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrWhiteSpace(chromiumPath))
        {
            launchOptions.ExecutablePath = chromiumPath;
        }

        return launchOptions;
    }

    private async Task<IBrowser> LaunchBrowserAsync(LaunchOptions launchOptions)
    {
        if (!string.IsNullOrWhiteSpace(launchOptions.ExecutablePath))
        {
            return await Puppeteer.LaunchAsync(launchOptions);
        }

        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        return await Puppeteer.LaunchAsync(launchOptions);
    }

    private static async Task InjectAccessTokenAsync(IPage page, string accessToken)
    {
        var authStorage = JsonSerializer.Serialize(new
        {
            state = new
            {
                accessToken
            }
        });

        await page.EvaluateFunctionAsync(
            @"(payload) => {
                localStorage.setItem('auth-storage', payload);
                sessionStorage.removeItem('auth-last-refresh-failure');
            }",
            authStorage
        );
    }

    private async Task WaitForPrintReadyAsync(IPage page)
    {
        try
        {
            await page.WaitForFunctionAsync(
                @"() => window.__PRINT_READY__ === true",
                new WaitForFunctionOptions { Timeout = _options.ReadyTimeoutMs }
            );
        }
        catch (WaitTaskTimeoutException ex)
        {
            var consoleSnapshot = await page.EvaluateExpressionAsync<string>(
                "document?.body?.innerText?.slice(0, 3000) ?? ''"
            );
            _logger.LogError(
                ex,
                "Timed out waiting for __PRINT_READY__. Snapshot: {Snapshot}",
                consoleSnapshot
            );
            throw new InvalidOperationException("Print page did not reach ready state in time.", ex);
        }
    }

    private static async Task WaitForFontsAndImagesAsync(IPage page)
    {
        await page.EvaluateFunctionAsync(
            @"async () => {
                const timeout = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

                if (document.fonts?.ready) {
                    await Promise.race([document.fonts.ready, timeout(8000)]);
                }

                const images = Array.from(document.images || []);
                await Promise.race([
                    Promise.all(images.map((img) => {
                        if (img.complete) {
                            return Promise.resolve();
                        }
                        return new Promise((resolve) => {
                            img.addEventListener('load', resolve, { once: true });
                            img.addEventListener('error', resolve, { once: true });
                        });
                    })),
                    timeout(8000),
                ]);
            }"
        );
    }
}
