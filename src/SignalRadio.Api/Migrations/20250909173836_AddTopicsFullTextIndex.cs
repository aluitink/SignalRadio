using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicsFullTextIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create full-text index on Topics.Name
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Topics'))
                BEGIN
                    CREATE FULLTEXT INDEX ON Topics(Name LANGUAGE 'English')
                    KEY INDEX PK_Topics
                    ON FTC_Transcriptions;
                END
            ", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Topics'))
                BEGIN
                    DROP FULLTEXT INDEX ON Topics;
                END
            ", suppressTransaction: true);
        }
    }
}
