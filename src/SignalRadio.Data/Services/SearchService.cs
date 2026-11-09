using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data.Entities;

namespace SignalRadio.Data.Services;

public class SearchService(SignalRadioDbContext db, IAiClient aiClient) : ISearchService
{
    private const double HybridAlpha = 0.7; // weight for semantic score in hybrid mode
    private const int HybridCandidateLimit = 100;

    public async Task<SearchResultsDto> SearchAsync(string query, int page, int pageSize, bool semantic, bool hybrid, CancellationToken ct = default)
    {
        var transcriptionResults = new List<TranscriptionSearchResultDto>();
        var summaryResults = new List<TranscriptSummaryDto>();
        var incidentResults = new List<NotableIncidentDto>();
        var totalCount = 0;

        if (hybrid && aiClient.AreEmbeddingsEnabled)
        {
            // Hybrid: combine semantic cosine similarity with keyword ts_rank.
            // final = α * semantic + (1-α) * min(1, 4 * ts_rank)
            var queryEmbedding = await aiClient.EmbedAsync(query, ct);
            var vectorLiteral = ToVectorLiteral(queryEmbedding);

            var hybridSql = $@"
                SELECT ""TranscriptionId"", ""RecordingId"", ""FullText"", ""Confidence"", ""Service"",
                       ""CallId"", ""TalkGroupId"", ""RecordingTime"",
                       ""TalkGroupNumber"", ""TalkGroupName"",
                       {HybridAlpha} * ""SemanticScore"" + {1 - HybridAlpha} * LEAST(1.0, 4.0 * ""KeywordScore"") AS ""RelevanceScore""
                FROM (
                    SELECT t.""Id"" AS ""TranscriptionId"", t.""RecordingId"", t.""FullText"", t.""Confidence"", t.""Service"",
                           r.""CallId"", c.""TalkGroupId"", c.""RecordingTime"",
                           tg.""Number"" AS ""TalkGroupNumber"", tg.""Name"" AS ""TalkGroupName"",
                           1.0 - (t.""Embedding"" <=> '{vectorLiteral}'::vector) AS ""SemanticScore"",
                           ts_rank(t.""SearchVector"", plainto_tsquery('english', @p0)) AS ""KeywordScore""
                    FROM ""Transcriptions"" t
                    JOIN ""Recordings"" r ON r.""Id"" = t.""RecordingId""
                    JOIN ""Calls"" c ON c.""Id"" = r.""CallId""
                    JOIN ""TalkGroups"" tg ON tg.""Id"" = c.""TalkGroupId""
                    WHERE t.""Embedding"" IS NOT NULL
                ) sub
                ORDER BY ""RelevanceScore"" DESC
                LIMIT {HybridCandidateLimit}";

            var candidates = await db.Database
                .SqlQueryRaw<TranscriptionSearchResultDto>(hybridSql, query)
                .ToListAsync(ct);

            totalCount = candidates.Count;
            var offset = (page - 1) * pageSize;
            transcriptionResults = candidates.Skip(offset).Take(pageSize).ToList();
        }
        else if (semantic && aiClient.AreEmbeddingsEnabled)
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
            // Keyword search via tsvector GIN index with ts_rank relevance
            var keywordSql = $@"
                SELECT t.""Id"" AS ""TranscriptionId"", t.""RecordingId"", t.""FullText"", t.""Confidence"", t.""Service"",
                       r.""CallId"", c.""TalkGroupId"", c.""RecordingTime"",
                       tg.""Number"" AS ""TalkGroupNumber"", tg.""Name"" AS ""TalkGroupName"",
                       ts_rank(t.""SearchVector"", plainto_tsquery('english', @p0)) AS ""RelevanceScore""
                FROM ""Transcriptions"" t
                JOIN ""Recordings"" r ON r.""Id"" = t.""RecordingId""
                JOIN ""Calls"" c ON c.""Id"" = r.""CallId""
                JOIN ""TalkGroups"" tg ON tg.""Id"" = c.""TalkGroupId""
                WHERE t.""SearchVector"" @@ plainto_tsquery('english', @p0)
                ORDER BY ts_rank(t.""SearchVector"", plainto_tsquery('english', @p0)) DESC, t.""CreatedAt"" DESC
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

        // Populate summaries and incidents (sequential — same DbContext instance).
        var pagedSummaries = await SearchSummariesPagedAsync(query, 5, null, null, ct);
        summaryResults = pagedSummaries.Summaries.ToList();
        incidentResults = (await SearchIncidentsAsync(query, 5, ct)).ToList();

        return new SearchResultsDto(
            transcriptionResults, summaryResults, incidentResults, totalCount, page, pageSize,
            pagedSummaries.Summaries.Count, pagedSummaries.NextCursorTime, pagedSummaries.NextCursorId, pagedSummaries.HasMore);
    }

    public async Task<IReadOnlyList<TranscriptSummaryDto>> SearchSummariesAsync(string query, int count, CancellationToken ct = default)
    {
        var sql = $@"
            SELECT s.""Id"", s.""TalkGroupId"", s.""StartTime"", s.""EndTime"", s.""TranscriptCount"",
                   s.""TotalDurationSeconds"", s.""Summary"", s.""Severity"", s.""GeneratedAt"", s.""CreatedAt"",
                   tg.""Number"", tg.""Name"", tg.""Description""
            FROM ""TranscriptSummaries"" s
            JOIN ""TalkGroups"" tg ON tg.""Id"" = s.""TalkGroupId""
            WHERE s.""SearchVector"" @@ plainto_tsquery('english', @p0)
            ORDER BY s.""StartTime"" DESC
            LIMIT {count}";

        var results = await db.Database
            .SqlQueryRaw<TranscriptSummarySearchRow>(sql, query)
            .ToListAsync(ct);

        return results.Select(r => new TranscriptSummaryDto(
            r.Id, r.TalkGroupId, r.Number, r.Name, r.Description,
            r.StartTime, r.EndTime, r.TranscriptCount, r.TotalDurationSeconds,
            r.Summary, r.Severity, r.GeneratedAt.UtcDateTime,
            [], [], [])).ToList();
    }

    public async Task<PagedSummarySearchResult> SearchSummariesPagedAsync(
        string query, int pageSize, DateTimeOffset? beforeTime, long? beforeId, CancellationToken ct = default)
    {
        // Cursor over (EndTime, Id) so equal EndTimes page deterministically.
        var cursorClause = beforeTime.HasValue && beforeId.HasValue
            ? "AND (s.\"EndTime\" < @p1 OR (s.\"EndTime\" = @p1 AND s.\"Id\" < @p2))"
            : "";

        var sql = $@"
            SELECT s.""Id"", s.""TalkGroupId"", s.""StartTime"", s.""EndTime"", s.""TranscriptCount"",
                   s.""TotalDurationSeconds"", s.""Summary"", s.""Severity"", s.""GeneratedAt"", s.""CreatedAt"",
                   tg.""Number"", tg.""Name"", tg.""Description""
            FROM ""TranscriptSummaries"" s
            JOIN ""TalkGroups"" tg ON tg.""Id"" = s.""TalkGroupId""
            WHERE s.""SearchVector"" @@ plainto_tsquery('english', @p0)
            {cursorClause}
            ORDER BY s.""EndTime"" DESC, s.""Id"" DESC
            LIMIT {pageSize + 1}";

        var rows = beforeTime.HasValue && beforeId.HasValue
            ? await db.Database.SqlQueryRaw<TranscriptSummarySearchRow>(sql, query, beforeTime.Value, beforeId.Value).ToListAsync(ct)
            : await db.Database.SqlQueryRaw<TranscriptSummarySearchRow>(sql, query).ToListAsync(ct);

        var hasMore = rows.Count > pageSize;
        if (hasMore) rows = rows.Take(pageSize).ToList();

        var last = rows.Count > 0 ? rows[^1] : null;
        return new PagedSummarySearchResult(
            rows.Select(r => new TranscriptSummaryDto(
                r.Id, r.TalkGroupId, r.Number, r.Name, r.Description,
                r.StartTime, r.EndTime, r.TranscriptCount, r.TotalDurationSeconds,
                r.Summary, r.Severity, r.GeneratedAt.UtcDateTime,
                [], [], [])).ToList(),
            last?.EndTime,
            last?.Id,
            hasMore);
    }

    public async Task<IReadOnlyList<NotableIncidentDto>> SearchIncidentsAsync(string query, int count, CancellationToken ct = default)
    {
        var sql = $@"
            SELECT n.""Id"", n.""Description"", n.""ImportanceScore"", n.""CreatedAt""
            FROM ""NotableIncidents"" n
            WHERE n.""SearchVector"" @@ plainto_tsquery('english', @p0)
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
    long Id, int TalkGroupId, string Number, string? Name, string? Description,
    DateTimeOffset StartTime, DateTimeOffset EndTime, int TranscriptCount,
    double TotalDurationSeconds, string Summary, int? Severity, DateTimeOffset GeneratedAt, DateTimeOffset CreatedAt);
