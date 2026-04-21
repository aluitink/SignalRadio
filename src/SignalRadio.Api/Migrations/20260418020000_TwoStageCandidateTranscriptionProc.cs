using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SignalRadio.DataAccess;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    [DbContext(typeof(SignalRadioDbContext))]
    [Migration("20260418020000_TwoStageCandidateTranscriptionProc")]
    public partial class TwoStageCandidateTranscriptionProc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_GetRecordingsNeedingTranscription]
                    @Limit              INT = 10,
                    @MinDurationSeconds INT = 1,    -- exclude sub-second noise bursts entirely
                    @LookbackHours      INT = 168   -- candidate window; default 7 days
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- ---------------------------------------------------------------
                    -- Stage 1 — build a small, bounded candidate set.
                    --
                    -- Drives from IX_Calls_RecordingTimeUtc (time-range seek), then
                    -- nested-loop joins to Recordings and NOT EXISTS probe.
                    -- Capping at 50 rows means stage 2 always sorts in memory.
                    --
                    -- Parameters let the caller shrink the window further if needed:
                    --   @MinDurationSeconds: skip noise (0-second squelch breaks etc.)
                    --   @LookbackHours:      ignore stale backlog beyond this horizon
                    -- ---------------------------------------------------------------
                    SELECT TOP (50)
                        r.Id,
                        r.CallId,
                        r.StorageLocationId,
                        r.FileName,
                        r.SizeBytes,
                        r.ReceivedAtUtc,
                        c.DurationSeconds,
                        c.RecordingTimeUtc,
                        c.TalkGroupId
                    INTO #Candidates
                    FROM Calls c
                    INNER JOIN Recordings r ON r.CallId = c.Id
                    WHERE c.RecordingTimeUtc >= DATEADD(HOUR, -@LookbackHours, GETUTCDATE())
                      AND c.DurationSeconds  >= @MinDurationSeconds
                      AND r.IsProcessed = 0
                      AND NOT EXISTS (
                              SELECT 1 FROM Transcriptions t
                              WHERE t.RecordingId = r.Id
                                AND t.IsFinal = 1
                          )
                    ORDER BY c.RecordingTimeUtc DESC;   -- newest-first so most-recent calls win ties

                    -- ---------------------------------------------------------------
                    -- Stage 2 — score and rank the tiny candidate set.
                    --
                    -- Sorting <=50 rows is trivially in-memory regardless of @Limit.
                    -- Scoring: EffPriority * (1 + age_hours * 0.1)
                    --   -> lower score = sooner
                    --   -> priority-1 ties fresh priority-2 after 10 hours
                    -- ---------------------------------------------------------------
                    SELECT TOP (@Limit)
                        cand.Id,
                        cand.CallId,
                        cand.StorageLocationId,
                        cand.FileName,
                        cand.SizeBytes,
                        cand.ReceivedAtUtc,
                        CAST(0 AS BIT) AS IsProcessed
                    FROM   #Candidates cand
                    INNER JOIN TalkGroups tg ON tg.Id = cand.TalkGroupId
                    ORDER BY
                        CAST(ISNULL(tg.Priority, 2147483647) AS FLOAT)
                            * (1.0 + DATEDIFF(SECOND, cand.RecordingTimeUtc, GETUTCDATE()) / 3600.0 * 0.1) ASC,
                        cand.DurationSeconds DESC;

                    DROP TABLE #Candidates;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the blended-priority single-query version from 20260418000000
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_GetRecordingsNeedingTranscription]
                    @Limit INT = 10
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP (@Limit)
                        r.Id, r.CallId, r.StorageLocationId, r.FileName,
                        r.SizeBytes, r.ReceivedAtUtc, r.IsProcessed
                    FROM Recordings r
                    INNER JOIN Calls      c  ON c.Id  = r.CallId
                    INNER JOIN TalkGroups tg ON tg.Id = c.TalkGroupId
                    WHERE r.IsProcessed = 0
                      AND NOT EXISTS (
                        SELECT 1 FROM Transcriptions t
                        WHERE t.RecordingId = r.Id AND t.IsFinal = 1
                    )
                    ORDER BY
                        CASE WHEN c.DurationSeconds < 1 THEN 1 ELSE 0 END ASC,
                        CAST(ISNULL(tg.Priority, 2147483647) AS FLOAT)
                            * (1.0 + DATEDIFF(SECOND, c.RecordingTimeUtc, GETUTCDATE()) / 3600.0 * 0.5) ASC,
                        c.DurationSeconds DESC;
                END
            ");
        }
    }
}
