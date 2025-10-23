using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public class TranscriptionsService : ITranscriptionsService
{
    private readonly SignalRadioDbContext _db;

    public TranscriptionsService(SignalRadioDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Transcription>> GetAllAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.Transcriptions.Include(t => t.Recording).AsNoTracking().OrderByDescending(t => t.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Transcription>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<Transcription?> GetByIdAsync(int id)
    {
        return await _db.Transcriptions.Include(t => t.Recording).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transcription> CreateAsync(Transcription model)
    {
        _db.Transcriptions.Add(model);
        await _db.SaveChangesAsync();
        return model;
    }

    public async Task<bool> UpdateAsync(int id, Transcription model)
    {
        if (id != model.Id) return false;
        var exists = await _db.Transcriptions.AnyAsync(t => t.Id == id);
        if (!exists) return false;
        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.Transcriptions.FindAsync(id);
        if (item == null) return false;
        _db.Transcriptions.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResult<Transcription>> SearchAsync(string q, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var param = new SqlParameter("@p0", q);

        var query = _db.Transcriptions
            .FromSqlRaw("SELECT * FROM [Transcriptions] WHERE CONTAINS([FullText], @p0)", param)
            .Include(t => t.Recording)
                .ThenInclude(r => r!.Call)
                    .ThenInclude(c => c!.TalkGroup)
            .AsNoTracking();

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Transcription>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<PagedResult<Call>> SearchCallsAsync(string q, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var param = new SqlParameter("@p0", q);

        // First, get transcriptions that match the search
        var transcriptionQuery = _db.Transcriptions
            .FromSqlRaw("SELECT * FROM [Transcriptions] WHERE CONTAINS([FullText], @p0)", param)
            .Include(t => t.Recording)
                .ThenInclude(r => r!.Call)
                    .ThenInclude(c => c!.TalkGroup)
            .AsNoTracking();

        // Get unique call IDs from the transcription search results
        var callIds = await transcriptionQuery
            .Where(t => t.Recording != null && t.Recording.Call != null)
            .Select(t => t.Recording!.CallId)
            .Distinct()
            .ToListAsync();

        // Now get the calls with their full data
        var callQuery = _db.Calls
            .Where(c => callIds.Contains(c.Id))
            .Include(c => c.TalkGroup)
            .Include(c => c.Recordings)
                .ThenInclude(r => r.Transcriptions)
            .AsNoTracking()
            .OrderByDescending(c => c.RecordingTime);

        var total = await callQuery.CountAsync();
        var calls = await callQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Call>
        {
            Items = calls,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<List<Transcription>> GetByTalkGroupAndTimeRangeAsync(int talkGroupId, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return await _db.Transcriptions
            .Include(t => t.Recording)
                .ThenInclude(r => r!.Call)
            .AsNoTracking()
            .Where(t => t.Recording != null &&
                       t.Recording.Call != null &&
                       t.Recording.Call.TalkGroupId == talkGroupId &&
                       t.CreatedAt >= startTime &&
                       t.CreatedAt <= endTime &&
                       !string.IsNullOrWhiteSpace(t.FullText))
            .OrderByDescending(t => t.Recording!.Call!.DurationSeconds)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();
    }
}
