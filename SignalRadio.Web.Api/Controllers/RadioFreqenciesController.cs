using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRadio.Web.Api.Database;
using SignalRadio.Web.Api.Models;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RadioFreqenciesController : SignalRadioControllerBase
    {
        public RadioFreqenciesController(SignalRadioDbContext dbContext, ILogger<TalkGroupsController> logger):
            base(dbContext, logger) { }

        [HttpGet]
        public IEnumerable<RadioFrequency> Get()
        {
            Logger.LogInformation("Get RadioFrequencies");
            return new List<RadioFrequency>() {
                new RadioFrequency(){
                    
                }
            };
        }
    }

}
