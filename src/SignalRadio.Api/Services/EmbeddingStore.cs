using Microsoft.EntityFrameworkCore;
using SignalRadio.Data;

namespace SignalRadio.Api.Services;

/// <summary>
/// Stores pgvector embeddings via raw SQL. The <c>Embedding</c> columns are managed outside
/// EF Core (see <c>DatabaseInitializer</c>), so we write them directly.
/// </summary>
public static class EmbeddingStore
{
    private static string ToVectorLiteral(float[] vector) =>
        "[" + string.Join(", ", vector.Select(v => v.ToString("R"))) + "]";

    public static async Task SetTranscriptionEmbeddingAsync(
        SignalRadioDbContext db, long transcriptionId, float[] embedding, CancellationToken ct = default)
    {
        var literal = ToVectorLiteral(embedding);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Transcriptions\" SET \"Embedding\" = {literal}::vector WHERE \"Id\" = {transcriptionId}", ct);
    }

    public static async Task SetSummaryEmbeddingAsync(
        SignalRadioDbContext db, long summaryId, float[] embedding, CancellationToken ct = default)
    {
        var literal = ToVectorLiteral(embedding);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"TranscriptSummaries\" SET \"Embedding\" = {literal}::vector WHERE \"Id\" = {summaryId}", ct);
    }
}
