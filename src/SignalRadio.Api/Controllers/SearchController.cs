using Microsoft.AspNetCore.Mvc;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(ISearchService search) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool semantic = false,
        [FromQuery] bool hybrid = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query parameter 'q' is required.");
        return Ok(await search.SearchAsync(q, page, pageSize, semantic, hybrid, ct));
    }

    [HttpGet("summaries")]
    public async Task<IActionResult> SearchSummaries([FromQuery] string q, [FromQuery] int count = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query parameter 'q' is required.");
        return Ok(await search.SearchSummariesAsync(q, count, ct));
    }

    // Cursor-paged summary search (newest first). Pass the previous page's
    // cursor (beforeTime + beforeId) to fetch the next page.
    [HttpGet("summaries/paged")]
    public async Task<IActionResult> SearchSummariesPaged(
        [FromQuery] string q,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTimeOffset? beforeTime = null,
        [FromQuery] long? beforeId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query parameter 'q' is required.");
        return Ok(await search.SearchSummariesPagedAsync(q, pageSize, beforeTime, beforeId, ct));
    }

    [HttpGet("incidents")]
    public async Task<IActionResult> SearchIncidents([FromQuery] string q, [FromQuery] int count = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query parameter 'q' is required.");
        return Ok(await search.SearchIncidentsAsync(q, count, ct));
    }
}
