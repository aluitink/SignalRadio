using SignalRadio.Api.Dtos;
using SignalRadio.DataAccess;

namespace SignalRadio.Api.Extensions;

public static class EntityExtensions
{
    public static CallDto ToDto(this Call call, string? apiBaseUrl = null)
    {
        apiBaseUrl ??= "/api"; // Default fallback

        return new CallDto
        {
            Id = call.Id,
            TalkGroupId = call.TalkGroupId,
            TalkGroup = call.TalkGroup?.ToDto(),
            RecordingTime = call.RecordingTime,
            FrequencyHz = call.FrequencyHz,
            DurationSeconds = call.DurationSeconds,
            CreatedAt = call.CreatedAt,
            Recordings = call.Recordings?.Select(r => r.ToDto(apiBaseUrl)).ToList() ?? new(),
            Transcriptions = call.Recordings?.SelectMany(r => r.Transcriptions ?? new List<Transcription>())
                .Select(t => t.ToDto()).ToList()
        };
    }

    public static TalkGroupDto ToDto(this TalkGroup talkGroup)
    {
        return new TalkGroupDto
        {
            Id = talkGroup.Id,
            Number = talkGroup.Number,
            Name = talkGroup.Name,
            AlphaTag = talkGroup.AlphaTag,
            Description = talkGroup.Description,
            Tag = talkGroup.Tag,
            Category = talkGroup.Category,
            Priority = talkGroup.Priority
        };
    }

    public static RecordingDto ToDto(this Recording recording, string apiBaseUrl)
    {
        return new RecordingDto
        {
            Id = recording.Id,
            FileName = recording.FileName,
            Url = $"{apiBaseUrl}/recordings/{recording.Id}/file",
            DurationSeconds = 0, // Will need to be calculated from file metadata
            SizeBytes = recording.SizeBytes
        };
    }

    public static TranscriptionDto ToDto(this Transcription transcription)
    {
        return new TranscriptionDto
        {
            Id = transcription.Id,
            Text = transcription.FullText,
            Confidence = transcription.Confidence,
            Language = transcription.Language
        };
    }
}
