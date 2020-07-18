
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRadio.Web.Api.Database;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : SignalRadioControllerBase
    {
        public StatusController(SignalRadioDbContext dbContext, ILogger logger) 
        : base(dbContext, logger) { }

        
    }

}