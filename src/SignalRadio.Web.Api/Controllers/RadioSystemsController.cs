using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRadio.Database.EF;
using SignalRadio.Public.Lib.Models;
using SignalRadio.Public.Lib.Models.Enums;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RadioSystemsController : SignalRadioControllerBase
    {
        public RadioSystemsController(SignalRadioDbContext dbContext, ILogger<RadioCallsController> logger):
            base(dbContext, logger) { }


        [HttpPatch]
        public async Task<bool> PatchOneAsync(uint id, [FromBody]RadioSystem radioSystem)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<RadioSystem> PostOneAsync([FromBody]RadioSystem radioSystem)
        {
            throw new NotImplementedException();
        }


        [HttpGet]
        public async Task<IEnumerable<RadioSystem>> GetAsync()
        {
            throw new NotImplementedException();
            //await DbContext.RadioSystems.AddAsync(new RadioSystem() 
            //{
            //    City = "test",
            //    State = "testState",
            //    County = "TestCounty",
            //    SystemType = RadioSystemType.P25Phase2,
            //    SystemVoice = RadioSystemVoice.APCO25,
            //    LastUpdatedUtc = DateTime.UtcNow,
            //    ControlFrequencies = new Collection<RadioFrequency>()
            //    {
            //        new RadioFrequency()
            //        {
            //            FrequencyHz = 172000000,
            //            ControlData = true,
            //        },
            //        new RadioFrequency()
            //        {
            //            FrequencyHz = 174000000,
            //            ControlData = false,
            //        }
            //    }
            //});

            //await DbContext.SaveChangesAsync();

            //return DbContext.RadioSystems;
        }

        [HttpGet("{id}")]
        public async Task<RadioSystem> GetOneAsync(uint id)
        {
            throw new NotImplementedException();
            //return await Task.FromResult(DbContext.RadioSystems.FirstOrDefault(r => r.Id == id));
        }

        [HttpDelete]
        public async Task<bool> DeleteAsync(uint id)
        {
            throw new NotImplementedException();
        }
    }


}
