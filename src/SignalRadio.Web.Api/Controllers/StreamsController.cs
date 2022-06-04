using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRadio.Database.EF;
using SignalRadio.Public.Lib.Models;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamsController : SignalRadioControllerBase
    {
        public StreamsController(SignalRadioDbContext dbContext, ILogger<RadioCallsController> logger):
            base(dbContext, logger) { }

        [HttpGet("Identifier/{identifier}")]
        public async Task<Public.Lib.Models.Stream> GetByIdentifierAsync(string identifier)
        {
            return await Task.FromResult(DbContext.Streams.Where(tg => tg.StreamIdentifier == identifier).FirstOrDefault());
        }

        [HttpGet("{id}")]
        public async Task<Public.Lib.Models.Stream> GetById(uint id)
        {
            return await Task.FromResult(DbContext.Streams.FirstOrDefault(tg => tg.Id == id));
        }
    }
}
