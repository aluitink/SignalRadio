namespace SignalRadio.Api.Dtos;

public class RecordingDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public long SizeBytes { get; set; }
}

public class TranscriptionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public string? Language { get; set; }
}

public class TalkGroupDto
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string? Name { get; set; }
    public string? AlphaTag { get; set; }
    public string? Description { get; set; }
    public string? Tag { get; set; }
    public string? Category { get; set; }
    public int? Priority { get; set; }
}

public class CallDto
{
    public int Id { get; set; }
    public int TalkGroupId { get; set; }
    public TalkGroupDto? TalkGroup { get; set; }
    public DateTimeOffset RecordingTime { get; set; }
    public double FrequencyHz { get; set; }
    public int DurationSeconds { get; set; }
    public List<RecordingDto> Recordings { get; set; } = new();
    public List<TranscriptionDto>? Transcriptions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
