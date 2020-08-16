using Newtonsoft.Json;


namespace SignalRadio.Public.Lib.Models.TrunkRecorder
{
    public class Source
    {
        [JsonProperty("source_num")]
        public string SourceNumber { get; set; }

        [JsonProperty("antenna")]
        public string Antenna { get; set; }

        [JsonProperty("qpsk")]
        public string Qpsk { get; set; }

        [JsonProperty("silence_frames")]
        public string SilenceFrames { get; set; }

        [JsonProperty("analog_levels")]
        public string AnalogLevels { get; set; }

        [JsonProperty("digital_levels")]
        public string DigitalLevels { get; set; }

        [JsonProperty("min_hz")]
        public string MinHz { get; set; }

        [JsonProperty("max_hz")]
        public string MaxHz { get; set; }

        [JsonProperty("center")]
        public string Center { get; set; }

        [JsonProperty("rate")]
        public string Rate { get; set; }

        [JsonProperty("driver")]
        public string Driver { get; set; }

        [JsonProperty("device")]
        public string Device { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("mix_gain")]
        public string MixGain { get; set; }

        [JsonProperty("lna_gain")]
        public string LnaGain { get; set; }

        [JsonProperty("vga1_gain")]
        public string Vga1Gain { get; set; }

        [JsonProperty("vga2_gain")]
        public string Vga2Gain { get; set; }

        [JsonProperty("bb_gain")]
        public string BbGain { get; set; }

        [JsonProperty("gain")]
        public string Gain { get; set; }

        [JsonProperty("if_gain")]
        public string IfGain { get; set; }

        [JsonProperty("squelch_db")]
        public string SquelchDb { get; set; }

        [JsonProperty("analog_recorders")]
        public string AnalogRecorders { get; set; }

        [JsonProperty("digital_recorders")]
        public string DigitalRecorders { get; set; }

        [JsonProperty("debug_recorders")]
        public string DebugRecorders { get; set; }

        [JsonProperty("sigmf_recorders")]
        public string SigmfRecorders { get; set; }
    }
}
