using System.Threading.Tasks;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public interface IStorageLocationsService
{
    Task<PagedResult<StorageLocation>> GetAllAsync(int page, int pageSize);
    Task<StorageLocation?> GetByIdAsync(int id);
    Task<StorageLocation> CreateAsync(StorageLocation model);
    Task<bool> UpdateAsync(int id, StorageLocation model);
    Task<bool> DeleteAsync(int id);
}
