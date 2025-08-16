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

    public RecordingController(ILogger<RecordingController> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadRecording([FromForm] RecordingUploadRequest request, IFormFile? audioFile, IFormFile? m4aFile)
    {
        try
        {
            var hasWav = audioFile != null && audioFile.Length > 0;
            var hasM4a = m4aFile != null && m4aFile.Length > 0;

            _logger.LogInformation("Recording upload - TalkgroupId={TalkgroupId}, System={SystemName}, Frequency={Frequency}, Timestamp={Timestamp}",
                request.TalkgroupId, request.SystemName, request.Frequency, request.Timestamp);

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

            var uploadedFiles = new List<RecordingMetadata>();
            var storageResults = new List<StorageResult>();

            // Process WAV file if provided
            if (hasWav)
            {
                var wavMetadata = new RecordingMetadata
                {
                    TalkgroupId = request.TalkgroupId,
                    SystemName = request.SystemName,
                    RecordingTime = request.Timestamp,
                    Frequency = request.Frequency,
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
                    wavMetadata.BlobUri = wavResult.BlobUri;
                    wavMetadata.BlobName = wavResult.BlobName;
                    uploadedFiles.Add(wavMetadata);
                    _logger.LogInformation("WAV file uploaded successfully: {BlobName}", wavResult.BlobName);
                }
                else
                {
                    _logger.LogError("Failed to upload WAV file: {Error}", wavResult.ErrorMessage);
                }

                storageResults.Add(wavResult);
            }

            // Process M4A file if provided
            if (hasM4a)
            {
                var m4aMetadata = new RecordingMetadata
                {
                    TalkgroupId = request.TalkgroupId,
                    SystemName = request.SystemName,
                    RecordingTime = request.Timestamp,
                    Frequency = request.Frequency,
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
                    m4aMetadata.BlobUri = m4aResult.BlobUri;
                    m4aMetadata.BlobName = m4aResult.BlobName;
                    uploadedFiles.Add(m4aMetadata);
                    _logger.LogInformation("M4A file uploaded successfully: {BlobName}", m4aResult.BlobName);
                }
                else
                {
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
                UploadedFiles = uploadedFiles,
                FileCount = uploadedFiles.Count,
                HasDualFormat = hasWav && hasM4a,
                TotalUploadedBytes = totalUploadedBytes,
                Status = "Phase3-AzureStorage",
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
            Phase = "3-AzureStorage",
            Features = new[] { "WAV Upload", "M4A Upload", "Dual Format Support", "Azure Blob Storage", "Recording Management" }
        });
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
