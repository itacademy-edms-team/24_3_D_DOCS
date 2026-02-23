using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Services.Agent.Tools.DocumentTools;

namespace RusalProject.Services.Agent;

public class DocumentAgentToolExecutor
{
    private readonly IReadOnlyDictionary<string, IDocumentAgentTool> _tools;
    private readonly ILogger<DocumentAgentToolExecutor> _logger;

    public DocumentAgentToolExecutor(IEnumerable<IDocumentAgentTool> tools, ILogger<DocumentAgentToolExecutor> logger)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public (string TextBefore, string ToolCallBlock) SplitToolCall(string fullResponse)
    {
        var toolBlockStart = fullResponse.IndexOf("TOOL_CALL", StringComparison.OrdinalIgnoreCase);
        if (toolBlockStart < 0)
            return (fullResponse, string.Empty);

        var textBefore = fullResponse.Substring(0, toolBlockStart).TrimEnd();
        var toolBlock = fullResponse.Substring(toolBlockStart).Trim();
        return (textBefore, toolBlock);
    }

    public bool TryParseToolCall(string text, out string toolName, out Dictionary<string, object> args)
    {
        var parsed = ParseAllToolCalls(text);
        if (parsed.Count == 0)
        {
            toolName = string.Empty;
            args = new Dictionary<string, object>();
            return false;
        }
        toolName = parsed[0].ToolName;
        args = parsed[0].Args;
        return true;
    }

    /// <summary>
    /// Parses all TOOL_CALL blocks from response. Supports multiple tool calls in one response.
    /// </summary>
    public IReadOnlyList<(string ToolName, Dictionary<string, object> Args)> ParseAllToolCalls(string text)
    {
        var result = new List<(string ToolName, Dictionary<string, object> Args)>();
        var normalized = text
            .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\r\n", "\n")
            .Trim();

        var searchStart = 0;
        while (true)
        {
            var toolCallIndex = normalized.IndexOf("TOOL_CALL", searchStart, StringComparison.OrdinalIgnoreCase);
            if (toolCallIndex < 0)
                break;

            var working = normalized.Substring(toolCallIndex);
            var toolMatch = Regex.Match(working, @"tool:\s*([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);
            if (!toolMatch.Success)
                break;

            var name = toolMatch.Groups[1].Value.Trim();

            var argsIdx = working.IndexOf("args:", toolMatch.Index + toolMatch.Length, StringComparison.OrdinalIgnoreCase);
            if (argsIdx < 0)
                break;

            var argsRaw = working.Substring(argsIdx + "args:".Length).Trim();
            var argsJson = ExtractBalancedJson(argsRaw);
            if (string.IsNullOrWhiteSpace(argsJson))
                break;

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsJson);
                if (dict == null)
                    break;

                var args = new Dictionary<string, object>();
                foreach (var kv in dict)
                    args[kv.Key] = kv.Value;

                result.Add((name, args));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse TOOL_CALL args: {Args}", argsJson);
                break;
            }

            var nextCall = working.IndexOf("TOOL_CALL", argsIdx, StringComparison.OrdinalIgnoreCase);
            if (nextCall < 0)
                break;
            searchStart = toolCallIndex + nextCall;
        }

        return result;
    }

    public async Task<(DocumentToolResult Result, ToolCallDTO Dto)> ExecuteAsync(
        string toolName,
        Dictionary<string, object> args,
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            var err = $"Неизвестный инструмент: {toolName}";
            _logger.LogWarning(err);

            var unknownResult = new DocumentToolResult
            {
                ResultMessage = err
            };

            return (unknownResult, new ToolCallDTO
            {
                ToolName = toolName,
                Arguments = args,
                Result = err
            });
        }

        args["user_id"] = userId.ToString();
        args["document_id"] = documentId.ToString();

        var result = await tool.ExecuteAsync(args, cancellationToken);

        return (result, new ToolCallDTO
        {
            ToolName = toolName,
            Arguments = args,
            Result = result.ResultMessage
        });
    }

    public static string GetToolDescription(string toolName, Dictionary<string, object> args)
    {
        return toolName switch
        {
            "read_document" => "Чтение документа",
            "propose_document_changes" => "Предложение правок по сущностям",
            _ => $"Вызов {toolName}"
        };
    }

    private static string ExtractBalancedJson(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        var firstBrace = source.IndexOf('{');
        if (firstBrace < 0)
            return string.Empty;

        var sb = new StringBuilder();
        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = firstBrace; i < source.Length; i++)
        {
            var ch = source[i];
            sb.Append(ch);

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{')
            {
                depth++;
                continue;
            }

            if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return sb.ToString();
                }
            }
        }

        return string.Empty;
    }
}
