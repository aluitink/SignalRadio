using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SignalRadio.DataAccess;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    [DbContext(typeof(SignalRadioDbContext))]
    [Migration("20260418030000_ExtendCallsRecordingTimeIndex")]
    public partial class ExtendCallsRecordingTimeIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The two-stage SP seeks IX_Calls_RecordingTimeUtc for the time-range, then
            // performs a key lookup into PK_Calls solely to fetch DurationSeconds for the
            // "DurationSeconds >= @MinDurationSeconds" residual predicate (NodeId=13, ~754 lookups).
            //
            // SQL Server's missing-index advisor recommends ([RecordingTimeUtc],[DurationSeconds])
            // as composite key columns with TalkGroupId INCLUDEd (29.8% estimated cost reduction).
            // Making DurationSeconds a key column (rather than INCLUDE) lets the optimizer skip
            // non-qualifying rows during the seek itself rather than as a post-seek residual filter.
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS [IX_Calls_RecordingTimeUtc] ON [dbo].[Calls];

                CREATE INDEX [IX_Calls_RecordingTimeUtc]
                ON [dbo].[Calls] ([RecordingTimeUtc], [DurationSeconds])
                INCLUDE ([TalkGroupId]);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS [IX_Calls_RecordingTimeUtc] ON [dbo].[Calls];

                CREATE INDEX [IX_Calls_RecordingTimeUtc]
                ON [dbo].[Calls] ([RecordingTimeUtc])
                INCLUDE ([TalkGroupId]);
            ");
        }
    }
}
