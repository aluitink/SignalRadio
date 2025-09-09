using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Models;
using SignalRadio.DataAccess.Services;
using SignalRadio.DataAccess.Extensions;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranscriptSummariesController : ControllerBase
{
    private readonly ITranscriptSummariesService _summariesService;

    public TranscriptSummariesController(ITranscriptSummariesService summariesService)
    {
        _summariesService = summariesService;
    }

    /// <summary>
    /// Get transcript summaries for a talkgroup within a time range
    /// </summary>
    [HttpGet("talkgroup/{talkGroupId}")]
    public async Task<ActionResult<IEnumerable<TranscriptSummaryResponse>>> GetByTalkGroupAndTimeRange(
        int talkGroupId,
        [FromQuery] DateTimeOffset startTime,
        [FromQuery] DateTimeOffset endTime)
    {
        var summaries = await _summariesService.GetByTalkGroupAndTimeRangeAsync(talkGroupId, startTime, endTime);
        var responses = summaries.Select(s => s.ToResponse()).ToList();
        
        return Ok(responses);
    }

    /// <summary>
    /// Get a specific transcript summary by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TranscriptSummaryResponse>> GetById(int id)
    {
        var summary = await _summariesService.GetByIdAsync(id);
        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary.ToResponse());
    }

    /// <summary>
    /// Get all transcript summaries with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TranscriptSummaryResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var pagedResult = await _summariesService.GetAllAsync(page, pageSize);
        
        var responseResult = new PagedResult<TranscriptSummaryResponse>
        {
            Items = pagedResult.Items.Select(s => s.ToResponse()).ToList(),
            TotalCount = pagedResult.TotalCount,
            Page = pagedResult.Page,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };

        return Ok(responseResult);
    }

    /// <summary>
    /// Get recent transcript summaries for ticker/dashboard display
    /// Returns summaries generated within the last specified minutes (defaults to fresh summaries)
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<TranscriptSummaryResponse>>> GetRecent(
        [FromQuery] int maxAgeMinutes = 15,
        [FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 50);
        maxAgeMinutes = Math.Clamp(maxAgeMinutes, 1, 1440); // 1 minute to 24 hours

        var cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-maxAgeMinutes);
        
        // Get all recent summaries and filter by age
        var allSummaries = await _summariesService.GetAllAsync(1, 1000);
        var recentSummaries = allSummaries.Items
            .Where(s => s.GeneratedAt >= cutoffTime)
            .OrderByDescending(s => s.GeneratedAt)
            .Take(limit)
            .Select(s => s.ToResponse())
            .ToList();

        return Ok(recentSummaries);
    }

    /// <summary>
    /// Get summaries for a specific talkgroup with pagination
    /// </summary>
    [HttpGet("talkgroup/{talkGroupId}/paged")]
    public async Task<ActionResult<PagedResult<TranscriptSummaryResponse>>> GetByTalkGroup(
        int talkGroupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var pagedResult = await _summariesService.GetByTalkGroupAsync(talkGroupId, page, pageSize);
        
        var responseResult = new PagedResult<TranscriptSummaryResponse>
        {
            Items = pagedResult.Items.Select(s => s.ToResponse()).ToList(),
            TotalCount = pagedResult.TotalCount,
            Page = pagedResult.Page,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };

        return Ok(responseResult);
    }

    /// <summary>
    /// Search for topics across all summaries
    /// </summary>
    [HttpGet("topics/search")]
    public async Task<ActionResult<IEnumerable<object>>> SearchTopics([FromQuery] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest("Search term is required");
        }

        var topics = await _summariesService.SearchTopicsAsync(searchTerm);
        var results = topics.Select(t => new 
        {
            Topic = t.Topic,
            Relevance = t.Relevance,
            TalkGroupId = t.TranscriptSummary?.TalkGroupId,
            TalkGroupName = t.TranscriptSummary?.TalkGroup?.Name ?? t.TranscriptSummary?.TalkGroup?.AlphaTag,
            GeneratedAt = t.TranscriptSummary?.GeneratedAt
        });

        return Ok(results);
    }

    /// <summary>
    /// Get notable calls for a specific call ID
    /// </summary>
    [HttpGet("notable-calls/{callId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetNotableCallsByCallId(int callId)
    {
        var notableCalls = await _summariesService.GetNotableCallsByCallIdAsync(callId);
        var results = notableCalls.Select(nic => new 
        {
            Description = nic.NotableIncident?.Description ?? "",
            ImportanceScore = nic.NotableIncident?.ImportanceScore,
            CallNote = nic.CallNote,
            CallId = nic.CallId,
            // Get summary information via the incident's relationships
            Summaries = nic.NotableIncident?.TranscriptSummaryNotableIncidents?.Select(tsni => new
            {
                TalkGroupId = tsni.TranscriptSummary?.TalkGroupId,
                TalkGroupName = tsni.TranscriptSummary?.TalkGroup?.Name ?? tsni.TranscriptSummary?.TalkGroup?.AlphaTag,
                StartTime = tsni.TranscriptSummary?.StartTime,
                EndTime = tsni.TranscriptSummary?.EndTime
            }) ?? Enumerable.Empty<object>()
        });

        return Ok(results);
    }

    /// <summary>
    /// Delete a transcript summary
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var summary = await _summariesService.GetByIdAsync(id);
        if (summary == null)
        {
            return NotFound();
        }

        await _summariesService.DeleteAsync(id);
        return NoContent();
    }
}
