using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRadio.Database.EF;
using SignalRadio.Public.Lib.Models;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RadioGroupsController : SignalRadioControllerBase
    {
        public RadioGroupsController(SignalRadioDbContext dbContext, ILogger<RadioCallsController> logger):
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
