using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SignalRadio.DataAccess;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    [DbContext(typeof(SignalRadioDbContext))]
    [Migration("20250831000000_SquashedInitialCreate")]
    public partial class SquashedInitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create tables (derived from current model)
            migrationBuilder.CreateTable(
                name: "StorageLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    LocationUri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TalkGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalkGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Calls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TalkGroupId = table.Column<int>(type: "int", nullable: false),
                    RecordingTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FrequencyHz = table.Column<double>(type: "float", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calls_TalkGroups_TalkGroupId",
                        column: x => x.TalkGroupId,
                        principalTable: "TalkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recordings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallId = table.Column<int>(type: "int", nullable: false),
                    StorageLocationId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recordings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recordings_Calls_CallId",
                        column: x => x.CallId,
                        principalTable: "Calls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recordings_StorageLocations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transcriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordingId = table.Column<int>(type: "int", nullable: false),
                    Service = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: true),
                    AdditionalDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transcriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transcriptions_Recordings_RecordingId",
                        column: x => x.RecordingId,
                        principalTable: "Recordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indexes from the model and alignment migration
            migrationBuilder.CreateIndex(
                name: "IX_Calls_TalkGroupId_RecordingTimeUtc",
                table: "Calls",
                columns: new[] { "TalkGroupId", "RecordingTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_CallId",
                table: "Recordings",
                column: "CallId");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_ReceivedAtUtc",
                table: "Recordings",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_StorageLocationId",
                table: "Recordings",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TalkGroups_Number",
                table: "TalkGroups",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_TalkGroups_Priority",
                table: "TalkGroups",
                column: "Priority");

            // Prefer {RecordingId, IsFinal} to speed final transcript lookup
            migrationBuilder.CreateIndex(
                name: "IX_Transcriptions_RecordingId_IsFinal",
                table: "Transcriptions",
                columns: new[] { "RecordingId", "IsFinal" });

            migrationBuilder.CreateIndex(
                name: "IX_Transcriptions_Service",
                table: "Transcriptions",
                column: "Service");

            // Full-text catalog and index
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'FTC_Transcriptions')
BEGIN
    CREATE FULLTEXT CATALOG [FTC_Transcriptions];
END

IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.Transcriptions'))
BEGIN
    CREATE FULLTEXT INDEX ON dbo.Transcriptions(FullText LANGUAGE 0)
    KEY INDEX PK_Transcriptions
    ON FTC_Transcriptions
    WITH CHANGE_TRACKING AUTO;
END
", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty to avoid destructive drops in a squashed baseline migration.
            // This project uses an additive baseline for development; if rollback is needed,
            // handle it manually or restore a DB backup.
        }
    }
}
