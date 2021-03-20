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
    public class SignalRadioClientTest
    {
        private SignalRadioClient _signalRadioClient;
        private WebSocketClient _webSocketClient;
        private HttpClient _testClient;
        private TestServer _testServer;

        private Uri _testWebSocketsUri;
        private Uri _testApiUri;

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
            _testClient = _testServer.CreateClient();
            
            _testApiUri = new UriBuilder(_testServer.BaseAddress)
            {
                Path = "api"
            }.Uri;
            
            _testWebSocketsUri = new UriBuilder(_testServer.BaseAddress) 
            {
                Scheme = "ws",
                Path = "ws"
            }.Uri;

            _signalRadioClient = new SignalRadioClient(_testApiUri, _testClient);
        }


        [Test]
        public async Task SignalRadioClient_CanPostCall()
        {
            var radioCall = new RadioCall()
            {
                TalkGroupIdentifier = 123,
                CallState = 123,
                CallRecordState = 123,
                CallIdentifier = "call",
                TalkGroupTag = "abc",
                Elapsed = 123,
                Length = 123,
                IsPhase2 = true,
                IsConventional = false,
                IsEncrypted = false,
                IsAnalog = false,
                StartTime = 123,
                StopTime = 123,
                FrequencyHz = 1233123123,
                Frequency = 123,
                CallSerialNumber = 123,
                CallWavPath = "thisisthewave",
                SigmfFileName = "sigmf",
                DebugFilename = "debug",
                Filename = "filename",
                StatusFilename = "filename"
                
                

            };

        }
    }
}