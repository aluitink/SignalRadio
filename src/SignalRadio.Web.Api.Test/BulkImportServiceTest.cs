using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

using SignalRadio.Database.EF;
using SignalRadio.Web.Api.Services;

namespace SignalRadio.Web.Api.Test
{
    public class BulkImportServiceTest:TestBase
    {
        private BulkImportService _bulkImportService;
        private SignalRadioDbContext _dbContext;
        private ILogger<TrunkRecorderStatusHandler> _nullLogger;

        [SetUp]
        public void Setup()
        {
            _nullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TrunkRecorderStatusHandler>.Instance;

            var options = new DbContextOptionsBuilder<SignalRadioDbContext>()
                .UseInMemoryDatabase(databaseName: "SignalRadio")
                .Options;

            _dbContext = new SignalRadioDbContext(options);
            _bulkImportService = new BulkImportService(_dbContext);
        }

        [Test]
        public async Task BulkImportService_CanImportTalkGroups()
        {
            var tempFile = Path.GetTempFileName();

            using(var fs = new FileStream(tempFile, FileMode.Create))
            {
                using (var s = GetTalkGroupFiles("danecom-talkgroups.priorities.csv"))
                {
                    await s.CopyToAsync(fs);
                }
            }

            await _bulkImportService.ImportTalkGroupsCsvAsync(tempFile);

            if(File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}