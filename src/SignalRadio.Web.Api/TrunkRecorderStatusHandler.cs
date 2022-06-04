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

        protected ISignalRadioDbContext DbContext { get; }
        protected ILogger Logger { get; }

        public TrunkRecorderStatusHandler(ISignalRadioDbContext dbContext, ILogger logger)
        {
            DbContext = dbContext;
            Logger = logger;
        }

        public async Task StartStatusMessageHandlerAsync(HttpContext context,
                                            WebSocket webSocket,
                                            CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInformation("Starting Trunk Recorder Status Handler - Client: {0}", context.Connection.RemoteIpAddress);
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
                    Logger.LogError(e, "Error in status message handler");
                }
            }
            while (result != null && !result.CloseStatus.HasValue);

            var closeStatus = WebSocketCloseStatus.Empty;
            var closeStatusDescription = string.Empty;

            if (result != null)
            {
                closeStatus = result.CloseStatus.GetValueOrDefault();
                closeStatusDescription = result.CloseStatusDescription;
            }

            await webSocket.CloseAsync(closeStatus, closeStatusDescription, cancellationToken);
        }
        public async Task HandleStatusMessageAsync(byte[] messageBuffer, int count, 
                                            CancellationToken cancellationToken = default(CancellationToken))
        {
            var messageString = ToUtf8(messageBuffer, count);

            if(IsTraceEnabled)
                TraceMessage(messageString);

            var messageObject = JObject.Parse(messageString);
            if (messageObject == null)
                throw new Exception("Could not parse status message");

            var statusMessage = messageObject.ToObject<StatusMessage>();
            if (statusMessage == null)
                throw new Exception("Could not convert to StatusMessage");

            switch(statusMessage.Type)
            {
                case "config":
                    var sources = messageObject["sources"]?.ToObject<List<Source>>();
                    var sys = messageObject["systems"]?.ToObject<List<TR.System>>();

                    await HandleSourcesAsync(sources, cancellationToken);
                    await HandleSystemsAsync(sys, cancellationToken);
                    break;
                case "rates":
                    var rates = messageObject["rates"]?.ToObject<List<TR.Rate>>();
                    await HandleRatesAsync(rates, cancellationToken);
                    break;
                case "systems":
                    var systems = messageObject["systems"]?.ToObject<List<TR.System>>();
                    await HandleSystemsAsync(systems, cancellationToken);
                    break;
                case "calls_active":
                    var calls = messageObject["calls"]?.ToObject<List<TR.Call>>();
                    await HandleCallsActiveAsync(calls, cancellationToken);
                    break;
                case "call_start":
                case "call_end":
                    var call = messageObject["call"]?.ToObject<Call>();
                    await HandleCallsActiveAsync(new [] { call }, cancellationToken);
                    break;
                case "recorders":
                    var recorders = messageObject["recorders"]?.ToObject<List<TR.Recorder>>();
                    await HandleRecordersAsync(recorders, cancellationToken);
                    break;
                case "recorder":
                    var recorder = messageObject["recorder"]?.ToObject<Recorder>();
                    await HandleRecordersAsync(new [] { recorder } , cancellationToken);
                    break;
            }
        }

        private async Task HandleSourcesAsync(IEnumerable<TR.Source> sources, CancellationToken cancellationToken)
        {
            if (sources == null)
                return;

            foreach (var source in sources)
            {
                var radioSource = await DbContext.RadioSources.FirstOrDefaultAsync(rs => rs.SourceNumber == source.SourceNumber && rs.Device == source.Device);
                if (radioSource == null)
                {
                    radioSource = RadioSource.FromSource(source);
                    await DbContext.RadioSources.AddAsync(radioSource, cancellationToken);
                }
                else
                {
                    radioSource.UpdateFromSource(source);
                }

                Logger.LogInformation(radioSource.ToString());
            }
            await DbContext.SaveChangesAsync(cancellationToken);

        }

        private async Task HandleRatesAsync(IEnumerable<TR.Rate> rates, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (rates == null)
                return;

            foreach (var rate in rates)
            {
                Logger.LogInformation("DecodeRate: {0}", rate.Decoderate);
            }
        }

        private async Task HandleSystemsAsync(IEnumerable<TR.System> systems, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (systems == null)
                return;

            foreach (var sys in systems)
            {
                var systemId = sys.SystemNumber > 0 ? sys.SystemNumber : sys.SystemId;

                var radioSystem = await DbContext
                                    .RadioSystems.FirstOrDefaultAsync(rs => rs.ShortName == sys.ShortName && rs.SystemNumber == systemId);

                if(radioSystem == null)
                {
                    radioSystem = RadioSystem.FromSystem(sys);
                    await DbContext.RadioSystems.AddAsync(radioSystem, cancellationToken);
                }
                else
                {
                    radioSystem.UpdateFromSystem(sys);
                }

                Logger.LogInformation(sys.ToString());
            }
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task HandleCallsActiveAsync(IEnumerable<TR.Call> calls, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (calls == null)
                return;
            foreach (var call in calls)
            {
                var radioCall = await DbContext.RadioCalls
                    .FirstOrDefaultAsync(c => c.CallIdentifier == call.Id);
                if(radioCall == null)
                {
                    var tgIdentifier = ushort.Parse(call.Talkgroup);

                    var existingTalkgroup = await DbContext.TalkGroups
                        .FirstOrDefaultAsync(tg => tg.Identifier == tgIdentifier);

                    if(existingTalkgroup == null)
                    {
                        existingTalkgroup = new TalkGroup()
                        {
                            Identifier = tgIdentifier,
                            AlphaTag = call.Talkgrouptag
                        };
                        await DbContext.TalkGroups
                            .AddAsync(existingTalkgroup, cancellationToken);
                        await DbContext.SaveChangesAsync();
                    }

                    radioCall = RadioCall.FromCall(call);
                    radioCall.TalkGroupId = existingTalkgroup.Id;

                    await DbContext.RadioCalls
                        .AddAsync(radioCall, cancellationToken);
                }
                else
                {
                    radioCall.UpdateFromCall(call);
                }

                Logger.LogInformation(radioCall.ToString());
            }
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task HandleRecordersAsync(IEnumerable<TR.Recorder> recorders, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var recorder in recorders)
            {
                var radioRecorder = await DbContext.RadioRecorders
                                        .FirstOrDefaultAsync(r => r.RecorderIdentifier == recorder.Id);

                if(radioRecorder == null)
                {
                    radioRecorder = RadioRecorder.FromRecorder(recorder);
                    await DbContext.RadioRecorders.AddAsync(radioRecorder, cancellationToken);
                }
                else
                {
                    radioRecorder.UpdateFromRecorder(recorder);
                }

                Logger.LogInformation(radioRecorder.ToString());
            }

            await DbContext.SaveChangesAsync(cancellationToken);
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