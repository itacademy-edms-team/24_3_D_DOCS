using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Agent;

public class AgentLogService : IAgentLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AgentLogService> _logger;

    public AgentLogService(ApplicationDbContext context, ILogger<AgentLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogUserMessageAsync(Guid documentId, Guid userId, Guid? chatSessionId, string message, int iterationNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new AgentLog
            {
                DocumentId = documentId,
                UserId = userId,
                ChatSessionId = chatSessionId,
                LogType = "user_message",
                Content = message,
                IterationNumber = iterationNumber,
                Timestamp = DateTime.UtcNow
            };

            _context.AgentLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user message. DocumentId={DocumentId}, UserId={UserId}", documentId, userId);
        }
    }

    public async Task LogLlmRequestAsync(Guid documentId, Guid userId, Guid? chatSessionId, string prompt, string? systemPrompt, int iterationNumber, int? stepNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                systemPrompt = systemPrompt,
                promptLength = prompt?.Length ?? 0,
                systemPromptLength = systemPrompt?.Length ?? 0
            });

            var log = new AgentLog
            {
                DocumentId = documentId,
                UserId = userId,
                ChatSessionId = chatSessionId,
                LogType = "llm_request",
                Content = prompt ?? string.Empty,
                Metadata = metadata,
                IterationNumber = iterationNumber,
                StepNumber = stepNumber,
                Timestamp = DateTime.UtcNow
            };

            _context.AgentLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log LLM request. DocumentId={DocumentId}, UserId={UserId}", documentId, userId);
        }
    }

    public async Task LogLlmResponseAsync(Guid documentId, Guid userId, Guid? chatSessionId, string response, int iterationNumber, int? stepNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new AgentLog
            {
                DocumentId = documentId,
                UserId = userId,
                ChatSessionId = chatSessionId,
                LogType = "llm_response",
                Content = response ?? string.Empty,
                IterationNumber = iterationNumber,
                StepNumber = stepNumber,
                Timestamp = DateTime.UtcNow
            };

            _context.AgentLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log LLM response. DocumentId={DocumentId}, UserId={UserId}", documentId, userId);
        }
    }

    public async Task LogToolCallAsync(Guid documentId, Guid userId, Guid? chatSessionId, string toolName, Dictionary<string, object> arguments, int iterationNumber, int? stepNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                toolName = toolName,
                arguments = arguments
            });

            var log = new AgentLog
            {
                DocumentId = documentId,
                UserId = userId,
                ChatSessionId = chatSessionId,
                LogType = "tool_call",
                Content = toolName,
                Metadata = metadata,
                IterationNumber = iterationNumber,
                StepNumber = stepNumber,
                Timestamp = DateTime.UtcNow
            };

            _context.AgentLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log tool call. DocumentId={DocumentId}, UserId={UserId}, ToolName={ToolName}", documentId, userId, toolName);
        }
    }

    public async Task LogToolResultAsync(Guid documentId, Guid userId, Guid? chatSessionId, string toolName, string result, int iterationNumber, int? stepNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                toolName = toolName,
                resultLength = result?.Length ?? 0
            });

            var log = new AgentLog
            {
                DocumentId = documentId,
                UserId = userId,
                ChatSessionId = chatSessionId,
                LogType = "tool_result",
                Content = result ?? string.Empty,
                Metadata = metadata,
                IterationNumber = iterationNumber,
                StepNumber = stepNumber,
                Timestamp = DateTime.UtcNow
            };

            _context.AgentLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log tool result. DocumentId={DocumentId}, UserId={UserId}, ToolName={ToolName}", documentId, userId, toolName);
        }
    }

    public async Task LogDocumentChangeAsync(Guid documentId, Guid userId, Guid? chatSessionId, string toolName, string beforeContent, string afterContent, int iterationNumber, int? stepNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                toolName = toolName,
                beforeLength = beforeContent?.Length ?? 0,
                afterLength = afterContent?.Length ?? 0,
                changeSize = (afterContent?.Length ?? 0) - (beforeContent?.Length ?? 0)
            });

            var content = $"Tool: {toolName}\nBefore length: {beforeContent?.Length ?? 0}\nAfter length: {afterContent?.Length ?? 0}";

            var log = new AgentLog
            {
                DocumentId = documentId,
                UserId = userId,
                ChatSessionId = chatSessionId,
                LogType = "document_change",
                Content = content,
                Metadata = metadata,
                IterationNumber = iterationNumber,
                StepNumber = stepNumber,
                Timestamp = DateTime.UtcNow
            };

            _context.AgentLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log document change. DocumentId={DocumentId}, UserId={UserId}, ToolName={ToolName}", documentId, userId, toolName);
        }
    }

    public async Task LogStatusCheckAsync(Guid documentId, Guid userId, Guid? chatSessionId, string verdict, string reason, int iterationNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                verdict = verdict,
                reason = reason
            });

            var log = new AgentLog
            {
                DocumentId = documentId,
                UserId = userId,
                ChatSessionId = chatSessionId,
                LogType = "status_check",
                Content = $"{verdict}: {reason}",
                Metadata = metadata,
                IterationNumber = iterationNumber,
                Timestamp = DateTime.UtcNow
            };

            _context.AgentLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log status check. DocumentId={DocumentId}, UserId={UserId}", documentId, userId);
        }
    }

    public async Task LogCompletionAsync(Guid documentId, Guid userId, Guid? chatSessionId, string reason, int documentChangeCount, int iterationNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                reason = reason,
                documentChangeCount = documentChangeCount,
                iterationNumber = iterationNumber
            });

            var log = new AgentLog
            {
                DocumentId = documentId,
                UserId = userId,
                ChatSessionId = chatSessionId,
                LogType = "completion",
                Content = $"Completed: {reason}. Changes: {documentChangeCount}",
                Metadata = metadata,
                IterationNumber = iterationNumber,
                Timestamp = DateTime.UtcNow
            };

            _context.AgentLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log completion. DocumentId={DocumentId}, UserId={UserId}", documentId, userId);
        }
    }

    public async Task<List<AgentLogDTO>> GetLogsAsync(Guid documentId, Guid userId, Guid? chatSessionId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.AgentLogs
                .Where(l => l.DocumentId == documentId && l.UserId == userId);

            if (chatSessionId.HasValue)
            {
                query = query.Where(l => l.ChatSessionId == chatSessionId);
            }

            var logs = await query
                .OrderBy(l => l.Timestamp)
                .Select(l => new AgentLogDTO
                {
                    Id = l.Id,
                    DocumentId = l.DocumentId,
                    UserId = l.UserId,
                    ChatSessionId = l.ChatSessionId,
                    LogType = l.LogType,
                    Content = l.Content,
                    Metadata = l.Metadata,
                    IterationNumber = l.IterationNumber,
                    StepNumber = l.StepNumber,
                    Timestamp = l.Timestamp
                })
                .ToListAsync(cancellationToken);

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs. DocumentId={DocumentId}, UserId={UserId}", documentId, userId);
            return new List<AgentLogDTO>();
        }
    }
}
