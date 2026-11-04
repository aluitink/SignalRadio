using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/transcriptions")]
public class TranscriptionsController(ITranscriptionsService transcriptions) : ControllerBase
{
    [HttpGet("recording/{recordingId:long}")]
    public async Task<IActionResult> GetByRecording(long recordingId, CancellationToken ct) =>
        Ok(await transcriptions.GetByRecordingIdAsync(recordingId, ct));

    [HttpGet("recording/{recordingId:long}/final")]
    public async Task<IActionResult> GetFinalByRecording(long recordingId, CancellationToken ct)
    {
        var result = await transcriptions.GetFinalByRecordingIdAsync(recordingId, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var deleted = await transcriptions.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
