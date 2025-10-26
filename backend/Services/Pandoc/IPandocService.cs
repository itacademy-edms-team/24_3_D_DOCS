using System.Threading.Tasks;

namespace RusalProject.Services.Pandoc;

public interface IPandocService
{
    Task<string> ConvertMarkdownToPdfAsync(string markdownContent, string templateContent, string documentTitle);
    Task<bool> ValidateTemplateAsync(string templateContent);
}
