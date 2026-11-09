using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data.Entities;

namespace SignalRadio.Data.Services;

public class TranscriptionsService(SignalRadioDbContext db) : ITranscriptionsService
{
    public async Task<TranscriptionDto> CreateAsync(long recordingId, string service, string fullText, string? language, double? confidence, string? additionalDataJson, bool isFinal, CancellationToken ct = default)
    {
        var entity = new Transcription
        {
            RecordingId = recordingId,
            Service = service,
            Language = language,
            FullText = fullText,
            Confidence = confidence,
            AdditionalDataJson = additionalDataJson,
            CreatedAt = DateTime.UtcNow,
            IsFinal = isFinal
        };
        db.Transcriptions.Add(entity);
        await db.SaveChangesAsync(ct);
        var duration = entity.Recording?.Call?.DurationSeconds ?? 0.0;
        var callId = entity.Recording?.CallId ?? 0;
        return new TranscriptionDto(entity.Id, entity.RecordingId, callId, entity.Service, entity.Language, entity.FullText, entity.Confidence, entity.AdditionalDataJson, entity.CreatedAt, entity.IsFinal, duration);
    }

    public async Task<IReadOnlyList<TranscriptionDto>> GetByRecordingIdAsync(long recordingId, CancellationToken ct = default)
    {
        return await db.Transcriptions
            .Where(t => t.RecordingId == recordingId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TranscriptionDto(t.Id, t.RecordingId, t.Recording!.CallId, t.Service, t.Language, t.FullText, t.Confidence, t.AdditionalDataJson, t.CreatedAt, t.IsFinal, t.Recording.Call!.DurationSeconds))
            .ToListAsync(ct);
    }

    public async Task<TranscriptionDto?> GetFinalByRecordingIdAsync(long recordingId, CancellationToken ct = default)
    {
        var t = await db.Transcriptions
            .Where(t => t.RecordingId == recordingId && t.IsFinal)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (t == null) return null;
        var duration = t.Recording?.Call?.DurationSeconds ?? 0.0;
        var callId = t.Recording?.CallId ?? 0;
        return new TranscriptionDto(t.Id, t.RecordingId, callId, t.Service, t.Language, t.FullText, t.Confidence, t.AdditionalDataJson, t.CreatedAt, t.IsFinal, duration);
    }

    public async Task<IReadOnlyList<TranscriptionDto>> GetFinalByTalkGroupSinceAsync(int talkGroupId, DateTime since, int limit, CancellationToken ct = default)
    {
        var sinceOffset = new DateTimeOffset(since, TimeSpan.Zero);
        return await db.Transcriptions
            .AsNoTracking()
            .Where(t => t.IsFinal)
            .Where(t => t.Recording.Call.TalkGroupId == talkGroupId)
            .Where(t => t.Recording.Call.RecordingTime > sinceOffset)
            .OrderBy(t => t.Recording.Call.RecordingTime)
            .Take(limit)
            .Select(t => new TranscriptionDto(t.Id, t.RecordingId, t.Recording!.CallId, t.Service, t.Language, t.FullText, t.Confidence, t.AdditionalDataJson, t.CreatedAt, t.IsFinal, t.Recording.Call!.DurationSeconds))
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.Transcriptions.FindAsync([id], ct);
        if (entity == null) return false;
        db.Transcriptions.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
