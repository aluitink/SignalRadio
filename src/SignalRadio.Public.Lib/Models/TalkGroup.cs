using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using SignalRadio.Public.Lib.Models.Enums;

namespace SignalRadio.Public.Lib.Models
{
    public class TalkGroupImportResults
    {
        public bool IsSuccessful { get; set; }
        public uint ItemsProcessed { get; set; }
    }
    public class TalkGroup
    {
        public uint Id { get; set; }
        public uint? RadioGroupId { get; set; }
        public uint? RadioSystemId { get; set; }
        public ushort Identifier {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public RadioGroup RadioGroup { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public RadioSystem RadioSystem { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<RadioCall> RadioCalls {get;set;}
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<TalkGroupStream> TalkGroupStreams {get;set;}
        public TalkGroupMode Mode { get; set; }
        public TalkGroupTag Tag { get; set; }
        public string AlphaTag { get; set; }
        public string Name {get;set;}
        public string Description { get; set; }

    }
}
