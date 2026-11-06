using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/calls")]
public class CallsController(ICallsService calls, IRecordingsService recordings, ICallNotifier notifier) : ControllerBase
{
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 20, CancellationToken ct = default) =>
        Ok(await calls.GetRecentAsync(count, ct));

    // Unified cursor-paged call list. Omit talkGroupId for the global stream;
    // pass it to scope the list to a single talk group.
    [HttpGet("cursor")]
    public async Task<IActionResult> GetCursor(
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTimeOffset? beforeTime = null,
        [FromQuery] long? beforeId = null,
        [FromQuery] int? talkGroupId = null,
        [FromQuery] string[]? talkGroupIds = null,
        CancellationToken ct = default)
    {
        var ids = talkGroupIds?
            .Select(s => int.TryParse(s, out var v) ? v : (int?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();
        return Ok(await calls.GetCursorAsync(pageSize, beforeTime, beforeId, talkGroupId, ids, ct));
    }

    [HttpGet("volume")]
    public async Task<IActionResult> GetVolume(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int? talkGroupId,
        [FromQuery] int? bucketMinutes,
        CancellationToken ct) =>
        Ok(await calls.GetVolumeAsync(from, to, talkGroupId, bucketMinutes, ct));

    [HttpGet("volume-by-talkgroup")]
    public async Task<IActionResult> GetVolumeByTalkGroup(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int? bucketMinutes,
        CancellationToken ct) =>
        Ok(await calls.GetVolumeByTalkGroupAsync(from, to, bucketMinutes, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await calls.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCallRequest request, CancellationToken ct)
    {
        var call = await calls.CreateAsync(request, ct);
        await notifier.NotifyNewCallAsync(call.TalkGroupId, call.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = call.Id }, call);
    }

    [HttpPost("{id:long}/recordings")]
    [RequestSizeLimit(500_000_000)]
    public async Task<IActionResult> UploadRecording(long id, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("No file provided.");

        using var stream = file.OpenReadStream();
        var recording = await recordings.UploadAsync(id, file.FileName, stream, file.Length, ct);
        return Ok(recording);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var deleted = await calls.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
