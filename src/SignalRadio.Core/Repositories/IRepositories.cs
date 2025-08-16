using SignalRadio.Core.Models;

namespace SignalRadio.Core.Repositories;

public interface ICallRepository
{
    Task<Call?> GetByIdAsync(int id);
    Task<Call?> GetByDetailsAsync(string talkgroupId, string systemName, DateTime recordingTime);
    Task<IEnumerable<Call>> GetByTalkgroupAsync(string talkgroupId, int? limit = null);
    Task<IEnumerable<Call>> GetBySystemAsync(string systemName, int? limit = null);
    Task<IEnumerable<Call>> GetRecentCallsAsync(int limit = 50);
    Task<IEnumerable<Call>> GetCallsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<Call> CreateAsync(Call call);
    Task<Call> UpdateAsync(Call call);
    Task DeleteAsync(int id);
}

public interface IRecordingRepository
{
    Task<Recording?> GetByIdAsync(int id);
    Task<IEnumerable<Recording>> GetByCallIdAsync(int callId);
    Task<IEnumerable<Recording>> GetByFormatAsync(string format);
    Task<IEnumerable<Recording>> GetUnuploadedAsync();
    Task<Recording> CreateAsync(Recording recording);
    Task<Recording> UpdateAsync(Recording recording);
    Task DeleteAsync(int id);
    Task MarkAsUploadedAsync(int id, string blobUri, string blobName);
}
