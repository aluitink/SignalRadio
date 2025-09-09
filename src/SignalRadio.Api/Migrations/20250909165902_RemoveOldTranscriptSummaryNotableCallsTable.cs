using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldTranscriptSummaryNotableCallsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TranscriptSummaryNotableCalls");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TranscriptSummaryNotableCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallId = table.Column<int>(type: "int", nullable: false),
                    TranscriptSummaryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImportanceScore = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptSummaryNotableCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptSummaryNotableCalls_Calls_CallId",
                        column: x => x.CallId,
                        principalTable: "Calls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TranscriptSummaryNotableCalls_TranscriptSummaries_TranscriptSummaryId",
                        column: x => x.TranscriptSummaryId,
                        principalTable: "TranscriptSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryNotableCalls_CallId",
                table: "TranscriptSummaryNotableCalls",
                column: "CallId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryNotableCalls_TranscriptSummaryId",
                table: "TranscriptSummaryNotableCalls",
                column: "TranscriptSummaryId");
        }
    }
}
