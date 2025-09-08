using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Interfaces;
using SignalRadio.Core.Models;
using SignalRadio.DataAccess.Services;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranscriptSummaryController : ControllerBase
{
    private readonly ITranscriptSummaryService _summaryService;
    private readonly ITalkGroupsService _talkGroupsService;
    private readonly ILogger<TranscriptSummaryController> _logger;

    public TranscriptSummaryController(
        ITranscriptSummaryService summaryService,
        ITalkGroupsService talkGroupsService,
        ILogger<TranscriptSummaryController> logger)
    {
        _summaryService = summaryService;
        _talkGroupsService = talkGroupsService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a summary of transcripts for a specific talkgroup within a time window
    /// </summary>
    /// <param name="talkGroupId">TalkGroup ID to summarize</param>
    /// <param name="windowMinutes">Time window in minutes (defaults to 60)</param>
    /// <param name="forceRefresh">Force refresh of cached summary</param>
    /// <returns>Transcript summary with AI analysis</returns>
    [HttpGet("talkgroup/{talkGroupId:int}")]
    public async Task<IActionResult> GetTalkGroupSummary(
        int talkGroupId,
        [FromQuery] int windowMinutes = 60,
        [FromQuery] bool forceRefresh = false)
    {
        try
        {
            // Validate talkgroup exists
            var talkGroup = await _talkGroupsService.GetByIdAsync(talkGroupId);
            if (talkGroup == null)
            {
                return NotFound($"TalkGroup with ID {talkGroupId} not found");
            }

            // Check if service is available
            if (!await _summaryService.IsAvailableAsync())
            {
                return BadRequest("Transcript summary service is not available or not configured");
            }

            // Validate window
            windowMinutes = Math.Clamp(windowMinutes, 5, 1440); // 5 minutes to 24 hours

            var endTime = DateTimeOffset.UtcNow;
            var startTime = endTime.AddMinutes(-windowMinutes);

            var request = new TranscriptSummaryRequest
            {
                TalkGroupId = talkGroupId,
                StartTime = startTime,
                EndTime = endTime,
                ForceRefresh = forceRefresh
            };

            var summary = await _summaryService.GenerateSummaryAsync(request);
            
            if (summary == null)
            {
                return StatusCode(500, "Failed to generate summary");
            }

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for TalkGroup {TalkGroupId}", talkGroupId);
            return StatusCode(500, "An error occurred while generating the summary");
        }
    }

    /// <summary>
    /// Generate a summary for a custom time range
    /// </summary>
    /// <param name="request">Summary request with custom time range</param>
    /// <returns>Transcript summary with AI analysis</returns>
    [HttpPost("custom")]
    public async Task<IActionResult> GetCustomSummary([FromBody] TranscriptSummaryRequest request)
    {
        try
        {
            // Validate talkgroup exists
            var talkGroup = await _talkGroupsService.GetByIdAsync(request.TalkGroupId);
            if (talkGroup == null)
            {
                return NotFound($"TalkGroup with ID {request.TalkGroupId} not found");
            }

            // Check if service is available
            if (!await _summaryService.IsAvailableAsync())
            {
                return BadRequest("Transcript summary service is not available or not configured");
            }

            // Validate time range
            if (request.StartTime >= request.EndTime)
            {
                return BadRequest("Start time must be before end time");
            }

            var timeSpan = request.EndTime - request.StartTime;
            if (timeSpan > TimeSpan.FromHours(24))
            {
                return BadRequest("Time window cannot exceed 24 hours");
            }

            var summary = await _summaryService.GenerateSummaryAsync(request);
            
            if (summary == null)
            {
                return StatusCode(500, "Failed to generate summary");
            }

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating custom summary for TalkGroup {TalkGroupId}", request.TalkGroupId);
            return StatusCode(500, "An error occurred while generating the summary");
        }
    }

    /// <summary>
    /// Check if the transcript summary service is available
    /// </summary>
    /// <returns>Service availability status</returns>
    [HttpGet("status")]
    public async Task<IActionResult> GetServiceStatus()
    {
        try
        {
            var isAvailable = await _summaryService.IsAvailableAsync();
            return Ok(new { available = isAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking transcript summary service status");
            return StatusCode(500, "An error occurred while checking service status");
        }
    }

    /// <summary>
    /// Clear cached summaries for a specific talkgroup or all cached summaries
    /// </summary>
    /// <param name="talkGroupId">Optional talkgroup ID to clear cache for</param>
    /// <returns>Success result</returns>
    [HttpDelete("cache")]
    public async Task<IActionResult> ClearCache([FromQuery] int? talkGroupId = null)
    {
        try
        {
            await _summaryService.ClearCacheAsync(talkGroupId);
            
            var message = talkGroupId.HasValue 
                ? $"Cache cleared for TalkGroup {talkGroupId}"
                : "All cached summaries cleared";
                
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, "An error occurred while clearing the cache");
        }
    }
}
