using NUnit.Framework;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using SRApi = SignalRadio.Web.Api;
using SignalRadio.Public.Lib.Models;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SignalRadio.Public.Lib.Models.TrunkRecorder;
using System.Text;
using System.Net.WebSockets;
using System.IO;

namespace SignalRadio.Web.Client.Test
{
    [TestFixture]
    public class WebSocketHandlerTest
    {
        private WebSocketClient _webSocketClient;
        private HttpClient _testClient;
        private TestServer _testServer;

        private Uri _testWebSocketsUri;

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
            _testServer = host.GetTestServer();
            
            _testWebSocketsUri = new UriBuilder(_testServer.BaseAddress) 
            {
                Scheme = "ws",
                Path = "ws"
            }.Uri;
        }

        [Test]
        public async Task WebSocketHandler_CanHandleTrunkRecorderStatusMessages()
        {
           var wsClient = _testServer.CreateWebSocketClient();
           var wsc = await wsClient.ConnectAsync(_testWebSocketsUri, CancellationToken.None);

            

        }
    }
}