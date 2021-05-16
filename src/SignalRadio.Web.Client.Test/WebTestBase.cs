using NUnit.Framework;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using SRApi = SignalRadio.Web.Api;
using System;
using System.Net.Http;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using SignalRadio.Database.EF;
using SignalRadio.Web.Api.Services;
using System.Threading.Tasks;

namespace SignalRadio.Web.Client.Test
{
    public class WebTestBase
    {
        protected SignalRadioDbContext DbContext {get; set; }
        protected IHost TestHost { get; set; }
        protected HttpClient TestClient { get; set; }
        protected TestServer TestServer { get; set; }
        protected WebSocketClient WebSocketClient { get; set; }

        protected Uri TestApiUri { get; set; }
        protected Uri TestWebSocketsUri { get; set; }

        [OneTimeSetUp]
        protected virtual void Init()
        {
            var unitTestDbFile = "SignalRadio-UnitTest.db";

            if(File.Exists(unitTestDbFile))
                File.Delete(unitTestDbFile);

            var connectionString = string.Format("Filename={0}", unitTestDbFile);

            var kvp = new KeyValuePair<string, string>("ConnectionStrings:SignalRadioDb", connectionString);
        
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new [] { kvp });

            connectionString = configurationBuilder
                .Build()
                    .GetConnectionString("SignalRadioDb");

            DbContext = new SignalRadioDbContext(connectionString);
            DbContext.Database.EnsureCreated();

            InitTestDataAsync().GetAwaiter().GetResult();

            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.UseStartup<SRApi.Startup>();
                })
                .ConfigureAppConfiguration((hostContext, builder) => 
                {
                    builder.AddInMemoryCollection(new [] { kvp });
                });

            TestHost = hostBuilder.Start();
        }

        [OneTimeTearDown]
        protected virtual void Cleanup()
        {
            TestClient?.Dispose();
            TestServer?.Dispose();
            TestHost?.Dispose();
            DbContext?.Dispose();
        }

        [SetUp]
        protected virtual void Setup()
        {
            TestClient = TestHost.GetTestClient();
            TestServer = TestHost.GetTestServer();
            WebSocketClient = TestServer.CreateWebSocketClient();

            TestApiUri = new UriBuilder(TestServer.BaseAddress)
            {
                Path = "/api/"
            }.Uri;

            TestWebSocketsUri = new UriBuilder(TestServer.BaseAddress) 
            {
                Scheme = "ws",
                Path = "/ws/"
            }.Uri;
        }

        [TearDown]
        protected virtual void TearDown()
        {
            TestClient?.Dispose();
            TestServer?.Dispose();
            WebSocketClient = null;
        }

        protected async Task InitTestDataAsync()
        {
            var tgFile = "Resources/sample-talkgroups.csv";
            using(var bulkImportService = new BulkImportService(DbContext))
            {
                var result = await bulkImportService.ImportTalkGroupsCsvAsync(tgFile);
                if(!result.IsSuccessful)
                    throw new Exception("Failed to initialize test data.");
            }
        }

        protected string GetResource(string fileName, string folderName = null)
        {
            string path = string.Empty;
            if(!string.IsNullOrWhiteSpace(folderName))
                path = Path.Combine("Resources", folderName);
                
            path = Path.Combine(path, fileName);
            if(!File.Exists(path))
                throw new FileNotFoundException("Could not find test resource", path);

            return File.ReadAllText(path);
        }
    }
}