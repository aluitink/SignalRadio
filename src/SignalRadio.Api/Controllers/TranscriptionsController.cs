using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SignalRadio.DataAccess;
using SignalRadio.DataAccess.Services;

namespace SignalRadio.Api.Controllers2;

[ApiController]
[Route("api/[controller]")]
public class TranscriptionsController : ControllerBase
{
    private readonly ITranscriptionsService _svc;
    private readonly ILogger<TranscriptionsController> _logger;

    public TranscriptionsController(ITranscriptionsService svc, ILogger<TranscriptionsController> logger)
    {
        _svc = svc;
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

    var result = await _svc.SearchAsync(q, page, pageSize);
    return Ok(result);
    }
}
