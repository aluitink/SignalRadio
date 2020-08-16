using Newtonsoft.Json;

namespace SignalRadio.Public.Lib.Models.TrunkRecorder
{
    public class CallSource
    {

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("signal_system")]
        public string SignalSystem { get; set; }

        [JsonProperty("emergency")]
        public string Emergency { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }
    }
}
