using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRadio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Full-text indexes have been moved to separate migrations for better reliability:
            // - AddTranscriptSummariesFullTextIndex
            // - AddNotableIncidentsFullTextIndex  
            // - AddTopicsFullTextIndex
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Full-text indexes are handled by their respective separate migrations
        }
    }
}
