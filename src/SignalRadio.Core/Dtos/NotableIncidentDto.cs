namespace SignalRadio.Core.Dtos;

public record NotableIncidentDto(
    long Id,
    string Description,
    double? ImportanceScore,
    DateTime CreatedAt);
