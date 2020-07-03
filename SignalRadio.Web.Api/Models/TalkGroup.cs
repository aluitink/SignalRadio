using Newtonsoft.Json;

namespace SignalRadio.Web.Api.Models
{
    public class TalkGroup
    {
        public uint Id { get; set; }
        public uint RadioGroupId { get; set; }
        public uint RadioSystemId { get; set; }
        public ushort Identifier {get;set;}

        [JsonIgnore]
        public RadioGroup RadioGroup { get; set; }
        [JsonIgnore]
        public RadioSystem RadioSystem { get; set; }
        
        public TalkGroupMode Mode { get; set; }
        public TalkGroupTag Tag { get; set; }

        public string AlphaTag { get; set; }
        public string Description { get; set; }

    }
}
