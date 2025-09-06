using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SignalRadio.DataAccess;
using SignalRadio.DataAccess.Services;
using SignalRadio.Api.Extensions;
using SignalRadio.Api.Dtos;
using SignalRadio.Core.AI.Interfaces;

namespace SignalRadio.Api.Controllers2;

[ApiController]
[Route("api/[controller]")]
public class TranscriptionsController : ControllerBase
{
    private readonly ITranscriptionsService _svc;
    private readonly IAiSummaryService _aiSummaryService;
    private readonly ILogger<TranscriptionsController> _logger;

    public TranscriptionsController(
        ITranscriptionsService svc, 
        IAiSummaryService aiSummaryService,
        ILogger<TranscriptionsController> logger)
    {
        _svc = svc;
        _aiSummaryService = aiSummaryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

    var result = await _svc.GetAllAsync(page, pageSize);
    return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
    var item = await _svc.GetByIdAsync(id);
    return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Transcription model)
    {
    var created = await _svc.CreateAsync(model);
    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Transcription model)
    {
    if (id != model.Id) return BadRequest("Id mismatch");
    var ok = await _svc.UpdateAsync(id, model);
    return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
    var ok = await _svc.DeleteAsync(id);
    return ok ? NoContent() : NotFound();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
    if (string.IsNullOrWhiteSpace(q)) return BadRequest("q is required");

    var callResult = await _svc.SearchCallsAsync(q, page, pageSize);
    
    // Convert to DTOs
    var callDtos = callResult.Items.Select(c => c.ToDto()).ToList();

    var result = new PagedResult<CallDto>
    {
        Items = callDtos,
        TotalCount = callResult.TotalCount,
        Page = callResult.Page,
        PageSize = callResult.PageSize,
        TotalPages = callResult.TotalPages
    };

    return Ok(result);
    }

    // AI Summary endpoints
    
    [HttpGet("summaries")]
    public async Task<IActionResult> GetTranscriptionsWithSummaries(
        [FromQuery] int? recordingId = null, 
        [FromQuery] int limit = 50)
    {
        var transcriptions = await _svc.GetTranscriptionsWithSummariesAsync(recordingId, limit);
        return Ok(transcriptions);
    }

    [HttpPost("{id:int}/generate-summary")]
    public async Task<IActionResult> GenerateSummary(int id)
    {
        if (!await _aiSummaryService.IsAvailableAsync())
        {
            return BadRequest("AI Summary service is not available");
        }

        var transcription = await _svc.GetByIdAsync(id);
        if (transcription == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(transcription.FullText))
        {
            return BadRequest("Transcription has no text to summarize");
        }

        try
        {
            // Build context for the summary
            var context = BuildContextString(transcription);
            
            var summaryResult = await _aiSummaryService.SummarizeAsync(
                transcription.FullText, 
                context);

            // Update the transcription with the summary result
            await _svc.UpdateTranscriptionSummaryAsync(id, summaryResult);

            return Ok(new { 
                success = summaryResult.IsSuccessful,
                summary = summaryResult.Summary,
                error = summaryResult.ErrorMessage,
                processingTimeMs = summaryResult.ProcessingTimeMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate summary for transcription {Id}", id);
            return StatusCode(500, new { error = "Internal server error during summary generation" });
        }
    }

    [HttpGet("ai-summary-status")]
    public async Task<IActionResult> GetAiSummaryStatus()
    {
        var isAvailable = await _aiSummaryService.IsAvailableAsync();
        var healthStatus = await _aiSummaryService.GetHealthStatusAsync();
        
        return Ok(new { 
            available = isAvailable,
            healthStatus = healthStatus.ToString(),
            pendingCount = (await _svc.GetTranscriptionsNeedingSummaryAsync(1000)).Count()
        });
    }

    private string BuildContextString(Transcription transcription)
    {
        var contextParts = new List<string>();
        
        if (transcription.Recording?.Call?.TalkGroup?.Name != null)
        {
            contextParts.Add($"Talk Group: {transcription.Recording.Call.TalkGroup.Name}");
        }
        
        if (transcription.Recording?.Call?.RecordingTime != null)
        {
            contextParts.Add($"Time: {transcription.Recording.Call.RecordingTime:yyyy-MM-dd HH:mm:ss} UTC");
        }
        
        if (transcription.Language != null)
        {
            contextParts.Add($"Language: {transcription.Language}");
        }
        
        if (transcription.Confidence.HasValue)
        {
            contextParts.Add($"Transcription Confidence: {transcription.Confidence:F2}");
        }

        return contextParts.Count > 0 ? string.Join(", ", contextParts) : "";
    }
}
