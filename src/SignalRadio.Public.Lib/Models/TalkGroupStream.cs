using System.Text.Json.Serialization;

namespace  SignalRadio.Public.Lib.Models
{
    public class TalkGroupStream
    {
        public uint TalkGroupId {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public TalkGroup TalkGroup {get;set;} 
        public uint StreamId {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Stream Stream {get;set;}
    }
}