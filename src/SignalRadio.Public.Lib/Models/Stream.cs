using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace SignalRadio.Public.Lib.Models
{
    public class Stream
    {
        public uint Id {get;set;}
        public uint? MountPointId {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public MountPoint Mount {get;set;}
        public string StreamIdentifier {get;set;}
        public string Name {get;set;}
        public string Description {get;set;}
        public string Genra {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public User Owner {get;set;}
        public uint? OwnerUserId {get;set;}

        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public ICollection<TalkGroupStream> StreamTalkGroups { get; set; }
        public DateTime LastCallTimeUtc {get;set;}
    }
}
