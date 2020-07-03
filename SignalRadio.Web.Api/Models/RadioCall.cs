using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace SignalRadio.Web.Api.Models
{
    public class RadioCall
    {
        public uint Id { get; set; }
        public uint TalkGroupIdentifier { get; set; }
        public ushort CallState { get; set; }
        public ushort CallRecordState {get;set;}
        public string CallIdentifier { get; set; }

        [JsonIgnore]
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
        public uint Frequency { get; set; }        
        
        public string SigmfFileName {get;set;}
        public string DebugFilename {get;set;}
        public string Filename {get;set;}
        public string StatusFilename {get;set;}
    }
}
