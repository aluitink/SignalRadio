namespace SignalRadio.Core.Dtos;

public record TalkGroupDto(
    int Id,
    string Number,
    string? Name,
    string? AlphaTag,
    string? Tag,
    string? Description,
    string? Category,
    int? Priority,
    DateTime CreatedAt);

public record CreateTalkGroupRequest(
    string Number,
    string? Name,
    string? AlphaTag,
    string? Tag,
    string? Description,
    string? Category,
    int? Priority);

public record UpdateTalkGroupRequest(
    string? Name,
    string? AlphaTag,
    string? Tag,
    string? Description,
    string? Category,
    int? Priority);
