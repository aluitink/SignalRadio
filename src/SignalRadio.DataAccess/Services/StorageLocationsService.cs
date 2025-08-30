using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public class StorageLocationsService : IStorageLocationsService
{
    private readonly SignalRadioDbContext _db;

    public StorageLocationsService(SignalRadioDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<StorageLocation>> GetAllAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.StorageLocations.AsNoTracking().OrderBy(s => s.Id);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<StorageLocation>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<StorageLocation?> GetByIdAsync(int id)
    {
        return await _db.StorageLocations.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<StorageLocation> CreateAsync(StorageLocation model)
    {
        _db.StorageLocations.Add(model);
        await _db.SaveChangesAsync();
        return model;
    }

    public async Task<bool> UpdateAsync(int id, StorageLocation model)
    {
        if (id != model.Id) return false;
        var exists = await _db.StorageLocations.AnyAsync(s => s.Id == id);
        if (!exists) return false;
        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.StorageLocations.FindAsync(id);
        if (item == null) return false;
        _db.StorageLocations.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
