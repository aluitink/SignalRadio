using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data.Entities;

namespace SignalRadio.Data.Services;

public class RecordingsService(SignalRadioDbContext db, IStorageService storage) : IRecordingsService
{
    public async Task<RecordingDto> UploadAsync(long callId, string fileName, Stream content, long sizeBytes, CancellationToken ct = default)
    {
        var storageLocation = await db.StorageLocations.FirstOrDefaultAsync(s => s.Kind == StorageKind.Local, ct)
            ?? throw new InvalidOperationException("No local storage location configured.");

        var storedFileName = await storage.SaveAsync(fileName, content, ct);

        var entity = new Recording
        {
            CallId = callId,
            StorageLocationId = storageLocation.Id,
            FileName = storedFileName,
            SizeBytes = sizeBytes,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsProcessed = false
        };
        db.Recordings.Add(entity);
        await db.SaveChangesAsync(ct);

        return new RecordingDto(entity.Id, entity.CallId, entity.StorageLocationId, entity.FileName, entity.SizeBytes, entity.ReceivedAt, entity.IsProcessed, storage.GetUrl(storedFileName));
    }
    public async Task<RecordingDto> CreateWithFileAsync(Stream content, string fileName, RecordingUploadRequest request, CancellationToken ct = default)
    {
        // Upsert talk group by number
        var talkGroup = await db.TalkGroups.FirstOrDefaultAsync(t => t.Number == request.TalkgroupId, ct);
        if (talkGroup == null)
        {
            talkGroup = new TalkGroup { Number = request.TalkgroupId, Name = request.SystemName };
            db.TalkGroups.Add(talkGroup);
            await db.SaveChangesAsync(ct);
        }

        // Create the call
        var call = new Call
        {
            TalkGroupId = talkGroup.Id,
            RecordingTime = new DateTimeOffset(request.Timestamp, TimeSpan.Zero),
            FrequencyHz = double.TryParse(request.Frequency, out var freq) ? freq : 0,
            DurationSeconds = request.Duration ?? 0
        };
        db.Calls.Add(call);
        await db.SaveChangesAsync(ct);

        // Save the audio file and create the recording
        var storageLocation = await db.StorageLocations.FirstOrDefaultAsync(s => s.Kind == StorageKind.Local, ct)
            ?? throw new InvalidOperationException("No local storage location configured.");

        var storedFileName = await storage.SaveAsync(fileName, content, ct);

        var entity = new Recording
        {
            CallId = call.Id,
            StorageLocationId = storageLocation.Id,
            FileName = storedFileName,
            SizeBytes = content.Length,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsProcessed = false
        };
        db.Recordings.Add(entity);
        await db.SaveChangesAsync(ct);

        return new RecordingDto(entity.Id, entity.CallId, entity.StorageLocationId, entity.FileName, entity.SizeBytes, entity.ReceivedAt, entity.IsProcessed, storage.GetUrl(storedFileName));
    }

    public async Task<RecordingDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var rec = await db.Recordings.FindAsync([id], ct);
        if (rec == null) return null;
        return new RecordingDto(rec.Id, rec.CallId, rec.StorageLocationId, rec.FileName, rec.SizeBytes, rec.ReceivedAt, rec.IsProcessed, storage.GetUrl(rec.FileName));
    }

    public async Task<Stream?> GetFileAsync(long id, CancellationToken ct = default)
    {
        var rec = await db.Recordings.FindAsync([id], ct);
        if (rec == null) return null;
        return await storage.OpenAsync(rec.FileName, ct);
    }

    public Task<Stream?> GetFileByFileNameAsync(string fileName, CancellationToken ct = default)
    {
        return storage.OpenAsync(fileName, ct);
    }

    public async Task<IReadOnlyList<RecordingDto>> GetNeedingTranscriptionAsync(int limit, double minDurationSeconds, int lookbackHours, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-lookbackHours);
        return await db.Recordings
            .Include(r => r.Call)
            .Where(r => !r.IsProcessed && r.Call!.DurationSeconds >= minDurationSeconds && r.ReceivedAt >= cutoff)
            .OrderBy(r => r.ReceivedAt)
            .Take(limit)
            .Select(r => new RecordingDto(r.Id, r.CallId, r.StorageLocationId, r.FileName, r.SizeBytes, r.ReceivedAt, r.IsProcessed, null))
            .ToListAsync(ct);
    }

    public async Task MarkProcessedAsync(long id, CancellationToken ct = default)
    {
        var rec = await db.Recordings.FindAsync([id], ct);
        if (rec == null) return;
        rec.IsProcessed = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var rec = await db.Recordings.FindAsync([id], ct);
        if (rec == null) return false;
        await storage.DeleteAsync(rec.FileName, ct);
        db.Recordings.Remove(rec);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
