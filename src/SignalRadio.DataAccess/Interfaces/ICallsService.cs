using System.Threading.Tasks;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public interface ICallsService
{
    Task<PagedResult<Call>> GetAllAsync(int page, int pageSize, string? sortBy = null, string? sortDir = null);
    Task<PagedResult<Call>> GetAllCallsByTalkGroupAsync(int talkGroupId, int page, int pageSize, string? sortBy = null, string? sortDir = null);
    Task<Dictionary<double, List<Call>>> GetCallsByFrequencyForTalkGroupAsync(int talkGroupId, int limit = 50);
    Task<Call?> GetByIdAsync(int id);
    Task<Call> CreateAsync(Call model);
    Task<bool> UpdateAsync(int id, Call model);
    Task<bool> DeleteAsync(int id);
}
