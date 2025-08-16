using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Models;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordingController : ControllerBase
{
    private readonly ILogger<RecordingController> _logger;

    public RecordingController(ILogger<RecordingController> logger)
    {
        _logger = logger;
    }

    [HttpPost("upload")]
    public IActionResult UploadRecording([FromForm] RecordingUploadRequest request, IFormFile? audioFile, IFormFile? m4aFile)
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
                uploadedFiles.Add(wavMetadata);
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
                uploadedFiles.Add(m4aMetadata);
            }

            // TODO Phase 3: Add Azure Blob Storage upload

            return Ok(new
            {
                Message = "Recording received successfully",
                UploadedFiles = uploadedFiles,
                FileCount = uploadedFiles.Count,
                HasDualFormat = hasWav && hasM4a,
                Status = "Phase2-DualFileHandling"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recording upload failed");
            return StatusCode(500, "Processing failed");
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "SignalRadio.Api",
            Phase = "2-DualFileHandling",
            Features = new[] { "WAV Upload", "M4A Upload", "Dual Format Support", "Enhanced Logging" }
        });
    }
}
