using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRadio.Web.Api.Database;
using SignalRadio.Web.Api.Models;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RadioGroupsController : SignalRadioControllerBase
    {
        public RadioGroupsController(SignalRadioDbContext dbContext, ILogger<TalkGroupsController> logger):
            base(dbContext, logger) { }

        [HttpGet]
        public IEnumerable<RadioGroup> Get()
        {
            Logger.LogInformation("Get RadioGroups");
            return new List<RadioGroup>() {
                new RadioGroup(){
                    
                }
            };
        }
    }

}
