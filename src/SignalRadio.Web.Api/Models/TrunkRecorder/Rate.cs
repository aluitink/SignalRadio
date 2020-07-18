using Newtonsoft.Json;


namespace SignalRadio.Web.Api.Models.TrunkRecorder
{
    public class Rate
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("decoderate")]
        public string Decoderate { get; set; }
    }
}
