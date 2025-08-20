using SignalRadio.Core.Models;

namespace SignalRadio.Core.Repositories;

public interface ICallRepository
{
    Task<Call> CreateAsync(Call call);
    Task<Call?> GetByIdAsync(int id);
    Task<Call?> GetByDetailsAsync(string talkgroupId, string systemName, DateTime recordingTime);
    Task<IEnumerable<Call>> GetRecentAsync(int count = 50);
    Task<IEnumerable<Call>> GetByTalkgroupAsync(string talkgroupId, int? limit = null);
    Task<IEnumerable<Call>> GetBySystemAsync(string systemName, int? limit = null);
    Task<Call> UpdateAsync(Call call);
    Task DeleteAsync(int id);
}

public interface IRecordingRepository
{
    Task<Recording> CreateAsync(Recording recording);
    Task<Recording?> GetByIdAsync(int id);
    Task<IEnumerable<Recording>> GetByCallIdAsync(int callId);
    Task<Recording> UpdateAsync(Recording recording);
    Task DeleteAsync(int id);
    Task MarkAsUploadedAsync(int recordingId, string? blobUri = null, string? blobName = null);
    Task MarkUploadFailedAsync(int recordingId, string? errorMessage = null);
    Task UpdateAudioMetadataAsync(int recordingId, TimeSpan? duration = null, int? sampleRate = null, int? bitrate = null, byte? channels = null, string? quality = null, string? fileHash = null);
    Task<IEnumerable<Recording>> GetFailedUploadsAsync(int maxAttempts = 3);
    Task<Dictionary<string, int>> GetRecordingStatsByFormatAsync();
    Task<Dictionary<string, long>> GetStorageStatsByFormatAsync();
    Task<IEnumerable<Recording>> GetRecordingsNeedingTranscriptionAsync(int limit = 10);
    Task<IEnumerable<Recording>> GetTranscriptionsAsync(int? callId = null, int limit = 50);
    Task UpdateRecordingTranscriptionAsync(int recordingId, TranscriptionResult? transcriptionResult, string? errorMessage);
}

public interface ITalkGroupRepository
{
    Task<TalkGroup> CreateAsync(TalkGroup talkGroup);
    Task<TalkGroup?> GetByIdAsync(int id);
    Task<TalkGroup?> GetByDecimalAsync(string decimalId);
    Task<IEnumerable<TalkGroup>> GetAllAsync();
    Task<IEnumerable<TalkGroup>> GetByCategoryAsync(string category);
    Task<IEnumerable<TalkGroup>> SearchAsync(string searchTerm);
    Task<TalkGroup> UpdateAsync(TalkGroup talkGroup);
    Task DeleteAsync(int id);
    Task DeleteAllAsync();
    Task BulkInsertAsync(IEnumerable<TalkGroup> talkGroups);
}
