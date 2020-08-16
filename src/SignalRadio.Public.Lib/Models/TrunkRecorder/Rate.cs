using Newtonsoft.Json;


namespace SignalRadio.Public.Lib.Models.TrunkRecorder
{
    public class Rate
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("decoderate")]
        public string Decoderate { get; set; }
    }
}
