using System.Collections.Generic;
using Newtonsoft.Json;


namespace SignalRadio.Public.Lib.Models.TrunkRecorder
{
    public class Call
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("freq")]
        public string Frequency { get; set; }

        [JsonProperty("sysNum")]
        public string SystemNumber { get; set; }

        [JsonProperty("shortName")]
        public string ShortName { get; set; }

        [JsonProperty("talkgroup")]
        public string Talkgroup { get; set; }

        [JsonProperty("talkgrouptag")]
        public string Talkgrouptag { get; set; }

        [JsonProperty("elasped")]
        public string Elasped { get; set; }

        [JsonProperty("length")]
        public string Length { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("phase2")]
        public string Phase2 { get; set; }

        [JsonProperty("conventional")]
        public string Conventional { get; set; }

        [JsonProperty("encrypted")]
        public string Encrypted { get; set; }

        [JsonProperty("emergency")]
        public string Emergency { get; set; }

        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        [JsonProperty("stopTime")]
        public string StopTime { get; set; }

        [JsonProperty("sourceList")]
        public IList<CallSource> SourceList { get; set; }

        [JsonProperty("freqList")]
        public IList<CallFrequency> FrequencyList { get; set; }

        [JsonProperty("recNum")]
        public string RecordNumber { get; set; }

        [JsonProperty("srcNum")]
        public string SourceNumber { get; set; }

        [JsonProperty("recState")]
        public string RecordState { get; set; }

        [JsonProperty("analog")]
        public string Analog { get; set; }

        [JsonProperty("sigmffilename")]
        public string SigmfFilename { get; set; }

        [JsonProperty("debugfilename")]
        public string DebugFilename { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("statusfilename")]
        public string StatusFilename { get; set; }
    }
}
