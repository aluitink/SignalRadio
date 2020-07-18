using Newtonsoft.Json;

namespace SignalRadio.Web.Api.Models
{
    public class RadioFrequency 
    {
        public uint Id { get; set; }
        public uint RadioSystemId { get; set; }
        [JsonIgnore]
        public RadioSystem RadioSystem { get; set; }
        public ulong FrequencyHz { get; set; }
        public bool ControlData { get; set; }
    }
}
