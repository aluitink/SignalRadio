using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using SignalRadio.Web.Api.Database;
using SignalRadio.Web.Api.Models.TrunkRecorder;

namespace SignalRadio.Web.Api
{
    public class TrunkRecorderStatusHandler
    {
        protected SignalRadioDbContext DbContext { get; }
        public TrunkRecorderStatusHandler(SignalRadioDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public async Task StartStatusMessageHandlerAsync(HttpContext context,
                                            WebSocket webSocket,
                                            CancellationToken cancellationToken = default(CancellationToken))
        {
            var buffer = new byte[1024 * 16];
            WebSocketReceiveResult result = null;
            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                await HandleStatusMessageAsync(buffer, result.Count);
            }
            while (!result.CloseStatus.HasValue);

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, cancellationToken);
        }

        private async Task HandleStatusMessageAsync(byte[] messageBuffer, int count, CancellationToken cancellationToken = default(CancellationToken))
        {
            try 
            {
                var jsonString = ToUtf8(messageBuffer, count);
                
                var obj = JObject.Parse(jsonString);
                
                var message = obj.First.DeepClone();
                
                obj.First.Remove();

                var statusMessage = obj.ToObject<StatusMessage>();
                
                switch(statusMessage.Type)
                {
                    case "config":
                        break;
                    case "rates":
                        //var rate = message.First.ToObject<Rate>();
                        break;
                    case "system":
                        //var system = message.First.ToObject<Models.TrunkRecorder.System>();
                        break;
                    case "calls_active":
                        //var calls = message.First.ToObject<List<Call>>();
                        break;
                    case "call_start":
                        var callStart = message.First.ToObject<Call>();
                        await DbContext.RadioCalls.AddAsync(new Models.RadioCall() {
                            TalkGroupIdentifier = uint.Parse(callStart.Talkgroup),
                            CallIdentifier = callStart.Id,
                            TalkGroupTag = callStart.Talkgrouptag,
                            Elapsed = uint.Parse(callStart.Elasped),
                            Length = uint.Parse(callStart.Length),
                            IsPhase2 = bool.Parse(callStart.Phase2),
                            IsConventional = bool.Parse(callStart.Conventional),
                            IsEncrypted = bool.Parse(callStart.Encrypted),
                            IsAnalog = bool.Parse(callStart.Analog),
                            StartTime = ulong.Parse(callStart.StartTime),
                            StopTime = ulong.Parse(callStart.StopTime),
                            Frequency = uint.Parse(callStart.Frequency),
                            SigmfFileName = callStart.SigmfFilename,
                            DebugFilename = callStart.DebugFilename,
                            Filename = callStart.Filename,
                            StatusFilename = callStart.StatusFilename,
                            CallState = ushort.Parse(callStart.State),
                            CallRecordState = ushort.Parse(callStart.RecordState)
                        }, cancellationToken);

                        await DbContext.SaveChangesAsync(cancellationToken);
                        break;
                    case "call_end":
                        var callEnd = message.First.ToObject<Call>();
                        var existingCall = DbContext.RadioCalls
                            .Where(c => c.CallIdentifier == callEnd.Id)
                            .FirstOrDefault();

                        if(existingCall != null)
                        {
                            if(callEnd.StopTime != null)
                                existingCall.StopTime = ulong.Parse(callEnd.StopTime);
                            if(callEnd.State != null)
                                existingCall.CallState = ushort.Parse(callEnd.State);
                            if(callEnd.RecordState != null)
                                existingCall.CallRecordState = ushort.Parse(callEnd.RecordState);
                        }

                        await DbContext.SaveChangesAsync(cancellationToken);
                        break;
                    case "recorders":
                        //var recorders = message.First.ToObject<List<Recorder>>();
                        break;
                    case "recorder":
                        //var recorder = message.First.ToObject<Recorder>();
                        break;
                }

                
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
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