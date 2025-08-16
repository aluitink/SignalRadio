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
    public IActionResult UploadRecording([FromForm] RecordingUploadRequest request, IFormFile audioFile)
    {
        try
        {
            _logger.LogInformation("Received recording upload: TalkgroupId={TalkgroupId}, System={SystemName}, Frequency={Frequency}", 
                request.TalkgroupId, request.SystemName, request.Frequency);

            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("No audio file provided");
            }

            // Phase 1: Just log the upload details
            var metadata = new RecordingMetadata
            {
                TalkgroupId = request.TalkgroupId,
                SystemName = request.SystemName,
                RecordingTime = request.Timestamp,
                Frequency = request.Frequency,
                FileName = audioFile.FileName,
                OriginalFormat = audioFile.ContentType,
                OriginalSize = audioFile.Length
            };

            _logger.LogInformation("Processing recording: {FileName}, Size: {Size} bytes", 
                metadata.FileName, metadata.OriginalSize);

            // TODO Phase 2: Add audio processing
            // TODO Phase 3: Add Azure Blob Storage upload

            return Ok(new { 
                Message = "Recording received successfully", 
                Metadata = metadata,
                Status = "Phase1-LogOnly"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process recording upload");
            return StatusCode(500, "Processing failed");
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "SignalRadio.Api", Phase = "1" });
    }
}
