using System.Text.Json;
using System.Text.RegularExpressions;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Services.Agent.Tools;

namespace RusalProject.Services.Agent;

/// <summary>
/// Parses TOOL_CALL blocks from model output and executes CRUDdocTools.
/// </summary>
public class MainAgentToolExecutor
{
    private readonly IReadOnlyDictionary<string, ITool> _tools;
    private readonly ILogger<MainAgentToolExecutor> _logger;

    public MainAgentToolExecutor(IEnumerable<ITool> tools, ILogger<MainAgentToolExecutor> logger)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    /// <summary>
    /// Tries to extract a TOOL_CALL block from text.
    /// Supports: TOOL_CALL\ntool: name\nargs: {} and optional ``` code fence.
    /// </summary>
    public bool TryParseToolCall(string text, out string toolName, out Dictionary<string, object> args)
    {
        toolName = string.Empty;
        args = new Dictionary<string, object>();

        var normalized = text
            .Replace("```", string.Empty)
            .Replace("\r\n", "\n")
            .Trim();

        // Match TOOL_CALL block - args can be {} or simple JSON (one level)
        var match = Regex.Match(
            normalized,
            @"TOOL_CALL\s+tool:\s*(\w+)\s+args:\s*(\{[^{}]*\}|\{\s*\})",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (!match.Success)
            return false;

        toolName = match.Groups[1].Value.Trim();
        var argsStr = match.Groups[2].Value.Trim();

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsStr);
            if (dict != null)
            {
                foreach (var kv in dict)
                    args[kv.Key] = kv.Value;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse TOOL_CALL args: {Args}", argsStr);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Splits response into text before TOOL_CALL and the TOOL_CALL block.
    /// </summary>
    public (string TextBefore, string ToolCallBlock) SplitToolCall(string fullResponse)
    {
        var toolBlockStart = fullResponse.IndexOf("TOOL_CALL", StringComparison.OrdinalIgnoreCase);
        if (toolBlockStart < 0)
            return (fullResponse, string.Empty);

        var textBefore = fullResponse.Substring(0, toolBlockStart).TrimEnd();
        var toolBlock = fullResponse.Substring(toolBlockStart).Trim();
        return (textBefore, toolBlock);
    }

    /// <summary>
    /// Executes the tool with user_id injected.
    /// </summary>
    public async Task<(string Result, ToolCallDTO Dto)> ExecuteAsync(
        string toolName,
        Dictionary<string, object> args,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            var err = $"Неизвестный инструмент: {toolName}";
            _logger.LogWarning(err);
            return (err, new ToolCallDTO
            {
                ToolName = toolName,
                Arguments = args,
                Result = err
            });
        }

        args["user_id"] = userId.ToString();
        var result = await tool.ExecuteAsync(args, cancellationToken);

        return (result, new ToolCallDTO
        {
            ToolName = toolName,
            Arguments = args,
            Result = result
        });
    }

    /// <summary>
    /// Human-readable description for a tool call.
    /// </summary>
    public static string GetToolDescription(string toolName, Dictionary<string, object> args)
    {
        return toolName switch
        {
            "list_documents" => "Получение списка документов",
            "create_document" => "Создание документа",
            "delete_document" => "Удаление документа",
            _ => $"Вызов {toolName}"
        };
    }
}
