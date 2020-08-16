using System.Text.Json.Serialization;

namespace SignalRadio.Public.Lib.Models
{
    public class MountPoint
    {
        public uint Id {get;set;}
        public uint UserId {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public User User {get;set;}
        public uint StreamId {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Stream Stream {get;set;}
        public string Name {get;set;}
        public string Host {get;set;}
        public uint Port {get;set;}
        public string Password {get;set;}
    }
}
