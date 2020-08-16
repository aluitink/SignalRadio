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
    }
}
