using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasTranscription",
                table: "Recordings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TranscriptionText",
                table: "Recordings",
                type: "nvarchar(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TranscriptionConfidence",
                table: "Recordings",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranscriptionLanguage",
                table: "Recordings",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TranscriptionProcessedAt",
                table: "Recordings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranscriptionSegments",
                table: "Recordings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TranscriptionAttempts",
                table: "Recordings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastTranscriptionError",
                table: "Recordings",
                type: "nvarchar(max)",
                nullable: true);

            // Create index for efficient querying of recordings needing transcription
            migrationBuilder.CreateIndex(
                name: "IX_Recordings_HasTranscription_IsUploaded_Format",
                table: "Recordings",
                columns: new[] { "HasTranscription", "IsUploaded", "Format" });

            // Create index for transcription status queries
            migrationBuilder.CreateIndex(
                name: "IX_Recordings_TranscriptionProcessedAt",
                table: "Recordings",
                column: "TranscriptionProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recordings_HasTranscription_IsUploaded_Format",
                table: "Recordings");

            migrationBuilder.DropIndex(
                name: "IX_Recordings_TranscriptionProcessedAt",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "HasTranscription",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "TranscriptionText",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "TranscriptionConfidence",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "TranscriptionLanguage",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "TranscriptionProcessedAt",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "TranscriptionSegments",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "TranscriptionAttempts",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "LastTranscriptionError",
                table: "Recordings");
        }
    }
}
