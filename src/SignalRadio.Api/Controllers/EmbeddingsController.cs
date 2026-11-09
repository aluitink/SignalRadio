using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalRadio.Api.Services;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data;
using SignalRadio.Data.Entities;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/embeddings")]
public class EmbeddingsController(IAiClient ai, SignalRadioDbContext db) : ControllerBase
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

    /// <summary>
    /// Re-embeds all final transcriptions that have text. Use after fixing the
    /// embedding model or tokenizer to refresh stale vectors.
    /// </summary>
    [HttpPost("re-embed")]
    public async Task<IActionResult> ReEmbed(CancellationToken ct)
    {
        if (!ai.AreEmbeddingsEnabled)
            return BadRequest("Embeddings are not enabled.");

        var transcriptions = await db.Transcriptions
            .Where(t => t.IsFinal && t.FullText != null && t.FullText.Length > 0)
            .ToListAsync(ct);

        var updated = 0;
        var failed = 0;

        foreach (var t in transcriptions)
        {
            try
            {
                var embedding = await ai.EmbedAsync(t.FullText, ct);
                if (embedding.Length > 0)
                {
                    await EmbeddingStore.SetTranscriptionEmbeddingAsync(db, t.Id, embedding, ct);
                    updated++;
                }
            }
            catch
            {
                failed++;
            }
        }

        return Ok(new { total = transcriptions.Count, updated, failed });
    }
}
