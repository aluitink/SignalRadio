using Microsoft.AspNetCore.Mvc;
using SignalRadio.Api.Services;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/investigations")]
public class InvestigationsController(IInvestigationsService investigations, InvestigationChatService chat) : ControllerBase
{
    /// <summary>
    /// POST /api/investigations
    /// Create a new investigation (queued for background processing).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvestigationRequest request, CancellationToken ct)
    {
        if (request.CallId <= 0)
            return BadRequest(new { error = "callId is required" });
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "prompt is required" });

        try
        {
            var dto = await investigations.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/investigations
    /// List investigations, optionally filtered.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] long? callId,
        [FromQuery] bool includeViewed = true,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var items = await investigations.ListAsync(status, callId, includeViewed, limit, ct);
        return Ok(items);
    }

    /// <summary>
    /// GET /api/investigations/unviewed
    /// Get unviewed completed investigations (for the ticker).
    /// </summary>
    [HttpGet("unviewed")]
    public async Task<IActionResult> GetUnviewed([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var items = await investigations.GetUnviewedCompletedAsync(limit, ct);
        return Ok(items);
    }

    /// <summary>
    /// GET /api/investigations/{id}
    /// Get a single investigation by ID.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var dto = await investigations.GetByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// POST /api/investigations/{id}/view
    /// Mark an investigation as viewed (dismisses it from the ticker).
    /// </summary>
    [HttpPost("{id:long}/view")]
    public async Task<IActionResult> MarkViewed(long id, CancellationToken ct)
    {
        await investigations.MarkViewedAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// DELETE /api/investigations/{id}
    /// Delete an investigation and its follow-ups.
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await investigations.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/investigations/{id}/chat
    /// Send a message to the investigation's chat. Runs the agent turn in the background
    /// (streaming events to the UI over SignalR) and returns immediately. The agent session
    /// is resumed from the persisted MAF AgentSession, so this works for any follow-up
    /// question.
    /// </summary>
    [HttpPost("{id:long}/chat")]
    public async Task<IActionResult> Chat(long id, [FromBody] InvestigationChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "message is required" });

        var inv = await investigations.GetByIdAsync(id, ct);
        if (inv is null)
            return NotFound(new { error = "investigation not found" });

        // Fire-and-forget: the turn streams to the UI over SignalR and persists its own state.
        _ = Task.Run(async () =>
        {
            try
            {
                await chat.RunTurnAsync(id, request.Message, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // RunTurnAsync already handles + notifies errors; safety net.
                System.Diagnostics.Debug.WriteLine($"Investigation chat turn failed: {ex}");
            }
        }, CancellationToken.None);

        return Accepted(new { investigationId = id });
    }
}

/// <summary>Request body for the investigation chat endpoint.</summary>
public record InvestigationChatRequest(string Message);
