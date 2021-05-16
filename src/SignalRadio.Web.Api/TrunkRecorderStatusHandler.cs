using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using SignalRadio.Database.EF;
using SignalRadio.Public.Lib.Models;
using SignalRadio.Public.Lib.Models.TrunkRecorder;
using TR = SignalRadio.Public.Lib.Models.TrunkRecorder;

namespace SignalRadio.Web.Api
{
    public class TrunkRecorderStatusHandler
    {
        protected bool IsTraceEnabled { get; }
        protected SignalRadioDbContext DbContext { get; }
        public TrunkRecorderStatusHandler(SignalRadioDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public async Task StartStatusMessageHandlerAsync(HttpContext context,
                                            WebSocket webSocket,
                                            CancellationToken cancellationToken = default(CancellationToken))
        {
            await DbContext.Database.EnsureCreatedAsync(cancellationToken);

            var buffer = new byte[1024 * 16];
            WebSocketReceiveResult result = null;
            do
            {
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    
                    if(!result.CloseStatus.HasValue)
                        await HandleStatusMessageAsync(buffer, result.Count, cancellationToken);    
                }
                catch (System.Exception e)
                {
                    
                }
            }
            while (!result.CloseStatus.HasValue);

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, cancellationToken);
        }
        private async Task HandleStatusMessageAsync(byte[] messageBuffer, int count, 
                                            CancellationToken cancellationToken = default(CancellationToken))
        {   
            var jsonString = ToUtf8(messageBuffer, count);

            if(IsTraceEnabled)
                TraceMessage(jsonString);

            var obj = JObject.Parse(jsonString);
            var message = obj.First.DeepClone();
            
            obj.First.Remove();
            var statusMessage = obj.ToObject<StatusMessage>();
            
            switch(statusMessage.Type)
            {
                case "config":
                    break;
                case "rates":
                    var rates = message.First.ToObject<List<Rate>>();
                    await HandleRatesAsync(rates, cancellationToken);
                    break;
                case "systems":
                    var systems = message.First.ToObject<List<TR.System>>();
                    await HandleSystemsAsync(systems, cancellationToken);
                    break;
                case "calls_active":
                    var calls = message.First.ToObject<List<Call>>();
                    await HandleCallsActiveAsync(calls, cancellationToken);
                    break;
                case "call_start":
                case "call_end":
                    var call = message.First.ToObject<Call>();
                    await HandleCallsActiveAsync(new [] { call }, cancellationToken);
                    break;
                case "recorders":
                    var recorder = message.First.ToObject<Recorder>();
                    await HandleRecordersAsync(new List<Recorder>() { recorder }, cancellationToken);
                    break;
                case "recorder":
                    var recorders = message.First.ToObject<List<Recorder>>();
                    await HandleRecordersAsync(recorders, cancellationToken);
                    break;
            }

            await DbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task HandleRatesAsync(List<Rate> rates, CancellationToken cancellationToken = default(CancellationToken))
        {

        }

        private async Task HandleSystemsAsync(List<TR.System> systems, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var sys in systems)
            {
                var radioSystem = await DbContext
                                    .RadioSystems.FirstOrDefaultAsync(rs => rs.Name == sys.ShortName);

                if(radioSystem == null)
                {
                    radioSystem = RadioSystem.FromSystem(sys);
                    await DbContext.RadioSystems.AddAsync(radioSystem, cancellationToken);
                }
                else
                {
                    radioSystem.UpdateFromSystem(sys);
                }
            }
        }

        private async Task HandleCallsActiveAsync(IEnumerable<Call> calls, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach(var call in calls)
            {
                var radioCall = await DbContext.RadioCalls.FirstOrDefaultAsync(c => c.CallIdentifier == call.Id);

                if(radioCall == null)
                {
                    radioCall = RadioCall.FromCall(call);

                    await DbContext
                        .RadioCalls.AddAsync(radioCall, cancellationToken);
                }
                else
                {
                    radioCall.UpdateFromCall(call);
                }
            }
        }

        private async Task HandleRecordersAsync(List<Recorder> recorders, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var recorder in recorders)
            {
                var radioRecorder = await DbContext.Recorders
                                        .FirstOrDefaultAsync(r => r.RecorderIdentifier == recorder.Id);

                if(radioRecorder == null)
                {
                    radioRecorder = RadioRecorder.FromRecorder(recorder);
                    await DbContext.Recorders.AddAsync(radioRecorder, cancellationToken);
                }
                else
                {
                    radioRecorder.UpdateFromRecorder(recorder);
                } 
            }

            await DbContext.SaveChangesAsync();
        }

        private void TraceMessage(string jsonString)
        {
            if(!Directory.Exists("TRACE"))
                Directory.CreateDirectory("TRACE");
                
            var tracePath = Path.Combine("TRACE", String.Format("tr-msg-{0}", DateTime.Now.Ticks));
            File.WriteAllText(tracePath, jsonString);
        }
        private string ToUtf8(byte[] ba, int resultCount)
        {
            return Encoding.UTF8.GetString(ba, 0, resultCount);
        }
        private string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }
}