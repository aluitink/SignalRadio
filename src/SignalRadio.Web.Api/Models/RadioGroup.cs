using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace SignalRadio.Web.Api.Models
{
    public class RadioGroup
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public uint RadioSystemId { get; set; }
        [JsonIgnore]
        public RadioSystem RadioSystem { get; set; }
        [JsonIgnore]
        public Collection<TalkGroup> TalkGroups { get; set; }
    }
}
