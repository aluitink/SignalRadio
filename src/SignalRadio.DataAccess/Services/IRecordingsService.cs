using System.Threading.Tasks;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public interface IRecordingsService
{
    Task<PagedResult<Recording>> GetAllAsync(int page, int pageSize);
    Task<Recording?> GetByIdAsync(int id);
    Task<Recording> CreateAsync(Recording model);
    Task<bool> UpdateAsync(int id, Recording model);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Recording>> GetRecordingsNeedingTranscriptionAsync(int limit = 10);
}
