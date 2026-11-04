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
        return new TranscriptionDto(entity.Id, entity.RecordingId, entity.Service, entity.Language, entity.FullText, entity.Confidence, entity.AdditionalDataJson, entity.CreatedAt, entity.IsFinal);
    }

    public async Task<IReadOnlyList<TranscriptionDto>> GetByRecordingIdAsync(long recordingId, CancellationToken ct = default)
    {
        return await db.Transcriptions
            .Where(t => t.RecordingId == recordingId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TranscriptionDto(t.Id, t.RecordingId, t.Service, t.Language, t.FullText, t.Confidence, t.AdditionalDataJson, t.CreatedAt, t.IsFinal))
            .ToListAsync(ct);
    }

    public async Task<TranscriptionDto?> GetFinalByRecordingIdAsync(long recordingId, CancellationToken ct = default)
    {
        var t = await db.Transcriptions
            .Where(t => t.RecordingId == recordingId && t.IsFinal)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
        return t == null ? null : new TranscriptionDto(t.Id, t.RecordingId, t.Service, t.Language, t.FullText, t.Confidence, t.AdditionalDataJson, t.CreatedAt, t.IsFinal);
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
