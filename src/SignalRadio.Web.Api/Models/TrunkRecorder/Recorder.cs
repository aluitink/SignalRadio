using Newtonsoft.Json;


namespace SignalRadio.Web.Api.Models.TrunkRecorder
{
    public class Recorder
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("srcNum")]
        public string SourceNumber { get; set; }

        [JsonProperty("recNum")]
        public int RecorderNumber { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("status_len")]
        public string StatusLength { get; set; }

        [JsonProperty("status_error")]
        public string StatusError { get; set; }

        [JsonProperty("status_spike")]
        public string StatusSpike { get; set; }
    }
}
