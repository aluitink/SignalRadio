using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotableIncidentsFullTextIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create full-text index on NotableIncidents.Description
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('NotableIncidents'))
                BEGIN
                    CREATE FULLTEXT INDEX ON NotableIncidents(Description LANGUAGE 'English')
                    KEY INDEX PK_NotableIncidents
                    ON FTC_Transcriptions;
                END
            ", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('NotableIncidents'))
                BEGIN
                    DROP FULLTEXT INDEX ON NotableIncidents;
                END
            ", suppressTransaction: true);
        }
    }
}
