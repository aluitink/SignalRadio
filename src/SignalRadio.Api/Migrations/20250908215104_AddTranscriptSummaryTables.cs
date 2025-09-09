using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriptSummaryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TranscriptSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TalkGroupId = table.Column<int>(type: "int", nullable: false),
                    StartTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TranscriptCount = table.Column<int>(type: "int", nullable: false),
                    TotalDurationSeconds = table.Column<double>(type: "float", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptSummaries_TalkGroups_TalkGroupId",
                        column: x => x.TalkGroupId,
                        principalTable: "TalkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptSummaryNotableCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TranscriptSummaryId = table.Column<int>(type: "int", nullable: false),
                    CallId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImportanceScore = table.Column<double>(type: "float", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "TranscriptSummaryTopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TranscriptSummaryId = table.Column<int>(type: "int", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Relevance = table.Column<double>(type: "float", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptSummaryTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptSummaryTopics_TranscriptSummaries_TranscriptSummaryId",
                        column: x => x.TranscriptSummaryId,
                        principalTable: "TranscriptSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaries_TalkGroupId_GeneratedAtUtc",
                table: "TranscriptSummaries",
                columns: new[] { "TalkGroupId", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaries_TalkGroupId_StartTimeUtc_EndTimeUtc",
                table: "TranscriptSummaries",
                columns: new[] { "TalkGroupId", "StartTimeUtc", "EndTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryNotableCalls_CallId",
                table: "TranscriptSummaryNotableCalls",
                column: "CallId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryNotableCalls_TranscriptSummaryId",
                table: "TranscriptSummaryNotableCalls",
                column: "TranscriptSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryTopics_Topic",
                table: "TranscriptSummaryTopics",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryTopics_TranscriptSummaryId",
                table: "TranscriptSummaryTopics",
                column: "TranscriptSummaryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TranscriptSummaryNotableCalls");

            migrationBuilder.DropTable(
                name: "TranscriptSummaryTopics");

            migrationBuilder.DropTable(
                name: "TranscriptSummaries");
        }
    }
}
