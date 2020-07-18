namespace SignalRadio.LiquidBridge
{
    public class Call
    {
        public TalkGroup TalkGroup { get;set; }
        public long Timestamp { get; set; }
        public string WavPath { get; set; }
        public string Mp3Path { get; set; }
        public long FrequencyHz { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}]({1}) - {2}", Timestamp, FrequencyHz, WavPath);
        }
    }
}
