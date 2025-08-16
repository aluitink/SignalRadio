using SignalRadio.Core.Models;
using SignalRadio.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace SignalRadio.Core.Services;

public interface ICallService
{
    Task<Call> ProcessCallAsync(RecordingUploadRequest request);
    Task<Recording> AddRecordingToCallAsync(int callId, string fileName, string format, string contentType, long fileSize);
    Task MarkRecordingUploadedAsync(int recordingId, string blobUri, string blobName);
    Task MarkRecordingUploadFailedAsync(int recordingId, string errorMessage);
    Task UpdateRecordingMetadataAsync(int recordingId, TimeSpan? duration, int? sampleRate, int? bitrate, byte? channels, string? quality, string? fileHash);
    Task<Call?> GetCallByIdAsync(int id);
    Task<IEnumerable<Call>> GetRecentCallsAsync(int limit = 50);
    Task<IEnumerable<Call>> GetCallsByTalkgroupAsync(string talkgroupId, int? limit = null);
    Task<IEnumerable<Call>> GetCallsBySystemAsync(string systemName, int? limit = null);
    Task UpdateCallDurationAsync(int callId, TimeSpan duration);
    Task<IEnumerable<Recording>> GetFailedUploadsAsync(int maxAttempts = 3);
    Task<Dictionary<string, object>> GetRecordingStatsAsync();
}

public class CallService : ICallService
{
    private readonly ICallRepository _callRepository;
    private readonly IRecordingRepository _recordingRepository;
    private readonly ILogger<CallService> _logger;

    public CallService(
        ICallRepository callRepository,
        IRecordingRepository recordingRepository,
        ILogger<CallService> logger)
    {
        _callRepository = callRepository;
        _recordingRepository = recordingRepository;
        _logger = logger;
    }

    public async Task<Call> ProcessCallAsync(RecordingUploadRequest request)
    {
        // Check if a call with these exact details already exists
        var existingCall = await _callRepository.GetByDetailsAsync(
            request.TalkgroupId.Trim(), 
            request.SystemName.Trim(), 
            request.Timestamp);

        if (existingCall != null)
        {
            // Update duration if provided and not already set
            if (request.Duration.HasValue && existingCall.Duration == null)
            {
                existingCall.Duration = TimeSpan.FromSeconds(request.Duration.Value);
                await _callRepository.UpdateAsync(existingCall);
                _logger.LogInformation("Updated existing call {CallId} with duration {Duration}s",
                    existingCall.Id, request.Duration.Value);
            }

            _logger.LogInformation("Found existing call with ID {CallId} for TalkgroupId={TalkgroupId}, System={SystemName}, Time={RecordingTime}",
                existingCall.Id, request.TalkgroupId, request.SystemName, request.Timestamp);
            return existingCall;
        }

        // Create new call
        var newCall = new Call
        {
            TalkgroupId = request.TalkgroupId.Trim(),
            SystemName = request.SystemName.Trim(),
            RecordingTime = request.Timestamp,
            Frequency = SanitizeFrequency(request.Frequency),
            Duration = request.Duration.HasValue ? TimeSpan.FromSeconds(request.Duration.Value) : null
        };

        var createdCall = await _callRepository.CreateAsync(newCall);
        
        _logger.LogInformation("Created new call with ID {CallId} for TalkgroupId={TalkgroupId}, System={SystemName}, Time={RecordingTime}, Duration={Duration}s",
            createdCall.Id, request.TalkgroupId, request.SystemName, request.Timestamp, request.Duration);

        return createdCall;
    }

    public async Task<Recording> AddRecordingToCallAsync(int callId, string fileName, string format, string contentType, long fileSize)
    {
        var recording = new Recording
        {
            CallId = callId,
            FileName = fileName,
            Format = format.ToUpperInvariant(),
            ContentType = contentType,
            FileSize = fileSize,
            IsUploaded = false
        };

        var createdRecording = await _recordingRepository.CreateAsync(recording);
        
        _logger.LogInformation("Created recording with ID {RecordingId} for call {CallId}, format {Format}, size {FileSize} bytes",
            createdRecording.Id, callId, format, fileSize);

        return createdRecording;
    }

    public async Task MarkRecordingUploadedAsync(int recordingId, string blobUri, string blobName)
    {
        await _recordingRepository.MarkAsUploadedAsync(recordingId, blobUri, blobName);
        _logger.LogInformation("Marked recording {RecordingId} as uploaded to {BlobName}",
            recordingId, blobName);
    }

    public async Task MarkRecordingUploadFailedAsync(int recordingId, string errorMessage)
    {
        await _recordingRepository.MarkUploadFailedAsync(recordingId, errorMessage);
        _logger.LogWarning("Marked recording {RecordingId} upload as failed: {Error}",
            recordingId, errorMessage);
    }

    public async Task UpdateRecordingMetadataAsync(int recordingId, TimeSpan? duration, int? sampleRate, int? bitrate, byte? channels, string? quality, string? fileHash)
    {
        await _recordingRepository.UpdateAudioMetadataAsync(recordingId, duration, sampleRate, bitrate, channels, quality, fileHash);
        _logger.LogInformation("Updated metadata for recording {RecordingId}: Duration={Duration}, SampleRate={SampleRate}, Bitrate={Bitrate}, Quality={Quality}",
            recordingId, duration, sampleRate, bitrate, quality);
    }

    public async Task<Call?> GetCallByIdAsync(int id)
    {
        return await _callRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Call>> GetRecentCallsAsync(int limit = 50)
    {
        return await _callRepository.GetRecentCallsAsync(limit);
    }

    public async Task<IEnumerable<Call>> GetCallsByTalkgroupAsync(string talkgroupId, int? limit = null)
    {
        return await _callRepository.GetByTalkgroupAsync(talkgroupId, limit);
    }

    public async Task<IEnumerable<Call>> GetCallsBySystemAsync(string systemName, int? limit = null)
    {
        return await _callRepository.GetBySystemAsync(systemName, limit);
    }

    public async Task UpdateCallDurationAsync(int callId, TimeSpan duration)
    {
        var call = await _callRepository.GetByIdAsync(callId);
        if (call != null)
        {
            call.Duration = duration;
            call.UpdatedAt = DateTime.UtcNow;
            await _callRepository.UpdateAsync(call);
            _logger.LogInformation("Updated call {CallId} duration to {Duration}s", callId, duration.TotalSeconds);
        }
        else
        {
            _logger.LogWarning("Call {CallId} not found for duration update", callId);
        }
    }

    public async Task<IEnumerable<Recording>> GetFailedUploadsAsync(int maxAttempts = 3)
    {
        return await _recordingRepository.GetFailedUploadsAsync(maxAttempts);
    }

    public async Task<Dictionary<string, object>> GetRecordingStatsAsync()
    {
        var formatStats = await _recordingRepository.GetRecordingStatsByFormatAsync();
        var storageStats = await _recordingRepository.GetStorageStatsByFormatAsync();
        var totalRecordings = formatStats.Values.Sum();
        var totalStorage = storageStats.Values.Sum();
        var failedUploads = await _recordingRepository.GetFailedUploadsAsync();

        return new Dictionary<string, object>
        {
            ["TotalRecordings"] = totalRecordings,
            ["TotalStorageBytes"] = totalStorage,
            ["TotalStorageMB"] = Math.Round(totalStorage / 1024.0 / 1024.0, 2),
            ["RecordingsByFormat"] = formatStats,
            ["StorageByFormat"] = storageStats.ToDictionary(x => x.Key, x => Math.Round(x.Value / 1024.0 / 1024.0, 2)),
            ["FailedUploads"] = failedUploads.Count(),
            ["AverageFileSizeMB"] = totalRecordings > 0 ? Math.Round(totalStorage / 1024.0 / 1024.0 / totalRecordings, 2) : 0
        };
    }

    private static string SanitizeFrequency(string frequency)
    {
        if (string.IsNullOrEmpty(frequency))
            return "0";

        // Remove whitespace, newlines, and other control characters
        var sanitized = frequency.Trim();
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", "");
        
        // Keep only digits and decimal points
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[^\d.]", "");
        
        // Ensure it's not too long for the database field (max 20 chars)
        if (sanitized.Length > 20)
        {
            sanitized = sanitized.Substring(0, 20);
        }

        return string.IsNullOrEmpty(sanitized) ? "0" : sanitized;
    }
}
