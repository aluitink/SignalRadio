using Newtonsoft.Json;

namespace SignalRadio.Public.Lib.Models.TrunkRecorder
{
    public class StatusMessage
    {

        [JsonProperty("captureDir")]
        public string CaptureDir { get; set; }

        [JsonProperty("uploadServer")]
        public string UploadServer { get; set; }

        [JsonProperty("callTimeout")]
        public string CallTimeout { get; set; }

        [JsonProperty("logFile")]
        public string LogFile { get; set; }

        [JsonProperty("instanceId")]
        public string InstanceId { get; set; }

        [JsonProperty("instanceKey")]
        public string InstanceKey { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class Config
    {

    }
}
