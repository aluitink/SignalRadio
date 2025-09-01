using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public class CallsService : ICallsService
{
    private readonly SignalRadioDbContext _db;

    public CallsService(SignalRadioDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Call>> GetAllAsync(int page, int pageSize, string? sortBy = null, string? sortDir = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "recordingTime" : sortBy.Trim().ToLowerInvariant();
        sortDir = string.IsNullOrWhiteSpace(sortDir) ? "desc" : sortDir.Trim().ToLowerInvariant();

        var q = _db.Calls
            .Include(c => c.Recordings)
                .ThenInclude(r => r.Transcriptions)
            .Include(c => c.TalkGroup)
            .AsNoTracking();

        // Apply supported sorting options. Default: recordingTime desc
        bool ascending = sortDir == "asc" || sortDir == "ascending";
        switch (sortBy)
        {
            case "createdat":
            case "created_at":
            case "created":
                q = ascending ? q.OrderBy(c => c.CreatedAt) : q.OrderByDescending(c => c.CreatedAt);
                break;
            case "talkgroupid":
            case "talkgroup":
            case "talk_group":
                q = ascending ? q.OrderBy(c => c.TalkGroupId) : q.OrderByDescending(c => c.TalkGroupId);
                break;
            case "recordingtime":
            case "recording_time":
            default:
                q = ascending ? q.OrderBy(c => c.RecordingTime) : q.OrderByDescending(c => c.RecordingTime);
                break;
        }

        var query = q;
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Call>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<Call?> GetByIdAsync(int id)
    {
        return await _db.Calls
            .Include(c => c.Recordings)
                .ThenInclude(r => r.Transcriptions)
            .Include(c => c.TalkGroup)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Call> CreateAsync(Call model)
    {
        _db.Calls.Add(model);
        await _db.SaveChangesAsync();
        return model;
    }

    public async Task<bool> UpdateAsync(int id, Call model)
    {
        if (id != model.Id) return false;
        var exists = await _db.Calls.AnyAsync(c => c.Id == id);
        if (!exists) return false;
        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.Calls.FindAsync(id);
        if (item == null) return false;
        _db.Calls.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
