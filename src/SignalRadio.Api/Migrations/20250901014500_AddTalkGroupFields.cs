using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTalkGroupFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlphaTag",
                table: "TalkGroups",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "TalkGroups",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TalkGroups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "TalkGroups",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TalkGroups_AlphaTag",
                table: "TalkGroups",
                column: "AlphaTag");

            migrationBuilder.CreateIndex(
                name: "IX_TalkGroups_Category",
                table: "TalkGroups",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TalkGroups_Tag",
                table: "TalkGroups",
                column: "Tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TalkGroups_AlphaTag",
                table: "TalkGroups");

            migrationBuilder.DropIndex(
                name: "IX_TalkGroups_Category",
                table: "TalkGroups");

            migrationBuilder.DropIndex(
                name: "IX_TalkGroups_Tag",
                table: "TalkGroups");

            migrationBuilder.DropColumn(
                name: "AlphaTag",
                table: "TalkGroups");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TalkGroups");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TalkGroups");

            migrationBuilder.DropColumn(
                name: "Tag",
                table: "TalkGroups");
        }
    }
}
