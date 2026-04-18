using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SignalRadio.DataAccess;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    [DbContext(typeof(SignalRadioDbContext))]
    [Migration("20260417000000_AddRecordingsTranscriptionQueryIndexes")]
    public partial class AddRecordingsTranscriptionQueryIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Covering index on Recordings(CallId) — eliminates key lookups when joining to Calls.
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS [IX_Recordings_CallId] ON [dbo].[Recordings];

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Recordings_CallId_Covering' AND object_id = OBJECT_ID('Recordings'))
                    CREATE INDEX [IX_Recordings_CallId_Covering]
                    ON [dbo].[Recordings] ([CallId])
                    INCLUDE ([ReceivedAtUtc], [StorageLocationId], [FileName], [SizeBytes], [IsProcessed]);
            ");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS [IX_Recordings_CallId_Covering] ON [dbo].[Recordings];
            ");
        }
    }
}
