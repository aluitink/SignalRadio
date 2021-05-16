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
using SignalRadio.Web.Api.Services;
using Stream = SignalRadio.Public.Lib.Models.Stream;

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

        [HttpGet("{id}/Streams")]
        public async Task<IEnumerable<Stream>> GetStreamsByTalkGroupId(uint id)
        {
            return await Task.FromResult(DbContext.TalkGroupStreams
                .Where(tgs => tgs.TalkGroupId == id)
                    .Select(tgs => tgs.Stream)
            );
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
                using(var fs = new FileStream(tempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 2048, true))
                {
                    await HttpContext.Request.Body.CopyToAsync(fs);
                    using(var bulkImportService = new BulkImportService(DbContext))
                        return await bulkImportService.ImportTalkGroupsCsvAsync(tempFilePath);
                }    
            }
            finally
            {
                if(System.IO.File.Exists(tempFilePath))
                    System.IO.File.Delete(tempFilePath);
            }
            
        }
    }
}
