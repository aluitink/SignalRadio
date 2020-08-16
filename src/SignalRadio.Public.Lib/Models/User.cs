using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace SignalRadio.Public.Lib.Models
{
    public class User
    {
        public uint Id {get;set;}
        public string Username {get;set;}
        public string EmailAddress {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<MountPoint> MountPoints {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<Stream> Streams {get;set;}
    }
}
