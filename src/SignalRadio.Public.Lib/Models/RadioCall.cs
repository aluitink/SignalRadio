using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using System;

namespace SignalRadio.Public.Lib.Models
{
    public class RadioCall
    {
        public uint Id { get; set; }
        public ushort CallState { get; set; }
        public ushort CallRecordState {get;set;}
        public string CallIdentifier { get; set; }
        public long CallSerialNumber { get; set; }

        public uint TalkGroupId {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public TalkGroup TalkGroup { get; set; }

        public uint Elapsed {get;set;}
        public bool IsPhase2 {get;set;}
        public bool IsConventional {get;set;}
        public bool IsEncrypted {get;set;}
        public bool IsAnalog {get;set;}
        public bool IsEmergency {get;set;}
        public DateTime StartTime {get;set;}
        public DateTime StopTime {get;set;}
        public long Frequency {get;set;}

        public string CallWavPath {get;set;}

        public static RadioCall FromCall(TrunkRecorder.Call call)
        {
            var radioCall = new RadioCall();
            radioCall.UpdateFromCall(call);
            return radioCall;
        }

        public void UpdateFromCall(TrunkRecorder.Call call)
        {
            CallIdentifier = call.Id;

            if (!string.IsNullOrEmpty(call.StartTime))
                CallSerialNumber = long.Parse(call.StartTime);
            if (!string.IsNullOrWhiteSpace(call.RecordState))
                CallRecordState = ushort.Parse(call.RecordState);
            if (!string.IsNullOrWhiteSpace(call.State))
                CallState = ushort.Parse(call.State);
            if (!string.IsNullOrWhiteSpace(call.Elasped))
                Elapsed = uint.Parse(call.Elasped);
            if (!string.IsNullOrWhiteSpace(call.Frequency))
                Frequency = long.Parse(call.Frequency);
            if (!string.IsNullOrWhiteSpace(call.Analog))
                IsAnalog = bool.Parse(call.Analog);
            if (!string.IsNullOrWhiteSpace(call.Conventional))
                IsConventional = bool.Parse(call.Conventional);
            if (!string.IsNullOrWhiteSpace(call.Encrypted))
                IsEncrypted = bool.Parse(call.Encrypted);
            if (!string.IsNullOrWhiteSpace(call.Emergency))
                IsEmergency = bool.Parse(call.Emergency);
            if (!string.IsNullOrWhiteSpace(call.Phase2))
                IsPhase2 = bool.Parse(call.Phase2);
            if (!string.IsNullOrWhiteSpace(call.StartTime))
                StartTime = DateTimeFromFileTime(long.Parse(call.StartTime));
            if (!string.IsNullOrWhiteSpace(call.StopTime))
                StopTime = DateTimeFromFileTime(long.Parse(call.StopTime));

            CallWavPath = call.Filename;
        }
    
        public override string ToString()
        {
            return string.Format("RadioCall[{0}](SN={1}) - TalkGroup: {2}, Elapsed: {3}", Id, CallSerialNumber, TalkGroup?.Name ?? "Unknown", Elapsed);
        }

        private DateTime DateTimeFromFileTime(long fileTime)
        {
            return new DateTime(1970, 1, 1).ToUniversalTime().AddSeconds(fileTime);
        }
    }
}
