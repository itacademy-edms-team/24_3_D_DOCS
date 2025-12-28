using RusalProject.Models.Types;

namespace RusalProject.Services.Markdown;

public interface IMarkdownParserService
{
    /// <summary>
    /// Парсит Markdown документ в список блоков с нормализованным текстом
    /// </summary>
    List<ParsedBlock> ParseDocument(string markdown);
}
