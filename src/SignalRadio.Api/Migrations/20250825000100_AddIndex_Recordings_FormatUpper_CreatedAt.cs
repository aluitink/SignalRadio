using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIndex_Recordings_FormatUpper_CreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add persisted computed column for UPPER(Format) so predicate can be sargable
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Recordings','FormatUpper') IS NULL
BEGIN
    ALTER TABLE dbo.Recordings ADD FormatUpper AS (UPPER([Format])) PERSISTED;
END");

            // Drop the previous index if it exists (quick index from earlier attempt)
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Recordings_Filter_CreatedAtDesc' AND object_id = OBJECT_ID('dbo.Recordings'))
BEGIN
    DROP INDEX IX_Recordings_Filter_CreatedAtDesc ON dbo.Recordings;
END");

            // Create better-ordered index: equality cols first, then FormatUpper, then CreatedAt (DESC) for ORDER BY, then TranscriptionAttempts (range)
            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Recordings_Filter_FormatUpper_CreatedAt' AND object_id = OBJECT_ID('dbo.Recordings'))
BEGIN
CREATE NONCLUSTERED INDEX IX_Recordings_Filter_FormatUpper_CreatedAt
ON dbo.Recordings (HasTranscription, IsUploaded, FormatUpper, CreatedAt DESC, TranscriptionAttempts)
INCLUDE (Id, CallId);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Recordings_Filter_FormatUpper_CreatedAt' AND object_id = OBJECT_ID('dbo.Recordings'))
BEGIN
    DROP INDEX IX_Recordings_Filter_FormatUpper_CreatedAt ON dbo.Recordings;
END");

            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Recordings','FormatUpper') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Recordings DROP COLUMN FormatUpper;
END");
        }
    }
}
