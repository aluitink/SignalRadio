using System.Text.Json.Serialization;
using System.Collections.ObjectModel;

namespace SignalRadio.Public.Lib.Models
{
    public class RadioGroup
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public uint RadioSystemId { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public RadioSystem RadioSystem { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<TalkGroup> TalkGroups { get; set; }
    }
}
