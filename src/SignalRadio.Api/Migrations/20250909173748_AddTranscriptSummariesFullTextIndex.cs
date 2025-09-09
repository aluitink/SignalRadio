using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriptSummariesFullTextIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create full-text index on TranscriptSummaries.Summary
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('TranscriptSummaries'))
                BEGIN
                    CREATE FULLTEXT INDEX ON TranscriptSummaries(Summary LANGUAGE 'English')
                    KEY INDEX PK_TranscriptSummaries
                    ON FTC_Transcriptions;
                END
            ", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('TranscriptSummaries'))
                BEGIN
                    DROP FULLTEXT INDEX ON TranscriptSummaries;
                END
            ", suppressTransaction: true);
        }
    }
}
