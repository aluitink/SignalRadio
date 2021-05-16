using NUnit.Framework;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using SRApi = SignalRadio.Web.Api;
using TR = SignalRadio.Public.Lib.Models.TrunkRecorder;
using SignalRadio.Public.Lib.Models;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net.WebSockets;
using System.IO;
using SignalRadio.Database.EF;
using Microsoft.EntityFrameworkCore;
using SignalRadio.Public.Lib.Models.TrunkRecorder;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace SignalRadio.Web.Client.Test
{
    [TestFixture]
    public class WebSocketHandlerTest: WebTestBase
    {
        private readonly string _testResourceFolder = "StatusServerWebSocketHandler";

        private async Task SendMessageAsync(string message)
        {
            var webSocket = await WebSocketClient.ConnectAsync(TestWebSocketsUri, CancellationToken.None);
            var msgBytes = Encoding.UTF8.GetBytes(message);

            await webSocket.SendAsync(new ArraySegment<byte>(msgBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);    
        }

        protected async Task<TMsg> WebSocketMessageTestAsync<TMsg>(string resource)
        {
            var messageJson = GetResource(resource, _testResourceFolder);
            
            var obj = JObject.Parse(messageJson);
            var message = obj.First.DeepClone();
            var statusMessage =message.ToObject<StatusMessage>();

            var typedMessage = message.First.ToObject<TMsg>();
            
            await SendMessageAsync(messageJson);

            return typedMessage;
        }

        [Test]
        public async Task WebSocketHandler_Message_Systems()
        {
            var systems = await WebSocketMessageTestAsync<TR.System[]>("message.systems.json");

            Assert.IsNotNull(systems, "Could not deserialize test data.");

            var expectedSystem = systems[0];

            Assert.IsNotNull(expectedSystem, "System was not found in collection, check test data.");

            var actualSystem = await DbContext.RadioSystems.FirstOrDefaultAsync(rs => rs.ShortName == expectedSystem.ShortName);

            Assert.IsNotNull(actualSystem, "Could not find system.");
            Assert.AreEqual(expectedSystem.ShortName, actualSystem.ShortName);
            Assert.AreEqual(expectedSystem.SystemNumber, actualSystem.SystemNumber);

            Assert.AreEqual(expectedSystem.NAC, actualSystem.NAC);
            Assert.AreEqual(expectedSystem.WACN, actualSystem.WANC);
            
        }
        [Test]
        public async Task WebSocketHandler_Message_Recorders()
        {
            var recorders = await WebSocketMessageTestAsync<TR.Recorder[]>("message.recorders.json");
            Assert.IsNotNull(recorders, "Could not deserialize test data.");

            foreach(var rec in recorders)
            {
                var expectedRecorder = recorders[0];

                Assert.IsNotNull(expectedRecorder, "System was not found in collection, check test data.");

                var actualSystem = await DbContext.RadioRecorders.FirstOrDefaultAsync(rs => rs.RecorderIdentifier == expectedRecorder.Id);
            }
        }
        [Test]
        public async Task WebSocketHandler_Message_Recorder()
        {
            await SendMessageAsync(GetResource("message.recorder.json"));
        }
        [Test]
        public async Task WebSocketHandler_Message_CallStart()
        {
            await SendMessageAsync(GetResource("message.call_start.json"));

            Console.ReadLine();
        }
        [Test]
        public async Task WebSocketHandler_Message_CallsActive_Single()
        {
            await SendMessageAsync(GetResource("message.calls_active.single.json"));
        }
        [Test]
        public async Task WebSocketHandler_Message_CallsActive_Empty()
        {
            await SendMessageAsync(GetResource("message.calls_active.empty.json"));
        }
    }
}