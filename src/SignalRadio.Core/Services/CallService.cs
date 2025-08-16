using SignalRadio.Core.Models;
using SignalRadio.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace SignalRadio.Core.Services;

public interface ICallService
{
    Task<Call> ProcessCallAsync(RecordingUploadRequest request);
    Task<Recording> AddRecordingToCallAsync(int callId, string fileName, string format, string contentType, long fileSize);
    Task MarkRecordingUploadedAsync(int recordingId, string blobUri, string blobName);
    Task<Call?> GetCallByIdAsync(int id);
    Task<IEnumerable<Call>> GetRecentCallsAsync(int limit = 50);
    Task<IEnumerable<Call>> GetCallsByTalkgroupAsync(string talkgroupId, int? limit = null);
    Task<IEnumerable<Call>> GetCallsBySystemAsync(string systemName, int? limit = null);
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
            request.TalkgroupId, 
            request.SystemName, 
            request.Timestamp);

        if (existingCall != null)
        {
            _logger.LogInformation("Found existing call with ID {CallId} for TalkgroupId={TalkgroupId}, System={SystemName}, Time={RecordingTime}",
                existingCall.Id, request.TalkgroupId, request.SystemName, request.Timestamp);
            return existingCall;
        }

        // Create new call
        var newCall = new Call
        {
            TalkgroupId = request.TalkgroupId,
            SystemName = request.SystemName,
            RecordingTime = request.Timestamp,
            Frequency = request.Frequency
        };

        var createdCall = await _callRepository.CreateAsync(newCall);
        
        _logger.LogInformation("Created new call with ID {CallId} for TalkgroupId={TalkgroupId}, System={SystemName}, Time={RecordingTime}",
            createdCall.Id, request.TalkgroupId, request.SystemName, request.Timestamp);

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
}
