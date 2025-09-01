using System.Threading.Tasks;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public interface ITalkGroupsService
{
    Task<PagedResult<TalkGroup>> GetAllAsync(int page, int pageSize);
    Task<TalkGroup?> GetByIdAsync(int id);
    Task<TalkGroup> CreateAsync(TalkGroup model);
    Task<bool> UpdateAsync(int id, TalkGroup model);
    Task<bool> DeleteAsync(int id);
}
