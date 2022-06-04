using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Database.EF.Migrations
{
    public partial class AddingRadioSources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RadioSources",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceNumber = table.Column<int>(type: "int", nullable: false),
                    Antenna = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsQPSK = table.Column<bool>(type: "bit", nullable: false),
                    SilenceFrames = table.Column<long>(type: "bigint", nullable: false),
                    AnalogLevels = table.Column<int>(type: "int", nullable: false),
                    DigitalLevels = table.Column<int>(type: "int", nullable: false),
                    MinHz = table.Column<long>(type: "bigint", nullable: false),
                    MaxHz = table.Column<long>(type: "bigint", nullable: false),
                    CenterHz = table.Column<long>(type: "bigint", nullable: false),
                    Rate = table.Column<long>(type: "bigint", nullable: false),
                    Driver = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Device = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<int>(type: "int", nullable: false),
                    MixGain = table.Column<int>(type: "int", nullable: false),
                    LnaGain = table.Column<int>(type: "int", nullable: false),
                    Vga1Gain = table.Column<int>(type: "int", nullable: false),
                    Vga2Gain = table.Column<int>(type: "int", nullable: false),
                    BBGain = table.Column<long>(type: "bigint", nullable: false),
                    Gain = table.Column<int>(type: "int", nullable: false),
                    IfGain = table.Column<int>(type: "int", nullable: false),
                    SquelchDB = table.Column<int>(type: "int", nullable: false),
                    AnalogRecorders = table.Column<int>(type: "int", nullable: false),
                    DigitalRecorders = table.Column<int>(type: "int", nullable: false),
                    DebugRecorders = table.Column<int>(type: "int", nullable: false),
                    SigmfRecorders = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioSources", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RadioSources",
                schema: "dbo");
        }
    }
}
