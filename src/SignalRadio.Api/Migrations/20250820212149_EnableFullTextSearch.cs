using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnableFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing indexes first
            migrationBuilder.DropIndex(
                name: "IX_Recordings_HasTranscription_IsUploaded_Format",
                table: "Recordings");

            migrationBuilder.DropIndex(
                name: "IX_Recordings_TranscriptionProcessedAt",
                table: "Recordings");

            // Create Full Text Catalog if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT name FROM sys.fulltext_catalogs WHERE name = 'SignalRadio_FTCatalog')
                BEGIN
                    CREATE FULLTEXT CATALOG SignalRadio_FTCatalog AS DEFAULT;
                END
            ");

            // Create Full Text Index on TranscriptionText column
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Recordings'))
                BEGIN
                    CREATE FULLTEXT INDEX ON Recordings (TranscriptionText LANGUAGE 1033)
                    KEY INDEX PK_Recordings
                    ON SignalRadio_FTCatalog
                    WITH CHANGE_TRACKING AUTO;
                END
            ");

            // Recreate the dropped indexes
            migrationBuilder.CreateIndex(
                name: "IX_Recordings_HasTranscription_IsUploaded_Format",
                table: "Recordings",
                columns: new[] { "HasTranscription", "IsUploaded", "Format" });

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_TranscriptionProcessedAt",
                table: "Recordings",
                column: "TranscriptionProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the Full Text Index
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Recordings'))
                BEGIN
                    DROP FULLTEXT INDEX ON Recordings;
                END
            ");

            // Drop the Full Text Catalog
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT name FROM sys.fulltext_catalogs WHERE name = 'SignalRadio_FTCatalog')
                BEGIN
                    DROP FULLTEXT CATALOG SignalRadio_FTCatalog;
                END
            ");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Recordings_HasTranscription_IsUploaded_Format",
                table: "Recordings");

            migrationBuilder.DropIndex(
                name: "IX_Recordings_TranscriptionProcessedAt",
                table: "Recordings");

            // Recreate original indexes
            migrationBuilder.CreateIndex(
                name: "IX_Recordings_HasTranscription_IsUploaded_Format",
                table: "Recordings",
                columns: new[] { "HasTranscription", "IsUploaded", "Format" });

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_TranscriptionProcessedAt",
                table: "Recordings",
                column: "TranscriptionProcessedAt");
        }
    }
}
