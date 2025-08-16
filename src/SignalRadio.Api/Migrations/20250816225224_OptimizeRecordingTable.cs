using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeRecordingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update field lengths for better optimization
            migrationBuilder.AlterColumn<string>(
                name: "Format",
                table: "Recordings",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "Recordings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "BlobUri",
                table: "Recordings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BlobName",
                table: "Recordings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            // Add new columns
            migrationBuilder.AddColumn<int>(
                name: "Bitrate",
                table: "Recordings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Channels",
                table: "Recordings",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "Recordings",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "Recordings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastUploadError",
                table: "Recordings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Quality",
                table: "Recordings",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SampleRate",
                table: "Recordings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UploadAttempts",
                table: "Recordings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Add indexes for better performance
            migrationBuilder.CreateIndex(
                name: "IX_Recordings_FileHash",
                table: "Recordings",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_Quality",
                table: "Recordings",
                column: "Quality");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_Duration",
                table: "Recordings",
                column: "Duration");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_UploadAttempts",
                table: "Recordings",
                column: "UploadAttempts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Recordings_FileHash",
                table: "Recordings");

            migrationBuilder.DropIndex(
                name: "IX_Recordings_Quality",
                table: "Recordings");

            migrationBuilder.DropIndex(
                name: "IX_Recordings_Duration",
                table: "Recordings");

            migrationBuilder.DropIndex(
                name: "IX_Recordings_UploadAttempts",
                table: "Recordings");

            // Revert field lengths
            migrationBuilder.AlterColumn<string>(
                name: "Format",
                table: "Recordings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "Recordings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "BlobUri",
                table: "Recordings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BlobName",
                table: "Recordings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            // Drop new columns
            migrationBuilder.DropColumn(
                name: "Bitrate",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "Channels",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "LastUploadError",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "Quality",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "SampleRate",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "UploadAttempts",
                table: "Recordings");
        }
    }
}
