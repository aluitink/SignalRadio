using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/calls")]
public class CallsController(ICallsService calls, IRecordingsService recordings, ICallNotifier notifier) : ControllerBase
{
    [HttpGet("talkgroup/{talkGroupId:int}")]
    public async Task<IActionResult> GetByTalkGroup(int talkGroupId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await calls.GetPagedAsync(talkGroupId, page, pageSize, ct));

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 20, CancellationToken ct = default) =>
        Ok(await calls.GetRecentAsync(count, ct));

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
