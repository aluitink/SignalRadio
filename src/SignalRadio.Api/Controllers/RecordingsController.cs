using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalRadio.DataAccess;
using SignalRadio.DataAccess.Services;
using SignalRadio.Core.Services;
using System.IO;
using SignalRadio.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordingsController : ControllerBase
{
    private readonly IRecordingsService _svc;
    private readonly ILogger<RecordingsController> _logger;
    private readonly IStorageService? _storage;
    private readonly string _basePath = string.Empty;

    // Single constructor: storage is optional so DI isn't ambiguous whether IStorageService is registered.
    public RecordingsController(IRecordingsService svc, ILogger<RecordingsController> logger, IStorageService? storage = null)
    {
        _svc = svc;
        _logger = logger;
        _storage = storage;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

    var result = await _svc.GetAllAsync(page, pageSize);
    return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
    var item = await _svc.GetByIdAsync(id);
    return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Recording model)
    {
    var created = await _svc.CreateAsync(model);
    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Accept a multipart/form-data POST containing a recording file and JSON metadata.
    /// Form fields:
    /// - file: the audio file
    /// - metadata: JSON string matching RecordingUploadRequest or RecordingMetadata shapes
    /// This endpoint will upload the file via IStorageService and then create the Recording row.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(524288000)] // 500MB limit - adjust as needed
    public async Task<IActionResult> CreateWithFile([FromForm] IFormFile? file, [FromForm] string? metadata)
    {
        if (file == null) return BadRequest("Missing file");

        // We expect a RecordingUploadRequest JSON string in the 'metadata' form field
        if (string.IsNullOrEmpty(metadata)) return BadRequest("Missing metadata");

        RecordingUploadRequest? req = null;
        try
        {
            req = JsonSerializer.Deserialize<RecordingUploadRequest>(metadata, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            req = null;
        }

        if (req == null) return BadRequest("Invalid metadata: expected RecordingUploadRequest JSON");

        // Open the uploaded file and hand off to the recordings service which will handle storage and DB persistence
        using var stream = file.OpenReadStream();
        try
        {
            // Map RecordingUploadRequest to core upload request and pass to service
            var created = await _svc.CreateWithFileAsync(stream, file.FileName, file.ContentType ?? "application/octet-stream", req);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error creating recording with file");
            return StatusCode(500, "Failed to create recording");
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Recording model)
    {
    if (id != model.Id) return BadRequest("Id mismatch");
    var ok = await _svc.UpdateAsync(id, model);
    return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
    var ok = await _svc.DeleteAsync(id);
    return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Download the audio file for a recording by id. Streams the file from the configured storage service.
    /// </summary>
    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item == null) return NotFound();

        if (_storage == null)
        {
            _logger.LogError("Storage service not configured - cannot download recording file {Id}", id);
            return StatusCode(500, "Storage service not available");
        }

        var stream = await _storage.DownloadRecordingAsync(item.FileName);
        if (stream == null)
        {
            _logger.LogWarning("Recording file not found in storage: {FileName}", item.FileName);
            return NotFound();
        }

        // Log that the recording file was requested/played. Include Call and TalkGroup info if available.
        try
        {
            if (item.Call != null)
            {
                _logger.LogInformation("Recording {RecordingId} (file={FileName}) requested - associated CallId={CallId}, TalkGroupId={TalkGroupId}",
                    item.Id, item.FileName, item.CallId, item.Call.TalkGroupId);
            }
            else
            {
                _logger.LogInformation("Recording {RecordingId} (file={FileName}) requested - no associated Call in DB lookup", item.Id, item.FileName);
            }
        }
        catch
        {
            // Swallow logging exceptions to avoid breaking delivery
        }

        // Try to infer a sensible content type from file extension; fall back to octet-stream
        string contentType = "application/octet-stream";
        var ext = Path.GetExtension(item.FileName)?.ToLowerInvariant();
        if (ext == ".wav") contentType = "audio/wav";
        else if (ext == ".mp3") contentType = "audio/mpeg";
        else if (ext == ".m4a") contentType = "audio/mp4";

        return File(stream, contentType, item.FileName);
    }
}
