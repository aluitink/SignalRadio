using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SignalRadio.Core.Models;
using SignalRadio.Core.Services;
using SignalRadio.Api.Hubs;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordingController : ControllerBase
{
    private readonly ILogger<RecordingController> _logger;
    private readonly IStorageService _storageService;
    private readonly ICallService _callService;
    private readonly IHubContext<TalkGroupHub> _hubContext;
    private readonly IAsrService _asrService;

    public RecordingController(
        ILogger<RecordingController> logger, 
        IStorageService storageService,
        ICallService callService,
        IHubContext<TalkGroupHub> hubContext,
        IAsrService asrService)
    {
        _logger = logger;
        _storageService = storageService;
        _callService = callService;
        _hubContext = hubContext;
        _asrService = asrService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadRecording([FromForm] RecordingUploadRequest request, IFormFile? audioFile, IFormFile? m4aFile)
    {
        try
        {
            var hasWav = audioFile != null && audioFile.Length > 0;
            var hasM4a = m4aFile != null && m4aFile.Length > 0;

            _logger.LogInformation("Recording upload - TalkgroupId={TalkgroupId}, System={SystemName}, Frequency={Frequency}, Timestamp={Timestamp}, Duration={Duration}s, StopTime={StopTime}",
                request.TalkgroupId, request.SystemName, request.Frequency, request.Timestamp, request.Duration, request.StopTime);

            // Check if at least one audio file is provided
            if (!hasWav && !hasM4a)
            {
                _logger.LogWarning("Upload rejected - No audio files provided");
                return BadRequest("No audio file provided");
            }

            // Log received files
            if (hasWav && hasM4a)
            {
                _logger.LogInformation("Files received: WAV ({WavSize:N0} bytes) + M4A ({M4aSize:N0} bytes)",
                    audioFile!.Length, m4aFile!.Length);
            }
            else if (hasWav)
            {
                _logger.LogInformation("Files received: WAV only ({WavSize:N0} bytes)", audioFile!.Length);
            }
            else
            {
                _logger.LogInformation("Files received: M4A only ({M4aSize:N0} bytes)", m4aFile!.Length);
            }

            // Process the call (create or find existing)
            var call = await _callService.ProcessCallAsync(request);

            var uploadedFiles = new List<RecordingMetadata>();
            var storageResults = new List<StorageResult>();
            var recordings = new List<Recording>();

            // Process WAV file if provided
            if (hasWav)
            {
                // Create recording record in database
                var wavRecording = await _callService.AddRecordingToCallAsync(
                    call.Id,
                    audioFile!.FileName,
                    "WAV",
                    audioFile.ContentType,
                    audioFile.Length);

                recordings.Add(wavRecording);

                var wavMetadata = new RecordingMetadata
                {
                    TalkgroupId = request.TalkgroupId,
                    SystemName = request.SystemName,
                    RecordingTime = request.Timestamp,
                    Frequency = request.Frequency,
                    Duration = request.Duration.HasValue ? TimeSpan.FromSeconds(request.Duration.Value) : TimeSpan.Zero,
                    FileName = audioFile!.FileName,
                    OriginalFormat = audioFile.ContentType,
                    OriginalSize = audioFile.Length
                };

                using var wavStream = audioFile.OpenReadStream();
                var wavResult = await _storageService.UploadRecordingAsync(
                    wavStream, 
                    audioFile.FileName, 
                    audioFile.ContentType, 
                    wavMetadata);

                if (wavResult.IsSuccess)
                {
                    // Update recording with blob information
                    await _callService.MarkRecordingUploadedAsync(
                        wavRecording.Id, 
                        wavResult.BlobUri, 
                        wavResult.BlobName);

                    wavMetadata.BlobUri = wavResult.BlobUri;
                    wavMetadata.BlobName = wavResult.BlobName;
                    uploadedFiles.Add(wavMetadata);
                    _logger.LogInformation("WAV file uploaded successfully: {BlobName}", wavResult.BlobName);
                }
                else
                {
                    await _callService.MarkRecordingUploadFailedAsync(wavRecording.Id, wavResult.ErrorMessage ?? "Unknown error");
                    _logger.LogError("Failed to upload WAV file: {Error}", wavResult.ErrorMessage);
                }

                storageResults.Add(wavResult);
            }

            // Process M4A file if provided
            if (hasM4a)
            {
                // Create recording record in database
                var m4aRecording = await _callService.AddRecordingToCallAsync(
                    call.Id,
                    m4aFile!.FileName,
                    "M4A",
                    m4aFile.ContentType,
                    m4aFile.Length);

                recordings.Add(m4aRecording);

                var m4aMetadata = new RecordingMetadata
                {
                    TalkgroupId = request.TalkgroupId,
                    SystemName = request.SystemName,
                    RecordingTime = request.Timestamp,
                    Frequency = request.Frequency,
                    Duration = request.Duration.HasValue ? TimeSpan.FromSeconds(request.Duration.Value) : TimeSpan.Zero,
                    FileName = m4aFile!.FileName,
                    OriginalFormat = m4aFile.ContentType,
                    OriginalSize = m4aFile.Length
                };

                using var m4aStream = m4aFile.OpenReadStream();
                var m4aResult = await _storageService.UploadRecordingAsync(
                    m4aStream, 
                    m4aFile.FileName, 
                    m4aFile.ContentType, 
                    m4aMetadata);

                if (m4aResult.IsSuccess)
                {
                    // Update recording with blob information
                    await _callService.MarkRecordingUploadedAsync(
                        m4aRecording.Id, 
                        m4aResult.BlobUri, 
                        m4aResult.BlobName);

                    m4aMetadata.BlobUri = m4aResult.BlobUri;
                    m4aMetadata.BlobName = m4aResult.BlobName;
                    uploadedFiles.Add(m4aMetadata);
                    _logger.LogInformation("M4A file uploaded successfully: {BlobName}", m4aResult.BlobName);
                }
                else
                {
                    await _callService.MarkRecordingUploadFailedAsync(m4aRecording.Id, m4aResult.ErrorMessage ?? "Unknown error");
                    _logger.LogError("Failed to upload M4A file: {Error}", m4aResult.ErrorMessage);
                }

                storageResults.Add(m4aResult);
            }

            // Check if any uploads failed
            var failedUploads = storageResults.Where(r => !r.IsSuccess).ToList();
            if (failedUploads.Any())
            {
                if (failedUploads.Count == storageResults.Count)
                {
                    // All uploads failed
                    return StatusCode(500, new
                    {
                        Message = "All file uploads failed",
                        Errors = failedUploads.Select(f => f.ErrorMessage).ToArray()
                    });
                }
                else
                {
                    // Partial success
                    _logger.LogWarning("Partial upload success: {SuccessCount}/{TotalCount} files uploaded",
                        storageResults.Count - failedUploads.Count, storageResults.Count);
                }
            }

            var totalUploadedBytes = storageResults.Where(r => r.IsSuccess).Sum(r => r.UploadedBytes);

            // Broadcast new call to SignalR clients
            try 
            {
                var callNotification = new
                {
                    call.Id,
                    call.TalkgroupId,
                    call.SystemName,
                    call.RecordingTime,
                    call.Frequency,
                    call.Duration,
                    call.CreatedAt,
                    call.UpdatedAt,
                    RecordingCount = recordings.Count,
                    Recordings = recordings.Select(r => new
                    {
                        r.Id,
                        r.FileName,
                        r.Format,
                        r.FileSize,
                        r.IsUploaded,
                        r.BlobName,
                        r.UploadedAt
                    })
                };

                // Broadcast to all clients monitoring the general call stream
                await _hubContext.Clients.Group("all_calls_monitor")
                    .SendAsync("AllCallsStreamUpdate", callNotification);

                // Broadcast to clients subscribed to this specific talk group
                await _hubContext.Clients.Group($"talkgroup_{call.TalkgroupId}")
                    .SendAsync("NewCall", callNotification);

                _logger.LogInformation("Broadcasted call notification for talk group {TalkgroupId} to subscribed clients and all-calls monitors", call.TalkgroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast call notification for talk group {TalkgroupId}", call.TalkgroupId);
                // Don't fail the upload if SignalR broadcast fails
            }

            return Ok(new
            {
                Message = "Recording processed successfully",
                CallId = call.Id,
                RecordingIds = recordings.Select(r => r.Id).ToArray(),
                UploadedFiles = uploadedFiles,
                FileCount = uploadedFiles.Count,
                HasDualFormat = hasWav && hasM4a,
                TotalUploadedBytes = totalUploadedBytes,
                Status = "Phase4-DatabaseIntegration",
                StorageResults = storageResults.Select(r => new 
                { 
                    Success = r.IsSuccess, 
                    BlobName = r.BlobName,
                    BlobUri = r.BlobUri,
                    UploadedBytes = r.UploadedBytes,
                    Error = r.ErrorMessage
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recording upload failed");
            return StatusCode(500, "Processing failed");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListRecordings(
        [FromQuery] string? systemName = null,
        [FromQuery] string? talkgroupId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var recordings = await _storageService.ListRecordingsAsync(systemName, talkgroupId, fromDate, toDate);
            
            return Ok(new
            {
                Recordings = recordings,
                Count = recordings.Count(),
                SystemName = systemName,
                TalkgroupId = talkgroupId,
                FromDate = fromDate,
                ToDate = toDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list recordings");
            return StatusCode(500, "Failed to retrieve recordings");
        }
    }

    [HttpGet("stream/{recordingId:int}")]
    public async Task<IActionResult> StreamRecording(int recordingId)
    {
        try
        {
            // Get recording details from database
            var recording = await _callService.GetRecordingByIdAsync(recordingId);
            if (recording == null)
            {
                return NotFound($"Recording not found: {recordingId}");
            }

            if (!recording.IsUploaded || string.IsNullOrEmpty(recording.BlobName))
            {
                return NotFound($"Recording file not available: {recordingId}");
            }

            // Get the audio stream
            var stream = await _storageService.DownloadRecordingAsync(recording.BlobName);
            if (stream == null)
            {
                return NotFound($"Recording file not found in storage: {recording.BlobName}");
            }

            var contentType = GetContentType(recording.FileName);

            // Return stream for audio playback
            return File(stream, contentType, recording.FileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream recording: {RecordingId}", recordingId);
            return StatusCode(500, "Failed to stream recording");
        }
    }

    [HttpGet("download/{*blobName}")]
    public async Task<IActionResult> DownloadRecording(string blobName)
    {
        try
        {
            var stream = await _storageService.DownloadRecordingAsync(blobName);
            if (stream == null)
            {
                return NotFound($"Recording not found: {blobName}");
            }

            var fileName = Path.GetFileName(blobName);
            var contentType = GetContentType(fileName);

            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download recording: {BlobName}", blobName);
            return StatusCode(500, "Failed to download recording");
        }
    }

    [HttpGet("{recordingId:int}/download")]
    public async Task<IActionResult> DownloadRecordingById(int recordingId)
    {
        try
        {
            var recording = await _callService.GetRecordingByIdAsync(recordingId);
            if (recording == null)
            {
                return NotFound($"Recording not found: {recordingId}");
            }

            if (!recording.IsUploaded || string.IsNullOrEmpty(recording.BlobName))
            {
                return BadRequest("Recording is not available for download");
            }

            var stream = await _storageService.DownloadRecordingAsync(recording.BlobName);
            if (stream == null)
            {
                return NotFound($"Recording file not found: {recording.BlobName}");
            }

            var contentType = GetContentType(recording.FileName);

            return File(stream, contentType, recording.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download recording: {RecordingId}", recordingId);
            return StatusCode(500, "Failed to download recording");
        }
    }

    [HttpDelete("delete/{*blobName}")]
    public async Task<IActionResult> DeleteRecording(string blobName)
    {
        try
        {
            var success = await _storageService.DeleteRecordingAsync(blobName);
            if (!success)
            {
                return NotFound($"Recording not found: {blobName}");
            }

            return Ok(new { Message = "Recording deleted successfully", BlobName = blobName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete recording: {BlobName}", blobName);
            return StatusCode(500, "Failed to delete recording");
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "SignalRadio.Api",
            Phase = "4-DatabaseIntegration-Optimized",
            Features = new[] { "WAV Upload", "M4A Upload", "Dual Format Support", "Azure Blob Storage", "SQL Server Database", "Call Tracking", "Recording Management", "Upload Retry", "Audio Metadata", "Quality Analysis" }
        });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _callService.GetRecordingStatsAsync();
            return Ok(new
            {
                Message = "Recording statistics",
                Data = stats,
                Generated = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recording statistics");
            return StatusCode(500, "Failed to retrieve statistics");
        }
    }

    [HttpGet("failed-uploads")]
    public async Task<IActionResult> GetFailedUploads([FromQuery] int maxAttempts = 3)
    {
        try
        {
            var failedUploads = await _callService.GetFailedUploadsAsync(maxAttempts);
            return Ok(new
            {
                Message = "Failed uploads",
                FailedUploads = failedUploads.Select(r => new
                {
                    RecordingId = r.Id,
                    CallId = r.CallId,
                    FileName = r.FileName,
                    Format = r.Format,
                    UploadAttempts = r.UploadAttempts,
                    LastError = r.LastUploadError,
                    CreatedAt = r.CreatedAt,
                    FileSizeMB = r.FileSizeMB
                }),
                Count = failedUploads.Count(),
                MaxAttempts = maxAttempts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed uploads");
            return StatusCode(500, "Failed to retrieve failed uploads");
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Trigger transcription for a specific recording
    /// </summary>
    [HttpPost("{id}/transcribe")]
    public async Task<IActionResult> TranscribeRecording(int id)
    {
        try
        {
            var call = await _callService.GetCallWithRecordingByIdAsync(id);
            if (call == null)
            {
                return NotFound(new { Error = "Recording not found" });
            }

            var recording = call.Recordings.FirstOrDefault(r => r.Id == id);
            if (recording == null)
            {
                return NotFound(new { Error = "Recording not found" });
            }

            // Check if already transcribed
            if (recording.HasTranscription)
            {
                return BadRequest(new { Error = "Recording already has transcription", TranscriptionText = recording.TranscriptionText });
            }

            // Only transcribe WAV files
            if (!recording.FileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { Error = "Only WAV files can be transcribed" });
            }

            // Download the audio file
            var downloadResult = await _storageService.DownloadFileAsync(recording.BlobName ?? recording.FileName);
            if (downloadResult == null)
            {
                return BadRequest(new { Error = "Could not download audio file for transcription" });
            }

            // Perform transcription
            try
            {
                var transcriptionResult = await _asrService.TranscribeAsync(downloadResult, recording.FileName);
                
                // Store the transcription result
                await _callService.UpdateRecordingTranscriptionAsync(id, transcriptionResult, null);

                _logger.LogInformation("Successfully transcribed recording {RecordingId}", id);

                // Broadcast the updated call with transcription to connected clients
                try
                {
                    var updatedCall = await _callService.GetCallByIdAsync(recording.CallId);
                    if (updatedCall != null)
                    {
                        var callNotification = new
                        {
                            updatedCall.Id,
                            updatedCall.TalkgroupId,
                            updatedCall.SystemName,
                            updatedCall.RecordingTime,
                            updatedCall.Frequency,
                            updatedCall.Duration,
                            updatedCall.CreatedAt,
                            updatedCall.UpdatedAt,
                            RecordingCount = updatedCall.Recordings?.Count ?? 0,
                            Recordings = updatedCall.Recordings?.Select(r => new
                            {
                                r.Id,
                                r.FileName,
                                r.Format,
                                r.FileSize,
                                r.IsUploaded,
                                r.BlobName,
                                r.UploadedAt,
                                r.HasTranscription,
                                r.TranscriptionText,
                                r.TranscriptionLanguage,
                                r.TranscriptionConfidence,
                                r.TranscriptionProcessedAt
                            }) ?? Enumerable.Empty<object>()
                        };

                        // Broadcast to all clients monitoring the general call stream
                        await _hubContext.Clients.Group("all_calls_monitor")
                            .SendAsync("CallUpdated", callNotification);

                        // Broadcast to clients subscribed to this specific talk group
                        await _hubContext.Clients.Group($"talkgroup_{updatedCall.TalkgroupId}")
                            .SendAsync("CallUpdated", callNotification);

                        _logger.LogDebug("Broadcasted transcription update for call {CallId} to SignalR clients", updatedCall.Id);
                    }
                }
                catch (Exception broadcastEx)
                {
                    _logger.LogWarning(broadcastEx, "Failed to broadcast transcription update for recording {RecordingId}, but transcription was successful", id);
                }

                return Ok(new 
                { 
                    Message = "Transcription completed",
                    RecordingId = id,
                    TranscriptionText = transcriptionResult.Text,
                    Language = transcriptionResult.Language,
                    Confidence = transcriptionResult.Segments.Any() && 
                        transcriptionResult.Segments.Any(s => s.Confidence.HasValue) ? 
                        transcriptionResult.Segments.Where(s => s.Confidence.HasValue).Average(s => s.Confidence!.Value) : (double?)null
                });
            }
            catch (Exception transcriptionEx)
            {
                // Store the error in the database
                await _callService.UpdateRecordingTranscriptionAsync(id, null, transcriptionEx.Message);
                return BadRequest(new { Error = transcriptionEx.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing recording {RecordingId}", id);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Get transcription status and result for a specific recording
    /// </summary>
    [HttpGet("{id}/transcription")]
    public async Task<IActionResult> GetTranscription(int id)
    {
        try
        {
            var call = await _callService.GetCallWithRecordingByIdAsync(id);
            if (call == null)
            {
                return NotFound(new { Error = "Recording not found" });
            }

            var recording = call.Recordings.FirstOrDefault(r => r.Id == id);
            if (recording == null)
            {
                return NotFound(new { Error = "Recording not found" });
            }

            var response = new
            {
                RecordingId = id,
                HasTranscription = recording.HasTranscription,
                TranscriptionText = recording.TranscriptionText,
                Language = recording.TranscriptionLanguage,
                Confidence = recording.TranscriptionConfidence,
                ProcessedAt = recording.TranscriptionProcessedAt,
                SegmentCount = !string.IsNullOrEmpty(recording.TranscriptionSegments) ? 
                    System.Text.Json.JsonSerializer.Deserialize<object[]>(recording.TranscriptionSegments)?.Length : 0,
                ErrorMessage = recording.LastTranscriptionError
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transcription for recording {RecordingId}", id);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Get all recordings that need transcription (WAV files without transcription)
    /// </summary>
    [HttpGet("pending-transcription")]
    public async Task<IActionResult> GetPendingTranscriptions()
    {
        try
        {
            var pendingCalls = await _callService.GetRecordingsNeedingTranscriptionAsync();
            
            var response = pendingCalls.SelectMany(call => 
                call.Recordings.Select(recording => new
                {
                    RecordingId = recording.Id,
                    FileName = recording.FileName,
                    TalkGroup = call.TalkgroupId,
                    UploadedAt = recording.UploadedAt,
                    Duration = recording.Duration,
                    FileSize = recording.FileSize
                })
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending transcriptions");
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}
