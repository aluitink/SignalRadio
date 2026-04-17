using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SignalRadio.DataAccess;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    [DbContext(typeof(SignalRadioDbContext))]
    [Migration("20260417010000_UpdateGetRecordingsNeedingTranscriptionProcCrossApply")]
    public partial class UpdateGetRecordingsNeedingTranscriptionProcCrossApply : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_GetRecordingsNeedingTranscription]
                    @Limit INT = 10
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP (@Limit)
                        r.Id, r.CallId, r.StorageLocationId, r.FileName,
                        r.SizeBytes, r.ReceivedAtUtc, r.IsProcessed
                    FROM (
                        -- 409 rows; drives the entire plan; scanned once
                        SELECT Id AS TalkGroupId, ISNULL(Priority, 2147483647) AS EffPriority
                        FROM TalkGroups
                    ) tg
                    CROSS APPLY (
                        -- Per-group seek: uses IX_Calls_TalkGroupId_RecordingTimeUtc ordered
                        -- backward (RecordingTimeUtc DESC) so TOP can terminate early.
                        SELECT TOP (@Limit)
                            r.Id, r.CallId, r.StorageLocationId, r.FileName,
                            r.SizeBytes, r.ReceivedAtUtc, r.IsProcessed
                        FROM Calls c
                        INNER JOIN Recordings r ON r.CallId = c.Id
                        WHERE c.TalkGroupId = tg.TalkGroupId
                          AND NOT EXISTS (
                                  SELECT 1 FROM Transcriptions t
                                  WHERE t.RecordingId = r.Id
                                    AND t.IsFinal = 1
                              )
                        ORDER BY c.RecordingTimeUtc DESC
                    ) r
                    ORDER BY tg.EffPriority ASC, r.ReceivedAtUtc DESC;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_GetRecordingsNeedingTranscription];");
        }
    }
}
