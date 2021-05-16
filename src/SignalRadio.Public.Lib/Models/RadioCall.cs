using System.Text.Json.Serialization;
using System.Collections.ObjectModel;

namespace SignalRadio.Public.Lib.Models
{
    public class RadioCall
    {
        public uint Id { get; set; }
        public ushort TalkGroupIdentifier { get; set; }
        public ushort CallState { get; set; }
        public ushort CallRecordState {get;set;}
        public string CallIdentifier { get; set; }

        public uint TalkGroupId {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public TalkGroup TalkGroup { get; set; }
        public string TalkGroupTag {get;set;}
        public uint Elapsed {get;set;}
        public uint Length {get;set;}
        public bool IsPhase2 {get;set;}
        public bool IsConventional {get;set;}
        public bool IsEncrypted {get;set;}
        public bool IsAnalog {get;set;}
        public bool IsEmergency {get;set;}
        public ulong StartTime {get;set;}
        public ulong StopTime {get;set;}
        public long FrequencyHz { get; set; }
        public long Frequency {get;set;}
        public long CallSerialNumber {get;set;}
        public string CallWavPath {get;set;}
        public string SigmfFileName {get;set;}
        public string DebugFilename {get;set;}
        public string Filename {get;set;}
        public string StatusFilename {get;set;}


        public static RadioCall FromCall(TrunkRecorder.Call call)
        {
            var radioCall = new RadioCall();
            radioCall.UpdateFromCall(call);
            return radioCall;
        }

        public void UpdateFromCall(TrunkRecorder.Call call)
        {
            CallIdentifier = call.Id;
            CallRecordState = ushort.Parse(call.RecordState);
            CallState = ushort.Parse(call.State);
            Elapsed = uint.Parse(call.Elasped);
            Frequency = long.Parse(call.Frequency);
            IsAnalog = bool.Parse(call.Analog);
            IsConventional = bool.Parse(call.Conventional);
            IsEncrypted = bool.Parse(call.Encrypted);
            IsEmergency = bool.Parse(call.Emergency);
            IsPhase2 = bool.Parse(call.Phase2);
            Length = uint.Parse(call.Length);
            StartTime = ulong.Parse(call.StartTime);
            StopTime = ulong.Parse(call.StopTime);
            TalkGroupIdentifier = ushort.Parse(call.Talkgroup);
            TalkGroupTag = call.Talkgrouptag;

            CallWavPath = call.Filename;
            Filename = call.Filename;
            DebugFilename = call.DebugFilename;
            SigmfFileName = call.SigmfFilename;
            StatusFilename = call.StatusFilename;
        }
    }
}
