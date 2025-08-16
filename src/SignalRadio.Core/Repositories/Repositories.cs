using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Data;
using SignalRadio.Core.Models;

namespace SignalRadio.Core.Repositories;

public class CallRepository : ICallRepository
{
    private readonly SignalRadioDbContext _context;

    public CallRepository(SignalRadioDbContext context)
    {
        _context = context;
    }

    public async Task<Call?> GetByIdAsync(int id)
    {
        return await _context.Calls
            .Include(c => c.Recordings)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Call?> GetByDetailsAsync(string talkgroupId, string systemName, DateTime recordingTime)
    {
        return await _context.Calls
            .Include(c => c.Recordings)
            .FirstOrDefaultAsync(c => c.TalkgroupId == talkgroupId 
                && c.SystemName == systemName 
                && c.RecordingTime == recordingTime);
    }

    public async Task<IEnumerable<Call>> GetByTalkgroupAsync(string talkgroupId, int? limit = null)
    {
        var query = _context.Calls
            .Include(c => c.Recordings)
            .Where(c => c.TalkgroupId == talkgroupId)
            .OrderByDescending(c => c.RecordingTime);

        if (limit.HasValue)
        {
            return await query.Take(limit.Value).ToListAsync();
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Call>> GetBySystemAsync(string systemName, int? limit = null)
    {
        var query = _context.Calls
            .Include(c => c.Recordings)
            .Where(c => c.SystemName == systemName)
            .OrderByDescending(c => c.RecordingTime);

        if (limit.HasValue)
        {
            return await query.Take(limit.Value).ToListAsync();
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Call>> GetRecentCallsAsync(int limit = 50)
    {
        return await _context.Calls
            .Include(c => c.Recordings)
            .OrderByDescending(c => c.RecordingTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Call>> GetCallsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Calls
            .Include(c => c.Recordings)
            .Where(c => c.RecordingTime >= startDate && c.RecordingTime <= endDate)
            .OrderByDescending(c => c.RecordingTime)
            .ToListAsync();
    }

    public async Task<Call> CreateAsync(Call call)
    {
        _context.Calls.Add(call);
        await _context.SaveChangesAsync();
        return call;
    }

    public async Task<Call> UpdateAsync(Call call)
    {
        _context.Entry(call).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return call;
    }

    public async Task DeleteAsync(int id)
    {
        var call = await _context.Calls.FindAsync(id);
        if (call != null)
        {
            _context.Calls.Remove(call);
            await _context.SaveChangesAsync();
        }
    }
}

public class RecordingRepository : IRecordingRepository
{
    private readonly SignalRadioDbContext _context;

    public RecordingRepository(SignalRadioDbContext context)
    {
        _context = context;
    }

    public async Task<Recording?> GetByIdAsync(int id)
    {
        return await _context.Recordings
            .Include(r => r.Call)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Recording>> GetByCallIdAsync(int callId)
    {
        return await _context.Recordings
            .Where(r => r.CallId == callId)
            .OrderBy(r => r.Format)
            .ToListAsync();
    }

    public async Task<IEnumerable<Recording>> GetByFormatAsync(string format)
    {
        return await _context.Recordings
            .Include(r => r.Call)
            .Where(r => r.Format == format)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Recording>> GetUnuploadedAsync()
    {
        return await _context.Recordings
            .Include(r => r.Call)
            .Where(r => !r.IsUploaded)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Recording> CreateAsync(Recording recording)
    {
        _context.Recordings.Add(recording);
        await _context.SaveChangesAsync();
        return recording;
    }

    public async Task<Recording> UpdateAsync(Recording recording)
    {
        _context.Entry(recording).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return recording;
    }

    public async Task DeleteAsync(int id)
    {
        var recording = await _context.Recordings.FindAsync(id);
        if (recording != null)
        {
            _context.Recordings.Remove(recording);
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAsUploadedAsync(int id, string blobUri, string blobName)
    {
        var recording = await _context.Recordings.FindAsync(id);
        if (recording != null)
        {
            recording.IsUploaded = true;
            recording.UploadedAt = DateTime.UtcNow;
            recording.BlobUri = blobUri;
            recording.BlobName = blobName;
            recording.UpdatedAt = DateTime.UtcNow;
            recording.UploadAttempts = recording.UploadAttempts + 1;
            recording.LastUploadError = null; // Clear any previous error
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkUploadFailedAsync(int id, string errorMessage)
    {
        var recording = await _context.Recordings.FindAsync(id);
        if (recording != null)
        {
            recording.UploadAttempts = recording.UploadAttempts + 1;
            recording.LastUploadError = errorMessage;
            recording.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateAudioMetadataAsync(int id, TimeSpan? duration, int? sampleRate, int? bitrate, byte? channels, string? quality, string? fileHash)
    {
        var recording = await _context.Recordings.FindAsync(id);
        if (recording != null)
        {
            recording.Duration = duration;
            recording.SampleRate = sampleRate;
            recording.Bitrate = bitrate;
            recording.Channels = channels;
            recording.Quality = quality;
            recording.FileHash = fileHash;
            recording.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Recording>> GetDuplicatesByHashAsync(string fileHash)
    {
        return await _context.Recordings
            .Include(r => r.Call)
            .Where(r => r.FileHash == fileHash)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Recording>> GetFailedUploadsAsync(int maxAttempts = 3)
    {
        return await _context.Recordings
            .Include(r => r.Call)
            .Where(r => !r.IsUploaded && r.UploadAttempts >= maxAttempts)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetRecordingStatsByFormatAsync()
    {
        return await _context.Recordings
            .GroupBy(r => r.Format)
            .Select(g => new { Format = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Format, x => x.Count);
    }

    public async Task<Dictionary<string, long>> GetStorageStatsByFormatAsync()
    {
        return await _context.Recordings
            .Where(r => r.IsUploaded)
            .GroupBy(r => r.Format)
            .Select(g => new { Format = g.Key, TotalSize = g.Sum(r => r.FileSize) })
            .ToDictionaryAsync(x => x.Format, x => x.TotalSize);
    }
}
