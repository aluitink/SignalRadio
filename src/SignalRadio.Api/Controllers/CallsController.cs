using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Services;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CallsController : ControllerBase
{
    private readonly ILogger<CallsController> _logger;
    private readonly ICallService _callService;

    public CallsController(ILogger<CallsController> logger, ICallService callService)
    {
        _logger = logger;
        _callService = callService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentCalls([FromQuery] int limit = 50)
    {
        try
        {
            var calls = await _callService.GetRecentCallsAsync(limit);
            
            return Ok(new
            {
                Calls = calls.Select(c => new
                {
                    c.Id,
                    c.TalkgroupId,
                    c.SystemName,
                    c.RecordingTime,
                    c.Frequency,
                    c.Duration,
                    c.CreatedAt,
                    c.UpdatedAt,
                    RecordingCount = c.Recordings.Count,
                    Recordings = c.Recordings.Select(r => new
                    {
                        r.Id,
                        r.FileName,
                        r.Format,
                        r.FileSize,
                        r.IsUploaded,
                        r.BlobName,
                        r.UploadedAt
                    })
                }),
                Count = calls.Count(),
                Limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent calls");
            return StatusCode(500, "Failed to retrieve recent calls");
        }
    }

    [HttpGet("activity-statistics")]
    public async Task<IActionResult> GetActivityStatistics()
    {
        try
        {
            var recentCalls = await _callService.GetRecentCallsAsync(1000);
            
            // Return activity count per talkgroup as a simple dictionary
            var activityStats = recentCalls
                .GroupBy(c => c.TalkgroupId)
                .ToDictionary(g => g.Key, g => g.Count());

            return Ok(activityStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve activity statistics");
            return StatusCode(500, "Failed to retrieve activity statistics");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCall(int id)
    {
        try
        {
            var call = await _callService.GetCallByIdAsync(id);
            if (call == null)
            {
                return NotFound($"Call with ID {id} not found");
            }

            return Ok(new
            {
                call.Id,
                call.TalkgroupId,
                call.SystemName,
                call.RecordingTime,
                call.Frequency,
                call.Duration,
                call.CreatedAt,
                call.UpdatedAt,
                RecordingCount = call.Recordings.Count,
                Recordings = call.Recordings.Select(r => new
                {
                    r.Id,
                    r.FileName,
                    r.Format,
                    r.ContentType,
                    r.FileSize,
                    r.IsUploaded,
                    r.BlobName,
                    r.BlobUri,
                    r.UploadedAt,
                    r.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve call {CallId}", id);
            return StatusCode(500, "Failed to retrieve call");
        }
    }

    [HttpGet("talkgroup/{talkgroupId}")]
    public async Task<IActionResult> GetCallsByTalkgroup(string talkgroupId, [FromQuery] int? limit = null)
    {
        try
        {
            var calls = await _callService.GetCallsByTalkgroupAsync(talkgroupId, limit);
            
            return Ok(new
            {
                TalkgroupId = talkgroupId,
                Calls = calls.Select(c => new
                {
                    c.Id,
                    c.TalkgroupId,
                    c.SystemName,
                    c.RecordingTime,
                    c.Frequency,
                    c.Duration,
                    c.CreatedAt,
                    RecordingCount = c.Recordings.Count,
                    Formats = c.Recordings.Select(r => r.Format).Distinct()
                }),
                Count = calls.Count(),
                Limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve calls for talkgroup {TalkgroupId}", talkgroupId);
            return StatusCode(500, "Failed to retrieve calls for talkgroup");
        }
    }

    [HttpGet("system/{systemName}")]
    public async Task<IActionResult> GetCallsBySystem(string systemName, [FromQuery] int? limit = null)
    {
        try
        {
            var calls = await _callService.GetCallsBySystemAsync(systemName, limit);
            
            return Ok(new
            {
                SystemName = systemName,
                Calls = calls.Select(c => new
                {
                    c.Id,
                    c.TalkgroupId,
                    c.SystemName,
                    c.RecordingTime,
                    c.Frequency,
                    c.Duration,
                    c.CreatedAt,
                    RecordingCount = c.Recordings.Count,
                    Formats = c.Recordings.Select(r => r.Format).Distinct()
                }),
                Count = calls.Count(),
                Limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve calls for system {SystemName}", systemName);
            return StatusCode(500, "Failed to retrieve calls for system");
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetCallStats()
    {
        try
        {
            var recentCalls = await _callService.GetRecentCallsAsync(1000); // Get more for stats
            
            var stats = new
            {
                TotalCalls = recentCalls.Count(),
                TotalRecordings = recentCalls.Sum(c => c.Recordings.Count),
                SystemBreakdown = recentCalls
                    .GroupBy(c => c.SystemName)
                    .Select(g => new
                    {
                        System = g.Key,
                        CallCount = g.Count(),
                        RecordingCount = g.Sum(c => c.Recordings.Count)
                    }),
                TalkgroupBreakdown = recentCalls
                    .GroupBy(c => c.TalkgroupId)
                    .Select(g => new
                    {
                        TalkgroupId = g.Key,
                        CallCount = g.Count(),
                        RecordingCount = g.Sum(c => c.Recordings.Count)
                    })
                    .OrderByDescending(x => x.CallCount)
                    .Take(10), // Top 10 most active talkgroups
                FormatBreakdown = recentCalls
                    .SelectMany(c => c.Recordings)
                    .GroupBy(r => r.Format)
                    .Select(g => new
                    {
                        Format = g.Key,
                        Count = g.Count(),
                        TotalSize = g.Sum(r => r.FileSize)
                    })
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve call statistics");
            return StatusCode(500, "Failed to retrieve call statistics");
        }
    }
}
