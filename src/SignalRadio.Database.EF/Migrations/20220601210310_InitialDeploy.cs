using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Database.EF.Migrations
{
    public partial class InitialDeploy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "RadioRecorders",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecorderIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceNumber = table.Column<int>(type: "int", nullable: false),
                    RecorderNumber = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false),
                    Duration = table.Column<float>(type: "real", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    StatusLength = table.Column<long>(type: "bigint", nullable: false),
                    StatusError = table.Column<int>(type: "int", nullable: false),
                    StatusSpike = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioRecorders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RadioSystems",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    County = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NAC = table.Column<int>(type: "int", nullable: false),
                    WANC = table.Column<int>(type: "int", nullable: false),
                    SystemNumber = table.Column<int>(type: "int", nullable: false),
                    SystemType = table.Column<byte>(type: "tinyint", nullable: false),
                    SystemVoice = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioSystems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RadioFrequencies",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RadioSystemId = table.Column<long>(type: "bigint", nullable: false),
                    FrequencyHz = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    ControlData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioFrequencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioFrequencies_RadioSystems_RadioSystemId",
                        column: x => x.RadioSystemId,
                        principalSchema: "dbo",
                        principalTable: "RadioSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RadioGroups",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RadioSystemId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioGroups_RadioSystems_RadioSystemId",
                        column: x => x.RadioSystemId,
                        principalSchema: "dbo",
                        principalTable: "RadioSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MountPoints",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    StreamId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Host = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Port = table.Column<long>(type: "bigint", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MountPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MountPoints_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TalkGroups",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RadioGroupId = table.Column<long>(type: "bigint", nullable: true),
                    RadioSystemId = table.Column<long>(type: "bigint", nullable: true),
                    Identifier = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<byte>(type: "tinyint", nullable: false),
                    Tag = table.Column<byte>(type: "tinyint", nullable: false),
                    AlphaTag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalkGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalkGroups_RadioGroups_RadioGroupId",
                        column: x => x.RadioGroupId,
                        principalSchema: "dbo",
                        principalTable: "RadioGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TalkGroups_RadioSystems_RadioSystemId",
                        column: x => x.RadioSystemId,
                        principalSchema: "dbo",
                        principalTable: "RadioSystems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Streams",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MountPointId = table.Column<long>(type: "bigint", nullable: true),
                    StreamIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Genra = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastCallTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Streams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Streams_MountPoints_MountPointId",
                        column: x => x.MountPointId,
                        principalSchema: "dbo",
                        principalTable: "MountPoints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Streams_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RadioCalls",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallState = table.Column<int>(type: "int", nullable: false),
                    CallRecordState = table.Column<int>(type: "int", nullable: false),
                    CallIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CallSerialNumber = table.Column<long>(type: "bigint", nullable: false),
                    TalkGroupId = table.Column<long>(type: "bigint", nullable: false),
                    Elapsed = table.Column<long>(type: "bigint", nullable: false),
                    IsPhase2 = table.Column<bool>(type: "bit", nullable: false),
                    IsConventional = table.Column<bool>(type: "bit", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    IsAnalog = table.Column<bool>(type: "bit", nullable: false),
                    IsEmergency = table.Column<bool>(type: "bit", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StopTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Frequency = table.Column<long>(type: "bigint", nullable: false),
                    CallWavPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioCalls_TalkGroups_TalkGroupId",
                        column: x => x.TalkGroupId,
                        principalSchema: "dbo",
                        principalTable: "TalkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TalkGroupStreams",
                schema: "dbo",
                columns: table => new
                {
                    TalkGroupId = table.Column<long>(type: "bigint", nullable: false),
                    StreamId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalkGroupStreams", x => new { x.TalkGroupId, x.StreamId });
                    table.ForeignKey(
                        name: "FK_TalkGroupStreams_Streams_StreamId",
                        column: x => x.StreamId,
                        principalSchema: "dbo",
                        principalTable: "Streams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TalkGroupStreams_TalkGroups_TalkGroupId",
                        column: x => x.TalkGroupId,
                        principalSchema: "dbo",
                        principalTable: "TalkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MountPoints_UserId",
                schema: "dbo",
                table: "MountPoints",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioCalls_TalkGroupId",
                schema: "dbo",
                table: "RadioCalls",
                column: "TalkGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioFrequencies_RadioSystemId",
                schema: "dbo",
                table: "RadioFrequencies",
                column: "RadioSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioGroups_RadioSystemId",
                schema: "dbo",
                table: "RadioGroups",
                column: "RadioSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Streams_MountPointId",
                schema: "dbo",
                table: "Streams",
                column: "MountPointId",
                unique: true,
                filter: "[MountPointId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Streams_OwnerUserId",
                schema: "dbo",
                table: "Streams",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TalkGroups_RadioGroupId",
                schema: "dbo",
                table: "TalkGroups",
                column: "RadioGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TalkGroups_RadioSystemId",
                schema: "dbo",
                table: "TalkGroups",
                column: "RadioSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_TalkGroupStreams_StreamId",
                schema: "dbo",
                table: "TalkGroupStreams",
                column: "StreamId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RadioCalls",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RadioFrequencies",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RadioRecorders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TalkGroupStreams",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Streams",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TalkGroups",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MountPoints",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RadioGroups",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RadioSystems",
                schema: "dbo");
        }
    }
}
