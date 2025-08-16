using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Models;
using SignalRadio.Core.Services;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordingController : ControllerBase
{
    private readonly ILogger<RecordingController> _logger;
    private readonly IStorageService _storageService;
    private readonly ICallService _callService;

    public RecordingController(
        ILogger<RecordingController> logger, 
        IStorageService storageService,
        ICallService callService)
    {
        _logger = logger;
        _storageService = storageService;
        _callService = callService;
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
}
