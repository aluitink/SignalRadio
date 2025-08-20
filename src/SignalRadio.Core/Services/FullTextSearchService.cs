using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SignalRadio.Core.Data;
using SignalRadio.Core.Models;

namespace SignalRadio.Core.Services;

public class FullTextSearchService : ISearchService
{
    private readonly SignalRadioDbContext _context;
    private readonly ILogger<FullTextSearchService> _logger;

    public FullTextSearchService(SignalRadioDbContext context, ILogger<FullTextSearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SearchResult<Recording>> SearchTranscriptionsAsync(
        string searchTerm,
        string? talkGroupId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        _logger.LogInformation("Searching transcriptions for term: {SearchTerm}, TalkGroup: {TalkGroupId}, DateRange: {StartDate} - {EndDate}",
            searchTerm, talkGroupId, startDate, endDate);

        try
        {
            // Escape special characters for Full Text Search
            var escapedSearchTerm = EscapeFullTextSearchTerm(searchTerm);

            // Build the base query
            var query = _context.Recordings
                .Include(r => r.Call)
                .Where(r => r.HasTranscription && !string.IsNullOrEmpty(r.TranscriptionText));

            // Apply filters
            if (!string.IsNullOrEmpty(talkGroupId))
            {
                query = query.Where(r => r.Call.TalkgroupId == talkGroupId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.Call.RecordingTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.Call.RecordingTime <= endDate.Value);
            }

            // Use raw SQL for Full Text Search with proper parameterization
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            var sqlQuery = $@"
                SELECT r.Id
                FROM Recordings r
                INNER JOIN Calls c ON r.CallId = c.Id
                WHERE r.HasTranscription = 1 
                AND r.TranscriptionText IS NOT NULL
                AND CONTAINS(r.TranscriptionText, @searchTerm)
                {(string.IsNullOrEmpty(talkGroupId) ? "" : "AND c.TalkgroupId = @talkGroupId")}
                {(startDate.HasValue ? "AND c.RecordingTime >= @startDate" : "")}
                {(endDate.HasValue ? "AND c.RecordingTime <= @endDate" : "")}
                ORDER BY c.RecordingTime DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            using var command = connection.CreateCommand();
            command.CommandText = sqlQuery;

            // Add parameters
            var searchParam = command.CreateParameter();
            searchParam.ParameterName = "@searchTerm";
            searchParam.Value = escapedSearchTerm;
            command.Parameters.Add(searchParam);

            if (!string.IsNullOrEmpty(talkGroupId))
            {
                var talkGroupParam = command.CreateParameter();
                talkGroupParam.ParameterName = "@talkGroupId";
                talkGroupParam.Value = talkGroupId;
                command.Parameters.Add(talkGroupParam);
            }

            if (startDate.HasValue)
            {
                var startDateParam = command.CreateParameter();
                startDateParam.ParameterName = "@startDate";
                startDateParam.Value = startDate.Value;
                command.Parameters.Add(startDateParam);
            }

            if (endDate.HasValue)
            {
                var endDateParam = command.CreateParameter();
                endDateParam.ParameterName = "@endDate";
                endDateParam.Value = endDate.Value;
                command.Parameters.Add(endDateParam);
            }

            var offsetParam = command.CreateParameter();
            offsetParam.ParameterName = "@offset";
            offsetParam.Value = (pageNumber - 1) * pageSize;
            command.Parameters.Add(offsetParam);

            var pageSizeParam = command.CreateParameter();
            pageSizeParam.ParameterName = "@pageSize";
            pageSizeParam.Value = pageSize;
            command.Parameters.Add(pageSizeParam);

            // Execute and get recording IDs
            var recordingIds = new List<int>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                recordingIds.Add(reader.GetInt32(0));
            }

            await connection.CloseAsync();

            // Now get the full recordings with their related data
            var recordings = await _context.Recordings
                .Include(r => r.Call)
                .Where(r => recordingIds.Contains(r.Id))
                .OrderByDescending(r => r.Call.RecordingTime)
                .ToListAsync();

            // Get total count for pagination
            var totalCount = await GetSearchResultCountAsync(escapedSearchTerm, talkGroupId, startDate, endDate);

            var result = new SearchResult<Recording>
            {
                Items = recordings,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _logger.LogInformation("Search completed. Found {TotalCount} results, returning page {PageNumber} with {ItemCount} items",
                totalCount, pageNumber, recordings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during transcription search for term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string partialTerm, int maxSuggestions = 10)
    {
        if (string.IsNullOrWhiteSpace(partialTerm) || partialTerm.Length < 3)
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            // This is a simplified implementation - in production you might want to 
            // use a more sophisticated approach like maintaining a separate search terms table
            var suggestions = await _context.Recordings
                .Where(r => r.HasTranscription && !string.IsNullOrEmpty(r.TranscriptionText))
                .Select(r => r.TranscriptionText!)
                .Take(100) // Limit initial results for performance
                .ToListAsync();

            // Extract words that start with the partial term
            var words = suggestions
                .SelectMany(text => text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Where(word => word.StartsWith(partialTerm, StringComparison.OrdinalIgnoreCase))
                .Select(word => word.ToLowerInvariant())
                .Distinct()
                .OrderBy(word => word)
                .Take(maxSuggestions);

            return words;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting search suggestions for term: {PartialTerm}", partialTerm);
            return Enumerable.Empty<string>();
        }
    }

    private async Task<int> GetSearchResultCountAsync(
        string escapedSearchTerm,
        string? talkGroupId,
        DateTime? startDate,
        DateTime? endDate)
    {
        try
        {
            var countSql = $@"
                SELECT COUNT(*)
                FROM Recordings r
                INNER JOIN Calls c ON r.CallId = c.Id
                WHERE r.HasTranscription = 1 
                AND r.TranscriptionText IS NOT NULL
                AND CONTAINS(r.TranscriptionText, @searchTerm)
                {(string.IsNullOrEmpty(talkGroupId) ? "" : "AND c.TalkgroupId = @talkGroupId")}
                {(startDate.HasValue ? "AND c.RecordingTime >= @startDate" : "")}
                {(endDate.HasValue ? "AND c.RecordingTime <= @endDate" : "")}";

            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = countSql;
            
            var searchParam = command.CreateParameter();
            searchParam.ParameterName = "@searchTerm";
            searchParam.Value = escapedSearchTerm;
            command.Parameters.Add(searchParam);

            if (!string.IsNullOrEmpty(talkGroupId))
            {
                var talkGroupParam = command.CreateParameter();
                talkGroupParam.ParameterName = "@talkGroupId";
                talkGroupParam.Value = talkGroupId;
                command.Parameters.Add(talkGroupParam);
            }

            if (startDate.HasValue)
            {
                var startDateParam = command.CreateParameter();
                startDateParam.ParameterName = "@startDate";
                startDateParam.Value = startDate.Value;
                command.Parameters.Add(startDateParam);
            }

            if (endDate.HasValue)
            {
                var endDateParam = command.CreateParameter();
                endDateParam.ParameterName = "@endDate";
                endDateParam.Value = endDate.Value;
                command.Parameters.Add(endDateParam);
            }

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search result count");
            return 0;
        }
    }

    private static string EscapeFullTextSearchTerm(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return string.Empty;

        // Escape special Full Text Search characters
        term = term.Replace("\"", "\"\"");
        
        // If the term contains spaces, wrap in quotes for phrase search
        if (term.Contains(' '))
        {
            return $"\"{term}\"";
        }

        // For single words, use wildcard search to match partial words
        return $"\"{term}*\"";
    }
}
