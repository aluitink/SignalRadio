using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Services;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Search recordings by transcription text using Full Text Search
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="talkGroupId">Optional filter by talk group ID</param>
    /// <param name="startDate">Optional start date filter (ISO 8601 format)</param>
    /// <param name="endDate">Optional end date filter (ISO 8601 format)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Search results with pagination metadata</returns>
    [HttpGet("transcriptions")]
    public async Task<IActionResult> SearchTranscriptions(
        [FromQuery] string q,
        [FromQuery] string? talkGroupId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Search query 'q' is required" });
        }

        if (q.Length < 3)
        {
            return BadRequest(new { error = "Search query must be at least 3 characters long" });
        }

        try
        {
            var result = await _searchService.SearchTranscriptionsAsync(
                q, talkGroupId, startDate, endDate, page, pageSize);

            var response = new
            {
                query = q,
                filters = new
                {
                    talkGroupId,
                    startDate,
                    endDate
                },
                pagination = new
                {
                    page = result.PageNumber,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages,
                    totalCount = result.TotalCount,
                    hasPreviousPage = result.HasPreviousPage,
                    hasNextPage = result.HasNextPage
                },
                results = result.Items.Select(r => new
                {
                    id = r.Id,
                    callId = r.CallId,
                    fileName = r.FileName,
                    format = r.Format,
                    duration = r.FormattedDuration,
                    transcriptionText = r.TranscriptionText,
                    transcriptionConfidence = r.TranscriptionConfidence,
                    transcriptionLanguage = r.TranscriptionLanguage,
                    call = new
                    {
                        id = r.Call.Id,
                        talkgroupId = r.Call.TalkgroupId,
                        systemName = r.Call.SystemName,
                        recordingTime = r.Call.RecordingTime,
                        frequency = r.Call.Frequency,
                        duration = r.Call.Duration?.ToString(@"mm\:ss")
                    },
                    createdAt = r.CreatedAt,
                    blobUri = r.BlobUri
                })
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid search request: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during transcription search for query: {Query}", q);
            return StatusCode(500, new { error = "An error occurred while processing your search request" });
        }
    }

    /// <summary>
    /// Get search suggestions/autocomplete for transcription search
    /// </summary>
    /// <param name="q">Partial search term</param>
    /// <param name="limit">Maximum number of suggestions (default: 10, max: 20)</param>
    /// <returns>List of suggested search terms</returns>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSearchSuggestions(
        [FromQuery] string q,
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Query 'q' is required" });
        }

        if (q.Length < 2)
        {
            return Ok(new { suggestions = new string[0] });
        }

        if (limit < 1 || limit > 20)
        {
            limit = 10;
        }

        try
        {
            var suggestions = await _searchService.GetSearchSuggestionsAsync(q, limit);
            
            return Ok(new
            {
                query = q,
                suggestions = suggestions.ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting search suggestions for query: {Query}", q);
            return StatusCode(500, new { error = "An error occurred while getting search suggestions" });
        }
    }

    /// <summary>
    /// Get search statistics and available filters
    /// </summary>
    /// <returns>Search metadata including available talk groups and date ranges</returns>
    [HttpGet("metadata")]
    public async Task<IActionResult> GetSearchMetadata()
    {
        try
        {
            // This could be expanded to include more metadata like:
            // - Available talk groups
            // - Date range of available transcriptions
            // - Total number of transcribed recordings
            // For now, return basic info
            
            return Ok(new
            {
                searchCapabilities = new
                {
                    fullTextSearch = true,
                    phraseSearch = true,
                    wildcardSearch = true,
                    filters = new[]
                    {
                        "talkGroupId",
                        "dateRange"
                    }
                },
                limits = new
                {
                    minQueryLength = 3,
                    maxPageSize = 100,
                    maxSuggestions = 20
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting search metadata");
            return StatusCode(500, new { error = "An error occurred while getting search metadata" });
        }
    }
}
