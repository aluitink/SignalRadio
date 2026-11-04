using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data.Entities;

namespace SignalRadio.Data.Services;

public class SearchService(SignalRadioDbContext db, IAiClient aiClient) : ISearchService
{
    public async Task<SearchResultsDto> SearchAsync(string query, int page, int pageSize, bool semantic, CancellationToken ct = default)
    {
        var transcriptionResults = new List<TranscriptionSearchResultDto>();
        var summaryResults = new List<TranscriptSummaryDto>();
        var incidentResults = new List<NotableIncidentDto>();
        var totalCount = 0;

        if (semantic && aiClient.AreEmbeddingsEnabled)
        {
            // Semantic search via pgvector cosine distance
            var queryEmbedding = await aiClient.EmbedAsync(query, ct);
            var vectorLiteral = ToVectorLiteral(queryEmbedding);

            var semanticSql = $@"
                SELECT t.""Id"" AS ""TranscriptionId"", t.""RecordingId"", t.""FullText"", t.""Confidence"", t.""Service"",
                       r.""CallId"", c.""TalkGroupId"", c.""RecordingTime"",
                       tg.""Number"" AS ""TalkGroupNumber"", tg.""Name"" AS ""TalkGroupName"",
                       1.0 - (t.""Embedding"" <=> '{vectorLiteral}'::vector) AS ""RelevanceScore""
                FROM ""Transcriptions"" t
                JOIN ""Recordings"" r ON r.""Id"" = t.""RecordingId""
                JOIN ""Calls"" c ON c.""Id"" = r.""CallId""
                JOIN ""TalkGroups"" tg ON tg.""Id"" = c.""TalkGroupId""
                WHERE t.""Embedding"" IS NOT NULL
                ORDER BY t.""Embedding"" <=> '{vectorLiteral}'::vector
                LIMIT {pageSize}";

            var semanticResults = await db.Database
                .SqlQueryRaw<TranscriptionSearchResultDto>(semanticSql)
                .ToListAsync(ct);

            transcriptionResults = semanticResults;
            totalCount = semanticResults.Count;
        }
        else
        {
            // Keyword search via tsvector GIN index
            var keywordSql = $@"
                SELECT t.""Id"" AS ""TranscriptionId"", t.""RecordingId"", t.""FullText"", t.""Confidence"", t.""Service"",
                       r.""CallId"", c.""TalkGroupId"", c.""RecordingTime"",
                       tg.""Number"" AS ""TalkGroupNumber"", tg.""Name"" AS ""TalkGroupName"",
                       1.0 AS ""RelevanceScore""
                FROM ""Transcriptions"" t
                JOIN ""Recordings"" r ON r.""Id"" = t.""RecordingId""
                JOIN ""Calls"" c ON c.""Id"" = r.""CallId""
                JOIN ""TalkGroups"" tg ON tg.""Id"" = c.""TalkGroupId""
                WHERE t.""SearchVector"" @@ plainto_tsquery('english', @p0)
                ORDER BY t.""CreatedAt"" DESC
                LIMIT {pageSize} OFFSET {(page - 1) * pageSize}";

            transcriptionResults = await db.Database
                .SqlQueryRaw<TranscriptionSearchResultDto>(keywordSql, query)
                .ToListAsync(ct);

            var countSql = @"
                SELECT COUNT(*) AS ""Value""
                FROM ""Transcriptions"" t
                WHERE t.""SearchVector"" @@ plainto_tsquery('english', @p0)";

            totalCount = await db.Database
                .SqlQueryRaw<int>(countSql, query)
                .FirstAsync(ct);
        }

        return new SearchResultsDto(transcriptionResults, summaryResults, incidentResults, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<TranscriptSummaryDto>> SearchSummariesAsync(string query, int count, CancellationToken ct = default)
    {
        var sql = $@"
            SELECT s.""Id"", s.""TalkGroupId"", s.""StartTime"", s.""EndTime"", s.""TranscriptCount"",
                   s.""TotalDurationSeconds"", s.""Summary"", s.""GeneratedAt"", s.""CreatedAt"",
                   tg.""Number"", tg.""Name""
            FROM ""TranscriptSummaries"" s
            JOIN ""TalkGroups"" tg ON tg.""Id"" = s.""TalkGroupId""
            WHERE s.""SearchVector"" @@ plainto_tsquery('english', @query)
            ORDER BY s.""StartTime"" DESC
            LIMIT {count}";

        var results = await db.Database
            .SqlQueryRaw<TranscriptSummarySearchRow>(sql, query)
            .ToListAsync(ct);

        return results.Select(r => new TranscriptSummaryDto(
            r.Id, r.TalkGroupId, r.Number, r.Name,
            r.StartTime, r.EndTime, r.TranscriptCount, r.TotalDurationSeconds,
            r.Summary, r.GeneratedAt.UtcDateTime,
            [], [])).ToList();
    }

    public async Task<IReadOnlyList<NotableIncidentDto>> SearchIncidentsAsync(string query, int count, CancellationToken ct = default)
    {
        var sql = $@"
            SELECT n.""Id"", n.""Description"", n.""ImportanceScore"", n.""CreatedAt""
            FROM ""NotableIncidents"" n
            WHERE n.""SearchVector"" @@ plainto_tsquery('english', @query)
            ORDER BY n.""ImportanceScore"" DESC
            LIMIT {count}";

        var results = await db.Database
            .SqlQueryRaw<NotableIncidentDto>(sql, query)
            .ToListAsync(ct);

        return results;
    }

    private static string ToVectorLiteral(float[] vector)
    {
        return "[" + string.Join(", ", vector.Select(v => v.ToString("R"))) + "]";
    }
}

// Helper row type for summary search
public record TranscriptSummarySearchRow(
    long Id, int TalkGroupId, string Number, string? Name,
    DateTimeOffset StartTime, DateTimeOffset EndTime, int TranscriptCount,
    double TotalDurationSeconds, string Summary, DateTimeOffset GeneratedAt, DateTimeOffset CreatedAt);
