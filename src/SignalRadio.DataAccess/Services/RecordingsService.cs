using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public class RecordingsService : IRecordingsService
{
    private readonly SignalRadioDbContext _db;

    public RecordingsService(SignalRadioDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Recording>> GetAllAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.Recordings.Include(r => r.Transcriptions).AsNoTracking().OrderByDescending(r => r.ReceivedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Recording>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<Recording?> GetByIdAsync(int id)
    {
        return await _db.Recordings.Include(r => r.Transcriptions).AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Recording> CreateAsync(Recording model)
    {
        _db.Recordings.Add(model);
        await _db.SaveChangesAsync();
        return model;
    }

    public async Task<bool> UpdateAsync(int id, Recording model)
    {
        if (id != model.Id) return false;
        var exists = await _db.Recordings.AnyAsync(r => r.Id == id);
        if (!exists) return false;
        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.Recordings.FindAsync(id);
        if (item == null) return false;
        _db.Recordings.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

      public async Task<IEnumerable<Recording>> GetRecordingsNeedingTranscriptionAsync(int limit = 10)
      {
        // DataAccess models: Call.TalkGroupId (int) and TalkGroup.Id (int)
        // Priority: lower numeric value is higher priority. Null means lowest priority.
        var query = from r in _db.Recordings.Include(r => r.Call)
                join tg in _db.TalkGroups on r.Call!.TalkGroupId equals tg.Id into tgJoin
                from tg in tgJoin.DefaultIfEmpty()
                where r.FileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                    && r.StorageLocationId != 0 // approximate "IsUploaded" semantics not available here
                    && !r.Transcriptions.Any(t => t.IsFinal)
                select new { Recording = r, TalkGroupPriority = (int?)tg.Priority };

        var ordered = query
            .OrderBy(x => x.TalkGroupPriority.HasValue ? x.TalkGroupPriority.Value : int.MaxValue)
            .ThenByDescending(x => x.Recording.ReceivedAt)
            .Take(limit)
            .Select(x => x.Recording);

        return await ordered.ToListAsync();
    }
}
