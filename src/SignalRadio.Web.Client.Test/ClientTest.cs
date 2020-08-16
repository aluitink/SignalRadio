using NUnit.Framework;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using SRApi = SignalRadio.Web.Api;
using SignalRadio.Public.Lib.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRadio.Web.Client.Test
{
    [TestFixture]
    public class ClientTest
    {
        private SignalRadioClient _signalRadioClient;

        [SetUp]
        public void Setup()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    // Add TestServer
                    webHost.UseTestServer();
                    webHost.UseStartup<SRApi.Startup>();
                });

            // Create and start up the host
            var host = hostBuilder.Start();
            
            // Create an HttpClient which is setup for the test host
            var client = host.GetTestClient();

            var connectionString = "http://localhost/api";

            _signalRadioClient = new SignalRadioClient(connectionString, client);
        }

        [Test]
        public async Task Test1()
        {
            try
            {
                var call = new RadioCall()
                {
                    
                };

                await _signalRadioClient.PostCallAsync(call, CancellationToken.None);
            }
            catch
            {

            }
            
        }
    }
}