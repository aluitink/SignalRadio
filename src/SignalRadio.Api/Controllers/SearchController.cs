using Microsoft.AspNetCore.Mvc;
using SignalRadio.DataAccess.Services;
using SignalRadio.Core.Models;
using SignalRadio.DataAccess;

namespace SignalRadio.Api.Controllers;

/// <summary>
/// Controller for full-text search functionality across summaries, incidents, and topics
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ITranscriptSummariesService _summariesService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ITranscriptSummariesService summariesService,
        ILogger<SearchController> logger)
    {
        _summariesService = summariesService;
        _logger = logger;
    }

    /// <summary>
    /// Search across all content types (summaries, incidents, topics)
    /// </summary>
    /// <param name="q">Search term</param>
    /// <param name="types">Comma-separated list of content types to search (Summary,Incident,Topic)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Results per page (default: 20, max: 100)</param>
    /// <returns>Paged search results</returns>
    [HttpGet]
    public async Task<ActionResult<SearchResultPage>> Search(
        [FromQuery] string q,
        [FromQuery] string? types = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search term is required");
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        if (page < 1)
        {
            page = 1;
        }

        var contentTypes = !string.IsNullOrWhiteSpace(types) 
            ? types.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim())
            : null;

        _logger.LogInformation("Searching for term: {SearchTerm}, types: {ContentTypes}, page: {Page}", 
            q, string.Join(",", contentTypes ?? new[] { "All" }), page);

        try
        {
            var results = await _summariesService.SearchAsync(q, contentTypes, page, pageSize);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search for term: {SearchTerm}", q);
            return StatusCode(500, "An error occurred while searching");
        }
    }

    /// <summary>
    /// Search specifically within transcript summaries
    /// </summary>
    /// <param name="q">The search term</param>
    /// <param name="talkGroupId">Optional talk group ID filter</param>
    /// <param name="startDate">Optional start date filter (ISO 8601 format)</param>
    /// <param name="endDate">Optional end date filter (ISO 8601 format)</param>
    /// <param name="maxResults">Maximum number of results to return (default: 50, max: 200)</param>
    /// <returns>Matching transcript summaries</returns>
    [HttpGet("summaries")]
    public async Task<ActionResult<IEnumerable<TranscriptSummary>>> SearchSummaries(
        [FromQuery] string q,
        [FromQuery] int? talkGroupId = null,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        [FromQuery] int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search term is required");
        }

        if (maxResults > 200)
        {
            maxResults = 200;
        }

        _logger.LogInformation("Searching summaries for term: {SearchTerm}, talkGroup: {TalkGroupId}", q, talkGroupId);

        try
        {
            var results = await _summariesService.SearchSummariesAsync(q, talkGroupId, startDate, endDate, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching summaries for term: {SearchTerm}", q);
            return StatusCode(500, "An error occurred while searching summaries");
        }
    }

    /// <summary>
    /// Search specifically within notable incidents
    /// </summary>
    /// <param name="q">The search term</param>
    /// <param name="minImportanceScore">Optional minimum importance score filter</param>
    /// <param name="maxResults">Maximum number of results to return (default: 50, max: 200)</param>
    /// <returns>Matching notable incidents</returns>
    [HttpGet("incidents")]
    public async Task<ActionResult<IEnumerable<DataAccess.NotableIncident>>> SearchIncidents(
        [FromQuery] string q,
        [FromQuery] double? minImportanceScore = null,
        [FromQuery] int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search term is required");
        }

        if (maxResults > 200)
        {
            maxResults = 200;
        }

        _logger.LogInformation("Searching incidents for term: {SearchTerm}, minScore: {MinScore}", q, minImportanceScore);

        try
        {
            var results = await _summariesService.SearchIncidentsAsync(q, minImportanceScore, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching incidents for term: {SearchTerm}", q);
            return StatusCode(500, "An error occurred while searching incidents");
        }
    }

    /// <summary>
    /// Search specifically within topics
    /// </summary>
    /// <param name="q">The search term</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="maxResults">Maximum number of results to return (default: 50, max: 200)</param>
    /// <returns>Matching topics</returns>
    [HttpGet("topics")]
    public async Task<ActionResult<IEnumerable<Topic>>> SearchTopics(
        [FromQuery] string q,
        [FromQuery] string? category = null,
        [FromQuery] int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search term is required");
        }

        if (maxResults > 200)
        {
            maxResults = 200;
        }

        _logger.LogInformation("Searching topics for term: {SearchTerm}, category: {Category}", q, category);

        try
        {
            var results = await _summariesService.SearchTopicsAsync(q, category, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching topics for term: {SearchTerm}", q);
            return StatusCode(500, "An error occurred while searching topics");
        }
    }
}
