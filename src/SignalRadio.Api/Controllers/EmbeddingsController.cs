using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/embeddings")]
public class EmbeddingsController(IAiClient ai) : ControllerBase
{
    [HttpGet("test")]
    public async Task<IActionResult> Test([FromQuery] string text = "fire at the intersection of main and 5th", CancellationToken ct = default)
    {
        if (!ai.AreEmbeddingsEnabled)
            return BadRequest("Embeddings are not enabled.");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var embedding = await ai.EmbedAsync(text, ct);
        sw.Stop();

        return Ok(new
        {
            text,
            dimension = embedding.Length,
            elapsedMs = sw.ElapsedMilliseconds,
            first10 = embedding[..Math.Min(10, embedding.Length)],
            norm = MathF.Sqrt(embedding.Sum(x => x * x))
        });
    }
}
