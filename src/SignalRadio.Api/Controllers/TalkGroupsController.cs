using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalRadio.DataAccess;
using SignalRadio.DataAccess.Services;
using SignalRadio.Api.Extensions;
using SignalRadio.Api.Dtos;
using System;
using System.Linq;

namespace SignalRadio.Api.Controllers2;

[ApiController]
[Route("api/[controller]")]
public class TalkGroupsController : ControllerBase
{
    private readonly ITalkGroupsService _svc;
    private readonly ICallsService _callsService;
    private readonly ILogger<TalkGroupsController> _logger;

    public TalkGroupsController(ITalkGroupsService svc, ICallsService callsService, ILogger<TalkGroupsController> logger)
    {
        _svc = svc;
        _callsService = callsService;
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

    [HttpGet("{id:int}/calls")]
    public async Task<IActionResult> GetCallsByTalkGroupId(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "recordingTime", [FromQuery] string sortDir = "desc")
    {
        // First verify the talkgroup exists
        var talkGroup = await _svc.GetByIdAsync(id);
        if (talkGroup == null) return NotFound($"TalkGroup with ID {id} not found");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var result = await _callsService.GetAllCallsByTalkGroupAsync(id, page, pageSize, sortBy, sortDir);
        
        // Convert to DTOs with TalkGroup information included
        var apiBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var dtoResult = new 
        {
            Items = result.Items.Select(call => call.ToDto(apiBaseUrl)).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages
        };
        
        return Ok(dtoResult);
    }

    [HttpGet("{id:int}/calls-by-frequency")]
    public async Task<IActionResult> GetCallsByFrequencyForTalkGroup(int id, [FromQuery] int limit = 50)
    {
        // First verify the talkgroup exists
        var talkGroup = await _svc.GetByIdAsync(id);
        if (talkGroup == null) return NotFound($"TalkGroup with ID {id} not found");

        limit = Math.Clamp(limit, 1, 1000);
        
        var result = await _callsService.GetCallsByFrequencyForTalkGroupAsync(id, limit);
        
        // Convert to DTOs with TalkGroup information included
        var apiBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var dtoResult = result.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(call => call.ToDto(apiBaseUrl)).ToList()
        );
        
        return Ok(dtoResult);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TalkGroup model)
    {
    var created = await _svc.CreateAsync(model);
    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TalkGroup model)
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

    [HttpPost("import")]
    public async Task<IActionResult> Import()
    {
        if (!Request.HasFormContentType) return BadRequest("Expected form data with file field 'file'");
        var form = await Request.ReadFormAsync();
        var file = form.Files.GetFile("file");
        if (file == null) return BadRequest("No file uploaded");

        using var stream = file.OpenReadStream();
        using var reader = new System.IO.StreamReader(stream);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            lines.Add(line);
        }

    // Assume CSV header present or rows matching: Decimal,Hex,Mode,Alpha Tag,Description,Tag,Category,Priority
        var imported = 0;
        foreach (var raw in lines)
        {
            var cols = raw.Split(',');
            // Skip header row if first col not numeric
            if (!int.TryParse(cols[0], out var number)) continue;

            var model = new TalkGroup
            {
                Number = number,
                // CSV layout: 0=Decimal,1=Hex,2=Mode,3=Alpha Tag,4=Description,5=Tag,6=Category,7=Priority
                AlphaTag = cols.Length > 3 ? cols[3].Trim() : null,
                Description = cols.Length > 4 ? cols[4].Trim() : null,
                Tag = cols.Length > 5 ? cols[5].Trim() : null,
                Category = cols.Length > 6 ? cols[6].Trim() : null,
            };

        if (cols.Length > 7 && int.TryParse(cols[7], out var p)) model.Priority = p;

            // Upsert: if a TalkGroup with same Number exists, update it; otherwise create
            var existing = await _svc.GetAllAsync(1, 1);
            // Simple lookup via DB context would be more efficient; use service create/update for now.
            var found = (await _svc.GetAllAsync(1, int.MaxValue)).Items.FirstOrDefault(t => t.Number == number);
            if (found != null)
            {
                model.Id = found.Id;
                await _svc.UpdateAsync(found.Id, model);
            }
            else
            {
                await _svc.CreateAsync(model);
            }
            imported++;
        }

        return Ok(new { imported });
    }
}
