using Newtonsoft.Json;


namespace SignalRadio.Web.Api.Models.TrunkRecorder
{
    public class CallFrequency
    {
        [JsonProperty("freq")]
        public string Frequency { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("spikes")]
        public string Spikes { get; set; }

        [JsonProperty("errors")]
        public string Errors { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("length")]
        public string Length { get; set; }
    }
}
