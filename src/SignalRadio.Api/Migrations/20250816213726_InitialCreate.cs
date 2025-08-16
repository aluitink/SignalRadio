using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Calls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TalkgroupId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SystemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecordingTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recordings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    BlobUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BlobName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsUploaded = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calls_RecordingTime",
                table: "Calls",
                column: "RecordingTime");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_SystemName",
                table: "Calls",
                column: "SystemName");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_TalkgroupId",
                table: "Calls",
                column: "TalkgroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_TalkgroupId_SystemName_RecordingTime",
                table: "Calls",
                columns: new[] { "TalkgroupId", "SystemName", "RecordingTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_CallId",
                table: "Recordings",
                column: "CallId");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_Format",
                table: "Recordings",
                column: "Format");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_IsUploaded",
                table: "Recordings",
                column: "IsUploaded");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recordings");

            migrationBuilder.DropTable(
                name: "Calls");
        }
    }
}
