using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRadio.Web.Api.Database;
using SignalRadio.Web.Api.Models;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TalkGroupsController : SignalRadioControllerBase
    {
        public TalkGroupsController(SignalRadioDbContext dbContext, ILogger<TalkGroupsController> logger):
            base(dbContext, logger) { }

        [HttpGet]
        public IEnumerable<TalkGroup> Get()
        {
            Logger.LogInformation("Get TalkGroups");
            return new List<TalkGroup>() {
                new TalkGroup(){
                    
                }
            };
        }
    }
}
