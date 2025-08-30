using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public class TalkGroupsService : ITalkGroupsService
{
    private readonly SignalRadioDbContext _db;

    public TalkGroupsService(SignalRadioDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<TalkGroup>> GetAllAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.TalkGroups.AsNoTracking().OrderBy(t => t.Number);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<TalkGroup>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<TalkGroup?> GetByIdAsync(int id)
    {
        return await _db.TalkGroups.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TalkGroup> CreateAsync(TalkGroup model)
    {
        _db.TalkGroups.Add(model);
        await _db.SaveChangesAsync();
        return model;
    }

    public async Task<bool> UpdateAsync(int id, TalkGroup model)
    {
        if (id != model.Id) return false;
        var exists = await _db.TalkGroups.AnyAsync(t => t.Id == id);
        if (!exists) return false;
        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.TalkGroups.FindAsync(id);
        if (item == null) return false;
        _db.TalkGroups.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
