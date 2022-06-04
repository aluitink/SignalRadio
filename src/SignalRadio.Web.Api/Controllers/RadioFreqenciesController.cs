using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRadio.Database.EF;
using SignalRadio.Public.Lib.Models;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RadioFreqenciesController : SignalRadioControllerBase
    {
        public RadioFreqenciesController(SignalRadioDbContext dbContext, ILogger<RadioCallsController> logger) :
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
