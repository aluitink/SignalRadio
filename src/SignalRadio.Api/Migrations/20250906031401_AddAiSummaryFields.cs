using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSummaryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasSummary",
                table: "Transcriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastSummaryError",
                table: "Transcriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SummaryAttempts",
                table: "Transcriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "SummaryConfidence",
                table: "Transcriptions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SummaryGeneratedAt",
                table: "Transcriptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryModel",
                table: "Transcriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SummaryProcessingTimeMs",
                table: "Transcriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryText",
                table: "Transcriptions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasSummary",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "LastSummaryError",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "SummaryAttempts",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "SummaryConfidence",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "SummaryGeneratedAt",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "SummaryModel",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "SummaryProcessingTimeMs",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "SummaryText",
                table: "Transcriptions");
        }
    }
}
