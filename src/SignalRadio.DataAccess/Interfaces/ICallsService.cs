using System.Threading.Tasks;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public interface ICallsService
{
    Task<PagedResult<Call>> GetAllAsync(int page, int pageSize, string? sortBy = null, string? sortDir = null);
    Task<PagedResult<Call>> GetAllCallsByTalkGroupAsync(int talkGroupId, int page, int pageSize, string? sortBy = null, string? sortDir = null);
    Task<Dictionary<double, List<Call>>> GetCallsByFrequencyForTalkGroupAsync(int talkGroupId, int limit = 50);
    Task<Dictionary<int, TalkGroupStats>> GetTalkGroupStatsAsync();
    Task<List<int>> GetTalkGroupsWithTranscriptsAsync(int windowMinutes = 15);
    Task<Call?> GetByIdAsync(int id);
    Task<Call> CreateAsync(Call model);
    Task<bool> UpdateAsync(int id, Call model);
    Task<bool> DeleteAsync(int id);
}

public class TalkGroupStats
{
    public int TalkGroupId { get; set; }
    public int CallCount { get; set; }
    public DateTimeOffset? LastActivity { get; set; }
    public double TotalDurationSeconds { get; set; }
}
