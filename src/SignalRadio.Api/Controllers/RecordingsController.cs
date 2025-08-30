using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalRadio.DataAccess;
using SignalRadio.DataAccess.Services;

namespace SignalRadio.Api.Controllers2;

[ApiController]
[Route("api/[controller]")]
public class RecordingsController : ControllerBase
{
    private readonly IRecordingsService _svc;
    private readonly ILogger<RecordingsController> _logger;

    public RecordingsController(IRecordingsService svc, ILogger<RecordingsController> logger)
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
    public async Task<IActionResult> Create(Recording model)
    {
    var created = await _svc.CreateAsync(model);
    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Recording model)
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
}
