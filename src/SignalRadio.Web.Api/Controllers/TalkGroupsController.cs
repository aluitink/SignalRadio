using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SignalRadio.Database.EF;
using SignalRadio.Public.Lib.Models;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TalkGroupsController : SignalRadioControllerBase
    {
        public TalkGroupsController(SignalRadioDbContext dbContext, ILogger<TalkGroupsController> logger):
            base(dbContext, logger) { }

        [HttpGet("Identifier/{identifier}")]
        public async Task<TalkGroup> GetByIdentifierAsync(ushort identifier)
        {
            return await Task.FromResult(DbContext.TalkGroups.Where(tg => tg.Identifier == identifier).FirstOrDefault());
        }

        [HttpGet("{id}")]
        public async Task<TalkGroup> GetById(uint id)
        {
            return await Task.FromResult(DbContext.TalkGroups.FirstOrDefault(tg => tg.Id == id));
        }

        [HttpGet]
        public async Task<IEnumerable<TalkGroup>> GetAsync()
        {
            return await Task.FromResult(DbContext.TalkGroups);
        }

        [HttpPost("Import")]
        public async Task<TalkGroupImportResults> ImportTalkGroupsAsync()
        {
            var tempFilePath = Path.GetTempFileName();
            try
            {
                var stream = HttpContext.Request.Body;    
                using(var tempFileStream = System.IO.File.Create(tempFilePath))
                {
                    await stream.CopyToAsync(tempFileStream);
                }

                var talkGroups = SignalRadio.Public.Lib.Helpers.FileHelpers.TalkGroupsFromCsv(tempFilePath);
                foreach(var talkGroup in talkGroups)
                {
                    try
                    {
                        var tGroup = DbContext.TalkGroups
                            .Where(tg => tg.Identifier  == talkGroup.Identifier)
                            .FirstOrDefault();

                        if(tGroup == null)
                            tGroup = (await DbContext.TalkGroups.AddAsync(talkGroup)).Entity;

                        
                    }
                    catch(Exception e)
                    {

                    }
                    finally
                    {
                        await DbContext.SaveChangesAsync();
                    }
                }
                
                return new TalkGroupImportResults()
                {
                    Success = true
                };
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e.ToString());
                return new TalkGroupImportResults();
            }
            finally
            {
                if(System.IO.File.Exists(tempFilePath))
                    System.IO.File.Delete(tempFilePath);
            }

        }
    }
}
