using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SignalRadio.Database.EF;
using SignalRadio.Public.Lib.Models;

namespace SignalRadio.Web.Api.Controllers
{
    public class ResultSet<TM>
    {
        public Collection<TM> Results { get;set; }
        public int TotalResults { get;set; }
        public int PageNumber { get;set; }
    }


    [ApiController]
    [Route("api/[controller]")]
    public class RadioCallsController : SignalRadioControllerBase
    {
        public RadioCallsController(SignalRadioDbContext dbContext, ILogger<RadioCallsController> logger):
            base(dbContext, logger) { }

        [HttpPost]
        public async Task<RadioCall> PostOneAsync([FromBody]RadioCall radioCall)
        {
            throw new NotImplementedException();
            //using (var scope = ServiceScopeFactory.CreateScope())
            //{
            //    using (var ctx = scope.ServiceProvider.GetRequiredService<SignalRadioDbContext>())
            //    {
            //        var tgIdent = radioCall.TalkGroupIdentifier;

            //        if (radioCall.TalkGroupId <= 0)
            //        {
            //            var talkGroup = await DbContext
            //                            .TalkGroups
            //                            .FirstOrDefaultAsync(t => t.Identifier == tgIdent);
            //            if (talkGroup == null)
            //            {
            //                talkGroup = new TalkGroup()
            //                {
            //                    Identifier = tgIdent
            //                };

            //                var tgEntity = await DbContext
            //                    .TalkGroups
            //                    .AddAsync(talkGroup);

            //                await DbContext.SaveChangesAsync();

            //                talkGroup = tgEntity.Entity;
            //            }

            //            radioCall.TalkGroupId = talkGroup.Id;
            //        }

            //        var radioCallEntity = await DbContext.RadioCalls.AddAsync(radioCall);
            //        await DbContext.SaveChangesAsync();

            //        return radioCallEntity.Entity;
            //    }
            //}
        }

        [HttpGet]
        public IEnumerable<RadioCall> Get()
        {
            Logger.LogInformation("Get RadioCalls");
            return new List<RadioCall>() {
                new RadioCall(){
                    
                }
            };
        }
    }
}
