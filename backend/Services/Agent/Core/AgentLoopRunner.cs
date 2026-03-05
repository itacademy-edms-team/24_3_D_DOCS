using System.Text.Json;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent.Core;

public sealed class AgentLoopRunner
{
    private readonly IOllamaChatService _ollamaChatService;
    private readonly ILogger<AgentLoopRunner> _logger;

    public AgentLoopRunner(IOllamaChatService ollamaChatService, ILogger<AgentLoopRunner> logger)
    {
        _ollamaChatService = ollamaChatService;
        _logger = logger;
    }

    public async Task<AgentLoopResult> RunAsync(
        AgentExecutionContext context,
        string systemPrompt,
        IReadOnlyList<IAgentTool> tools,
        List<OllamaMessageInput> messages,
        int maxIterations,
        Func<AgentStepDTO, Task>? onStepUpdate = null,
        Func<string, Task>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        var toolMap = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        var toolDefinitions = tools.Select(t => new OllamaToolDefinition
        {
            Name = t.Name,
            Description = t.Description,
            Parameters = t.ParametersSchema
        }).ToList();

        var steps = new List<AgentStepDTO>();
        var stepNumber = 0;

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var response = await _ollamaChatService.CompleteAsync(
                context.UserId,
                messages,
                systemPrompt,
                toolDefinitions,
                onChunk,
                cancellationToken);

            messages.Add(response.ToAssistantMessage());

            if (response.ToolCalls.Count == 0)
            {
                return new AgentLoopResult
                {
                    FinalMessage = response.Content.Trim(),
                    Steps = steps
                };
            }

            foreach (var toolCall in response.ToolCalls)
            {
                if (string.IsNullOrWhiteSpace(toolCall.Name))
                {
                    _logger.LogWarning("Model returned a tool call without a name. Available tools: {ToolNames}",
                        string.Join(", ", toolMap.Keys));
                    throw new InvalidOperationException("Модель вернула некорректный вызов инструмента без имени.");
                }

                if (!toolMap.TryGetValue(toolCall.Name, out var tool))
                {
                    _logger.LogWarning("Model returned unknown tool: {ToolName}. Available tools: {ToolNames}",
                        toolCall.Name,
                        string.Join(", ", toolMap.Keys));
                    throw new InvalidOperationException($"Модель вернула неизвестный инструмент: {toolCall.Name}");
                }

                stepNumber++;
                var result = await tool.ExecuteAsync(toolCall.Arguments, context, cancellationToken);

                var step = new AgentStepDTO
                {
                    StepNumber = stepNumber,
                    Description = tool.Description,
                    ToolCalls = new List<ToolCallDTO>
                    {
                        new()
                        {
                            ToolName = toolCall.Name,
                            Arguments = AgentSerialization.ToDictionary(toolCall.Arguments),
                            Result = result.ResultMessage
                        }
                    },
                    ToolResult = result.ResultMessage,
                    DocumentChanges = result.DocumentChanges.Count > 0 ? result.DocumentChanges : null
                };

                steps.Add(step);
                await (onStepUpdate?.Invoke(step) ?? Task.CompletedTask);

                messages.Add(new OllamaMessageInput
                {
                    Role = "tool",
                    ToolName = toolCall.Name,
                    Content = result.ResultMessage
                });

                if (result.Delegation != null)
                {
                    return new AgentLoopResult
                    {
                        FinalMessage = string.Empty,
                        Steps = steps,
                        Delegation = result.Delegation
                    };
                }
            }
        }

        _logger.LogWarning("Agent loop reached max iterations: {MaxIterations}", maxIterations);
        return new AgentLoopResult
        {
            FinalMessage = "Агент не смог завершить задачу за допустимое число шагов.",
            Steps = steps
        };
    }
}
