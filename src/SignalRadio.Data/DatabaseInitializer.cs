using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SignalRadio.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(SignalRadioDbContext db, ILogger logger)
    {
        await db.Database.EnsureCreatedAsync();
        logger.LogInformation("Database created/verified via EnsureCreated.");
        await ApplySearchIndexesAsync(db, logger);
        logger.LogInformation("DIAG: ApplySearchIndexesAsync returned, disposing context...");
        await db.DisposeAsync();
        logger.LogInformation("DIAG: Context disposed.");
    }

    public static async Task SeedDataAsync(SignalRadioDbContext db, ILogger logger)
    {
        // Seed storage location if missing
        if (!await db.StorageLocations.AnyAsync())
        {
            db.StorageLocations.Add(new Entities.StorageLocation
            {
                Kind = Entities.StorageKind.Local,
                LocationUri = "/data/recordings"
            });
            logger.LogInformation("Seeded local storage location.");
        }

        // Seed talk groups if missing
        if (!await db.TalkGroups.AnyAsync())
        {
            var groups = new[]
            {
                new Entities.TalkGroup { Number = "1", Name = "Dispatch", AlphaTag = "DSP" },
                new Entities.TalkGroup { Number = "2", Name = "Fire", AlphaTag = "FIRE" },
                new Entities.TalkGroup { Number = "3", Name = "EMS", AlphaTag = "EMS" },
                new Entities.TalkGroup { Number = "4", Name = "Police", AlphaTag = "POL" },
                new Entities.TalkGroup { Number = "5", Name = "Utility", AlphaTag = "UTIL" },
            };
            db.TalkGroups.AddRange(groups);
            logger.LogInformation("Seeded {Count} talk groups.", groups.Length);
        }

        await db.SaveChangesAsync();
    }

    // Dimension must match the configured embedding model (all-MiniLM-L6-v2 = 384).
    private const int EmbeddingDimension = 384;

    private static async Task ApplySearchIndexesAsync(SignalRadioDbContext db, ILogger logger)
    {
        // Each statement runs as its own command to avoid Npgsql multi-statement
        // batch locking issues that can hang subsequent queries on the same context.
        var statements = new[]
        {
            "ALTER TABLE \"Transcriptions\" ADD COLUMN IF NOT EXISTS \"SearchVector\" tsvector GENERATED ALWAYS AS (to_tsvector('english', coalesce(\"FullText\", ''))) STORED",
            "CREATE INDEX IF NOT EXISTS \"IX_Transcriptions_SearchVector\" ON \"Transcriptions\" USING GIN (\"SearchVector\")",
            "ALTER TABLE \"TranscriptSummaries\" ADD COLUMN IF NOT EXISTS \"SearchVector\" tsvector GENERATED ALWAYS AS (to_tsvector('english', coalesce(\"Summary\", ''))) STORED",
            "CREATE INDEX IF NOT EXISTS \"IX_TranscriptSummaries_SearchVector\" ON \"TranscriptSummaries\" USING GIN (\"SearchVector\")",
            "ALTER TABLE \"NotableIncidents\" ADD COLUMN IF NOT EXISTS \"SearchVector\" tsvector GENERATED ALWAYS AS (to_tsvector('english', coalesce(\"Description\", ''))) STORED",
            "CREATE INDEX IF NOT EXISTS \"IX_NotableIncidents_SearchVector\" ON \"NotableIncidents\" USING GIN (\"SearchVector\")",
            "ALTER TABLE \"Topics\" ADD COLUMN IF NOT EXISTS \"SearchVector\" tsvector GENERATED ALWAYS AS (to_tsvector('english', coalesce(\"Name\", ''))) STORED",
            "CREATE INDEX IF NOT EXISTS \"IX_Topics_SearchVector\" ON \"Topics\" USING GIN (\"SearchVector\")",
            $"ALTER TABLE \"Transcriptions\" ADD COLUMN IF NOT EXISTS \"Embedding\" vector({EmbeddingDimension})",
            "CREATE INDEX IF NOT EXISTS \"IX_Transcriptions_Embedding\" ON \"Transcriptions\" USING hnsw (\"Embedding\" vector_cosine_ops)",
            $"ALTER TABLE \"TranscriptSummaries\" ADD COLUMN IF NOT EXISTS \"Embedding\" vector({EmbeddingDimension})",
            "CREATE INDEX IF NOT EXISTS \"IX_TranscriptSummaries_Embedding\" ON \"TranscriptSummaries\" USING hnsw (\"Embedding\" vector_cosine_ops)",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"UX_Transcriptions_FinalPerRecording\" ON \"Transcriptions\" (\"RecordingId\") WHERE \"IsFinal\""
        };

        try
        {
            foreach (var sql in statements)
            {
                await db.Database.ExecuteSqlRawAsync(sql);
            }
            logger.LogInformation("Search indexes (tsvector GIN + vector HNSW) applied.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to apply search indexes (may already exist or vector extension not available).");
        }
    }
}
