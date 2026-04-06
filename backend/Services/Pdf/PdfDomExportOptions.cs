namespace RusalProject.Services.Pdf;

public class PdfDomExportOptions
{
    public bool Enabled { get; set; } = true;
    public string FrontendPrintBaseUrl { get; set; } = "http://frontend";
    public int NavigationTimeoutMs { get; set; } = 60000;
    public int ReadyTimeoutMs { get; set; } = 120000;
}
