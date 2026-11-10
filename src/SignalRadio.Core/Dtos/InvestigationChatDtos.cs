namespace SignalRadio.Core.Dtos;

/// <summary>A single persisted message in an investigation's chat.</summary>
public record InvestigationChatMessageDto(
    long Id,
    int Index,
    string Role,
    string? Text,
    string? ToolName,
    string? ToolArgs,
    string? ToolResult,
    string? ToolCallId,
    string? Error,
    DateTime CreatedAt);
