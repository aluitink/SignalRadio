using System.Text.Json.Serialization;

namespace SignalRadio.Public.Lib.Models
{
    public class RadioFrequency 
    {
        public uint Id { get; set; }
        public uint RadioSystemId { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public RadioSystem RadioSystem { get; set; }
        public ulong FrequencyHz { get; set; }
        public bool ControlData { get; set; }
    }
}
