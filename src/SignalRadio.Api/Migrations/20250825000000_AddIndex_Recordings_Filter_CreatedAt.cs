using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIndex_Recordings_Filter_CreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Recordings_Filter_CreatedAtDesc' AND object_id = OBJECT_ID('dbo.Recordings'))
BEGIN
CREATE NONCLUSTERED INDEX IX_Recordings_Filter_CreatedAtDesc
ON dbo.Recordings (HasTranscription, IsUploaded, TranscriptionAttempts, Format, CreatedAt DESC)
INCLUDE (Id, CallId);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Recordings_Filter_CreatedAtDesc' AND object_id = OBJECT_ID('dbo.Recordings'))
BEGIN
DROP INDEX IX_Recordings_Filter_CreatedAtDesc ON dbo.Recordings;
END");
        }
    }
}
