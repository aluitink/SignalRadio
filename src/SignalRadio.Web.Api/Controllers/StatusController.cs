
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRadio.Database.EF;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
        [Route("api/[controller]")]
    public class StatusController : SignalRadioControllerBase
    {
        public StatusController(SignalRadioDbContext dbContext, ILogger logger) 
        : base(dbContext, logger) { }

        
    }

}