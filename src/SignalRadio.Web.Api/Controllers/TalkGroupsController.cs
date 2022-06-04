
using Microsoft.AspNetCore.Mvc;

using SignalRadio.Database.EF;
using SignalRadio.Public.Lib.Models;
using SignalRadio.Web.Api.Services;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TalkGroupsController : SignalRadioControllerBase
    {
        public TalkGroupsController(SignalRadioDbContext dbContext, ILogger<WebSocketsController> logger):
            base(dbContext, logger) { }
        
        [HttpPost("Import")]
        public async Task<TalkGroupImportResults> ImportTalkGroupsAsync()
        {
            var tempFilePath = Path.GetTempFileName();
            try
            {
                using (var fs = new FileStream(tempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 2048, true))
                {
                    await HttpContext.Request.Body.CopyToAsync(fs);
                }

                return await (new BulkImportService(DbContext)).ImportTalkGroupsCsvAsync(tempFilePath);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to import Talk Groups");
                throw;
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath))
                    System.IO.File.Delete(tempFilePath);
            }
        }
    }
}
