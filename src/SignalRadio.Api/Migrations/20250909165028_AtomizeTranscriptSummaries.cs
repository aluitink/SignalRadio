using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AtomizeTranscriptSummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TranscriptSummaryTopics_Topic",
                table: "TranscriptSummaryTopics");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "TranscriptSummaryTopics");

            migrationBuilder.AddColumn<int>(
                name: "TopicId",
                table: "TranscriptSummaryTopics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "NotableIncidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImportanceScore = table.Column<double>(type: "float", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotableIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotableIncidentCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotableIncidentId = table.Column<int>(type: "int", nullable: false),
                    CallId = table.Column<int>(type: "int", nullable: false),
                    CallNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotableIncidentCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotableIncidentCalls_Calls_CallId",
                        column: x => x.CallId,
                        principalTable: "Calls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotableIncidentCalls_NotableIncidents_NotableIncidentId",
                        column: x => x.NotableIncidentId,
                        principalTable: "NotableIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptSummaryNotableIncidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TranscriptSummaryId = table.Column<int>(type: "int", nullable: false),
                    NotableIncidentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptSummaryNotableIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptSummaryNotableIncidents_NotableIncidents_NotableIncidentId",
                        column: x => x.NotableIncidentId,
                        principalTable: "NotableIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TranscriptSummaryNotableIncidents_TranscriptSummaries_TranscriptSummaryId",
                        column: x => x.TranscriptSummaryId,
                        principalTable: "TranscriptSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryTopics_TopicId",
                table: "TranscriptSummaryTopics",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryTopics_TranscriptSummaryId_TopicId",
                table: "TranscriptSummaryTopics",
                columns: new[] { "TranscriptSummaryId", "TopicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotableIncidentCalls_CallId",
                table: "NotableIncidentCalls",
                column: "CallId");

            migrationBuilder.CreateIndex(
                name: "IX_NotableIncidentCalls_NotableIncidentId",
                table: "NotableIncidentCalls",
                column: "NotableIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_NotableIncidentCalls_NotableIncidentId_CallId",
                table: "NotableIncidentCalls",
                columns: new[] { "NotableIncidentId", "CallId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotableIncidents_ImportanceScore",
                table: "NotableIncidents",
                column: "ImportanceScore");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Category",
                table: "Topics",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Name",
                table: "Topics",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryNotableIncidents_NotableIncidentId",
                table: "TranscriptSummaryNotableIncidents",
                column: "NotableIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryNotableIncidents_TranscriptSummaryId",
                table: "TranscriptSummaryNotableIncidents",
                column: "TranscriptSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryNotableIncidents_TranscriptSummaryId_NotableIncidentId",
                table: "TranscriptSummaryNotableIncidents",
                columns: new[] { "TranscriptSummaryId", "NotableIncidentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TranscriptSummaryTopics_Topics_TopicId",
                table: "TranscriptSummaryTopics",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TranscriptSummaryTopics_Topics_TopicId",
                table: "TranscriptSummaryTopics");

            migrationBuilder.DropTable(
                name: "NotableIncidentCalls");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "TranscriptSummaryNotableIncidents");

            migrationBuilder.DropTable(
                name: "NotableIncidents");

            migrationBuilder.DropIndex(
                name: "IX_TranscriptSummaryTopics_TopicId",
                table: "TranscriptSummaryTopics");

            migrationBuilder.DropIndex(
                name: "IX_TranscriptSummaryTopics_TranscriptSummaryId_TopicId",
                table: "TranscriptSummaryTopics");

            migrationBuilder.DropColumn(
                name: "TopicId",
                table: "TranscriptSummaryTopics");

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "TranscriptSummaryTopics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSummaryTopics_Topic",
                table: "TranscriptSummaryTopics",
                column: "Topic");
        }
    }
}
