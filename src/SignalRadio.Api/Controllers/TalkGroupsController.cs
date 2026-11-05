using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalRadio.Api.Services;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data;

namespace SignalRadio.Api.Controllers;

[ApiController]
[Route("api/talkgroups")]
public class TalkGroupsController(ITalkGroupsService talkGroups, SignalRadioDbContext db, IConfiguration config, ILogger<TalkGroupsController> logger) : ControllerBase
{
    /// <summary>
    /// Re-imports talk group definitions from the configured CSV
    /// (TalkGroups:CsvPath). Existing rows are enriched, never overwritten.
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(CancellationToken ct)
    {
        var csvPath = config["TalkGroups:CsvPath"];
        if (string.IsNullOrWhiteSpace(csvPath))
            return BadRequest("TalkGroups:CsvPath is not configured.");

        var result = await TalkGroupCsvImporter.ImportFromFileAsync(db, csvPath, logger, ct);
        return Ok(new { created = result.Created, updated = result.Updated, skipped = result.Skipped, totalRows = result.TotalRows });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await talkGroups.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await talkGroups.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("number/{number}")]
    public async Task<IActionResult> GetByNumber(string number, CancellationToken ct)
    {
        var result = await talkGroups.GetByNumberAsync(number, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTalkGroupRequest request, CancellationToken ct) =>
        CreatedAtAction(nameof(GetById), new { id = (await talkGroups.CreateAsync(request, ct)).Id }, null);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTalkGroupRequest request, CancellationToken ct)
    {
        var result = await talkGroups.UpdateAsync(id, request, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await talkGroups.DeleteAsync(id, ct);
        return NoContent();
    }
}
