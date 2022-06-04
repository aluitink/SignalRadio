using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

using SignalRadio.Database.EF;
using SignalRadio.Public.Lib.Models.Enums;

using System.Threading.Tasks;

namespace SignalRadio.Web.Api.Test
{
    public class TrunkRecorderStatusHandlerTest : TestBase
    {
        private TrunkRecorderStatusHandler _statusHandler;
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
            _statusHandler = new TrunkRecorderStatusHandler(_dbContext, _nullLogger);
        }

        [Test]
        public async Task TrunkRecorderStatusHandler_ShouldHandle_Config()
        {
            byte[] buffer = GetTrunkRecorderAssetsAsBytes("tr-config-msg.json");
            await _statusHandler.HandleStatusMessageAsync(buffer, buffer.Length);
        }

        [Test]
        public async Task TrunkRecorderStatusHandler_ShouldHandle_System()
        {
            /*
             *         {
                            "id": "0",
                            "name": "DaneCom",
                            "type": "p25",
                            "sysid": "304",
                            "wacn": "1",
                            "nac": "304"
                        }
             */
            byte[] buffer = GetTrunkRecorderAssetsAsBytes("tr-system-msg.json");
            await _statusHandler.HandleStatusMessageAsync(buffer, buffer.Length);

            var actualSystem = await _dbContext.RadioSystems
                .FirstOrDefaultAsync(rs => rs.ShortName == "DaneCom");

            Assert.IsNotNull(actualSystem, "Could not find system.");
            Assert.AreEqual("DaneCom", actualSystem.ShortName);
            Assert.AreEqual(304, actualSystem.SystemNumber);
            Assert.AreEqual(1, actualSystem.WANC);
            Assert.AreEqual(RadioSystemType.P25, actualSystem.SystemType);
        }

        [Test]
        public async Task TrunkRecorderStatusHandler_ShouldHandle_Recorder()
        {
            byte[] buffer = GetTrunkRecorderAssetsAsBytes("tr-recorder-msg.json");            
            await _statusHandler.HandleStatusMessageAsync(buffer, buffer.Length);

            buffer = GetTrunkRecorderAssetsAsBytes("tr-recorders-msg.json");
            await _statusHandler.HandleStatusMessageAsync(buffer, buffer.Length);
        }

        [Test]
        public async Task TrunkRecorderStatusHandler_ShouldHandle_Rates()
        {
            byte[] buffer = GetTrunkRecorderAssetsAsBytes("tr-rates-msg.json");
            await _statusHandler.HandleStatusMessageAsync(buffer, buffer.Length);
        }

        [Test]
        public async Task TrunkRecorderStatusHandler_ShouldHandle_Calls()
        {
            byte[] buffer = GetTrunkRecorderAssetsAsBytes("tr-calls-active-msg.json");
            await _statusHandler.HandleStatusMessageAsync(buffer, buffer.Length);

            buffer = GetTrunkRecorderAssetsAsBytes("tr-calls-end-msg.json");
            await _statusHandler.HandleStatusMessageAsync(buffer, buffer.Length);

        }
    }
}