using System.Text.Json.Serialization;
using SignalRadio.Public.Lib.Models.Enums;
using System;
using System.Collections.ObjectModel;

namespace SignalRadio.Public.Lib.Models
{
    public class RadioSystem 
    {
        public uint Id { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string County { get; set; }
        public RadioSystemType SystemType { get; set; }
        public RadioSystemVoice SystemVoice { get; set; }
        public DateTime LastUpdated { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<RadioFrequency> ControlFrequencies { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<RadioGroup> RadioGroups { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<TalkGroup> TalkGroups { get; set; }
    }
}
