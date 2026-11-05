using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/recordings")]
public class RecordingsController(IRecordingsService recordings) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(524288000)]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, [FromForm] string? metadata, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No audio file provided.");
        if (string.IsNullOrWhiteSpace(metadata))
            return BadRequest("No metadata provided.");

        RecordingUploadRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<RecordingUploadRequest>(metadata, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest("Invalid metadata JSON.");
        }
        if (request == null)
            return BadRequest("Invalid metadata JSON.");

        using var stream = file.OpenReadStream();
        var result = await recordings.CreateWithFileAsync(stream, file.FileName, request, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await recordings.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("call/{callId:long}/download")]
    public async Task<IActionResult> DownloadByCallId(long callId, CancellationToken ct)
    {
        var stream = await recordings.GetFileByCallIdAsync(callId, ct);
        if (stream == null) return NotFound();
        return File(stream, "audio/wav");
    }

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id, CancellationToken ct)
    {
        var stream = await recordings.GetFileAsync(id, ct);
        if (stream == null) return NotFound();
        return File(stream, "audio/wav");
    }

    [HttpGet("file/{fileName}")]
    public async Task<IActionResult> DownloadByFileName(string fileName, CancellationToken ct)
    {
        var stream = await recordings.GetFileByFileNameAsync(fileName, ct);
        if (stream == null) return NotFound();
        return File(stream, "audio/wav");
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var deleted = await recordings.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
