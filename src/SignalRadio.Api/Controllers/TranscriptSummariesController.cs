using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/transcript-summaries")]
public class TranscriptSummariesController(ITranscriptSummariesService summaries) : ControllerBase
{
    [HttpGet("talkgroup/{talkGroupId:int}")]
    public async Task<IActionResult> GetByTalkGroup(int talkGroupId, [FromQuery] DateTimeOffset? startTime, [FromQuery] DateTimeOffset? endTime, CancellationToken ct) =>
        Ok(await summaries.GetByTalkGroupAsync(talkGroupId, startTime, endTime, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await summaries.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTranscriptSummaryRequest request, CancellationToken ct) =>
        CreatedAtAction(nameof(GetById), new { id = (await summaries.CreateAsync(request, ct)).Id }, null);
}
