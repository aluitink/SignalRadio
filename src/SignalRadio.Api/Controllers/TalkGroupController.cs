using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Services;
using SignalRadio.Core.Models;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TalkGroupController : ControllerBase
{
    private readonly ITalkGroupService _talkGroupService;
    private readonly ICallService _callService;
    private readonly ILogger<TalkGroupController> _logger;

    public TalkGroupController(ITalkGroupService talkGroupService, ICallService callService, ILogger<TalkGroupController> logger)
    {
        _talkGroupService = talkGroupService;
        _callService = callService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTalkGroups([FromQuery] string? category = null, [FromQuery] string? search = null)
    {
        try
        {
            IEnumerable<TalkGroup> talkGroups;

            if (!string.IsNullOrEmpty(search))
            {
                talkGroups = await _talkGroupService.SearchTalkGroupsAsync(search);
            }
            else if (!string.IsNullOrEmpty(category))
            {
                talkGroups = await _talkGroupService.GetTalkGroupsByCategoryAsync(category);
            }
            else
            {
                talkGroups = await _talkGroupService.GetAllTalkGroupsAsync();
            }

            return Ok(talkGroups.Select(tg => new
            {
                tg.Id,
                tg.Decimal,
                tg.Hex,
                tg.Mode,
                tg.AlphaTag,
                tg.Description,
                tg.Tag,
                tg.Category,
                tg.Priority
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve talk groups");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{decimalId}")]
    public async Task<IActionResult> GetTalkGroup(string decimalId, [FromQuery] int? callLimit = 10)
    {
        try
        {
            var talkGroup = await _talkGroupService.GetTalkGroupByIdAsync(decimalId);
            if (talkGroup == null)
            {
                return NotFound(new { error = $"Talk group {decimalId} not found" });
            }

            // Get recent calls for this talk group
            var recentCalls = await _callService.GetCallsByTalkgroupAsync(decimalId, callLimit);

            return Ok(new
            {
                TalkGroup = new
                {
                    talkGroup.Id,
                    talkGroup.Decimal,
                    talkGroup.Hex,
                    talkGroup.Mode,
                    talkGroup.AlphaTag,
                    talkGroup.Description,
                    talkGroup.Tag,
                    talkGroup.Category,
                    talkGroup.Priority
                },
                RecentCalls = recentCalls.Select(c => new
                {
                    c.Id,
                    c.TalkgroupId,
                    c.SystemName,
                    c.RecordingTime,
                    c.Frequency,
                    c.Duration,
                    c.CreatedAt,
                    RecordingCount = c.Recordings.Count,
                    Recordings = c.Recordings.Select(r => new
                    {
                        r.Id,
                        r.FileName,
                        r.Format,
                        r.FileSize,
                        r.IsUploaded,
                        r.BlobName,
                        r.BlobUri,
                        r.UploadedAt
                    })
                }),
                CallStats = new
                {
                    TotalCalls = recentCalls.Count(),
                    TotalRecordings = recentCalls.Sum(c => c.Recordings.Count),
                    CallLimit = callLimit
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve talk group {DecimalId}", decimalId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await _talkGroupService.GetCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve categories");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportCsv(IFormFile csvFile)
    {
        try
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                return BadRequest(new { error = "No CSV file provided" });
            }

            if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "File must be a CSV file" });
            }

            _logger.LogInformation("Starting CSV import of file: {FileName} ({Size} bytes)", 
                csvFile.FileName, csvFile.Length);

            using var stream = csvFile.OpenReadStream();
            var importedCount = await _talkGroupService.ImportFromCsvAsync(stream);

            _logger.LogInformation("Successfully imported {Count} talk groups from {FileName}", 
                importedCount, csvFile.FileName);

            return Ok(new 
            { 
                message = $"Successfully imported {importedCount} talk groups",
                count = importedCount,
                fileName = csvFile.FileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import CSV file: {FileName}", csvFile?.FileName);
            return StatusCode(500, new { error = "Failed to import CSV file", details = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> ClearTalkGroups()
    {
        try
        {
            var success = await _talkGroupService.ClearAllTalkGroupsAsync();
            if (success)
            {
                return Ok(new { message = "All talk groups have been cleared" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to clear talk groups" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear talk groups");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
